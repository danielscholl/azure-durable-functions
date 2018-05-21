# docker-swarm-azure

This is a sample for writing durable functions  and enabling docker support.

__Requirements:__

- [.Net Core](https://www.microsoft.com/net/download/windows)  (>= 2.1.104)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (>= 2.0.32)
- [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools) (>= 2.0)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) (>= 5.3)
- [httpie](https://github.com/jakubroztocil/httpie) (>= 0.9.8)
- [docker](https://docs.docker.com/install/) (>= 17.12.0-ce)

## Installation
### Clone the repo or build it yourself

```bash
git clone https://github.com/danielscholl/docker-swarm-azure.git durable-functions
```

### Initialize a new Function project

```bash
mkdir durable-functions
cd durable-functions
func init --worker-runtime dotnet
```

### Initialize a DotNet Library

```bash
dotnet new lib --name durable-functions -o .
rm .\Class1.cs
```

### Add Library Dependencies

```bash
dotnet add package Microsoft.AspNetCore.Http -v 2.1.0-rc1-final
dotnet add package Microsoft.AspNetCore.Mvc -v 2.1.0-rc1-final
dotnet add package Microsoft.AspNetCore.Mvc.WebApiCompatShim -v 2.1.0-rc1-final
dotnet add package Microsoft.Azure.WebJobs -v 3.0.0-beta5
dotnet add package Microsoft.Azure.WebJobs.Extensions.DurableTask -v 1.4.1
dotnet add package Microsoft.Azure.WebJobs.Script.ExtensionsMetadataGenerator -v 1.0.0-beta3
```

### Create and test a Hello World Function

```bash
# Create It
func new -l c# -t "Http Trigger" -n echo

# Run It
func start

# Start It
http get http://localhost:7071/api/echo?name=world
```

### Create and Retrieve a Storage Account Connection String

- Option 1:  Use Azure Storage Emulator

```
AzureStorageEmulator.exe start
```

- Option 2:  Use Azure Storage Account

```bash
# Login to Azure and set subscription if necessary
Subscription='<azure_subscription_name>'
az login
az account set --subscription ${Subscription}

# Create Resource Group
ResourceGroup="durable-functions"
Location="southcentralus"
az group create --name ${ResourceGroup} \
  --location ${Location} \
  -ojsonc

# Create a Storage Account
StorageAccount="durablefunctions"$(date "+%m%d%Y")
az storage account check-name --name ${StorageAccount}
az storage account create --name ${StorageAccount} \
  --resource-group ${ResourceGroup} \
  --location ${Location} \
  --sku "Standard_LRS" \
  -ojsonc

# Set Storage Account Context
export STORAGE_CONNECTION=$(az storage account show-connection-string --name ${StorageAccount} --resource-group ${ResourceGroup} --query connectionString -otsv)

# Set Storage Account into .envrc file
echo "export STORAGE_ACCOUNT='${STORAGE_CONNECTION}'" > .envrc

# Create local.settings.json file
cat > local.settings.json << EOF1
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "${STORAGE_CONNECTION}"
  }
}
EOF1
```


### Create and test a Durable Function (Pattern #1 - Function Chaining)

#### Create It

```bash
func new -l C# -t "Durable Functions Activity" -n SayHello
func new -l C# -t "Durable Functions HTTP starter" -n HttpTrigger
func new -l C# -t "Durable Functions orchestrator" -n pattern1
```


#### Edit it

Modify pattern1/run.csx to call the proper Activity Function "SayHello"

```C#
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"

public static async Task<List<string>> Run(DurableOrchestrationContext context)
{
    var outputs = new List<string>();

    // Replace "hello" with the name of your Durable Activity Function.
    outputs.Add(await context.CallActivityAsync<string>("SayHello", "Tokyo"));
    outputs.Add(await context.CallActivityAsync<string>("SayHello", "Seattle"));
    outputs.Add(await context.CallActivityAsync<string>("SayHello", "London"));

    // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
    return outputs;
}
```

#### Test it

```bash
func start
http post http://localhost:7071/api/orchestrators/pattern1
```


### Create and test a Durable Function (Pattern #2 - Fan-out/Fan-in)

#### Create It
`func new -l C# -t "Durable Functions Activity" -n ItemList`

#### Edit it

Modify ItemList/function.json

```javascript
{
  "bindings": [
    {
      "name": "count",
      "type": "activityTrigger",
      "direction": "in"
    }
  ],
  "disabled": false
}
```

Modify ItemList/run.csx

```c#
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Microsoft.Extensions.Logging"

public static string[] Run(int count, ILogger log)
{
    string[] items = new string[count];
    log.LogInformation($"Creating array {items.Length}.");
    return items;
}
```

#### Create It
`func new -l C# -t "Durable Functions Activity" -n ItemAction`

#### Edit it

Modify ItemAction/function.json

```javascript
{
  "bindings": [
    {
      "name": "item",
      "type": "activityTrigger",
      "direction": "in"
    }
  ],
  "disabled": false
}
```

Modify CopyFileToBlob/run.csx

```c#
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Microsoft.Extensions.Logging"

public static async Task<long> Run(string item, Binder binder, ILogger log)
{
    long byteCount = 10;
    log.LogInformation($"**********  Received a Request for: '{item}'");

    return byteCount;
}
```

#### Create It
`func new -l C# -t "Durable Functions orchestrator" -n pattern2`

#### Edit it

Modify pattern2/run.csx

```c#
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"

public static async Task<long> Run(DurableOrchestrationContext context)
{

  string[] list = await context.CallActivityAsync<string[]>("ItemList", 3);

  list[0] = "Item One";
  list[1] = "Item Two";
  list[2] = "Item Three";

  var tasks = new Task<long>[list.Length];
  for (int i = 0; i < list.Length; i++)
  {
      tasks[i] = context.CallActivityAsync<long>("ItemAction", list[i]);
  }

  await Task.WhenAll(tasks);

  return list.Length;
}
```

#### Test it

```bash
func start
http post http://localhost:7071/api/orchestrators/pattern2

```






### Containerize and Test it

```bash
# Build It
docker build --build-arg STORAGE_ACCOUNT=${STORAGE_ACCOUNT} -t durable-functions .

# Run It
docker run -d -p 5000:80 durable

# Test It
http get http://localhost:5000/api/echo?name=world
http post http://localhost:5000/api/orchestrators/orchestrator
```


//TODO:  Create the Human Interaction Sample
https://docs.microsoft.com/en-us/azure/azure-functions/durable-functions-phone-verification

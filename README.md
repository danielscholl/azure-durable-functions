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

```bash
# Create It
func new -l C# -t "Durable Functions Activity" -n Hello
func new -l C# -t "Durable Functions HTTP starter" -n httpstart
func new -l C# -t "Durable Functions orchestrator" -n orchestrator

# Build It
dotnet build

# Start It
func start

# Test It
http post http://localhost:7071/api/orchestrators/orchestrator
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


func init

dotnet new lib --name durable-functions -o .
rm .\Class.cs

dotnet add package Microsoft.AspNetCore.Http -v 2.1.0-rc1-final
dotnet add package Microsoft.AspNetCore.Mvc -v 2.1.0-rc1-final
dotnet add package Microsoft.AspNetCore.Mvc.WebApiCompatShim -v 2.1.0-rc1-final
dotnet add package Microsoft.Azure.WebJobs -v 3.0.0-beta5
dotnet add package Microsoft.Azure.WebJobs.Extensions.DurableTask -v 1.4.1
dotnet add package Microsoft.Azure.WebJobs.Script.ExtensionsMetadataGenerator -v 1.0.0-beta3

func new -l javascript -t "Http Trigger" -n echo

func start
http get http://localhost:7071/api/echo?name=daniel

func new -l C# -t "Durable Functions Activity" -n Hello
func new -l C# -t "Durable Functions HTTP starter" -n httpstart
func new -l C# -t "Durable Functions orchestrator" -n orchestrator

dotnet build

func start
http post http://localhost:7071/api/orchestrators/orchestrator

docker build --build-arg STORAGE_ACCOUNT=${STORAGE_ACCOUNT} -t durable-functions .
docker run -it -p 5000:80 durable
http get http://localhost:5000/api/echo?name=daniel
http post http://localhost:5000/api/orchestrators/orchestrator
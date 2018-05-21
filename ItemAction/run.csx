#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Microsoft.Extensions.Logging"

public static async Task<long> Run(string item, Binder binder, ILogger log)
{
    long byteCount = 10;
    log.LogInformation($"**********  Received a Request for: '{item}'");

    return byteCount;
}

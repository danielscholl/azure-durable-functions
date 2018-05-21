#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Microsoft.Extensions.Logging"

public static string[] Run(int count, ILogger log)
{
    string[] items = new string[count];
    log.LogInformation($"Creating array {items.Length}.");
    return items;
}

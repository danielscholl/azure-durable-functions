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

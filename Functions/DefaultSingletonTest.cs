using Azure.Data.Tables;
using FunctionApp.SingletonDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionApp.SingletonDemo.Functions
{
    public static class DefaultSingletonTest
    {
        private static int _concurrentFunctionsByInstance;

        [FunctionName(nameof(DefaultSingletonTest) + "Start")]
        public static async Task<IActionResult> Start(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "default")] HttpRequest req,
            [Queue(Constants.DefaultTestQueueName)] IAsyncCollector<DoAction> commands,
            ILogger log)
        {

            var name = req.Query.TryGetValue("name", out var qValue) &&
                       !string.IsNullOrEmpty(qValue)
                ? qValue.ToString()
                : $"run-default@{DateTime.Now:T}";

            if (!(req.Query.TryGetValue("count", out qValue) &&
                  int.TryParse(qValue, out int count)))
            {
                count = 10;
            }
            if (!(req.Query.TryGetValue("duration", out qValue) &&
                  TimeSpan.TryParse(qValue, out TimeSpan duration)))
            {
                duration = TimeSpan.FromSeconds(1);
            }
            foreach (var index in Enumerable.Range(0, count))
            {
                var command = new DoAction
                {
                    TestRun = name,
                    Index = index,
                    Duration = duration
                };
                await commands.AddAsync(command).ConfigureAwait(false);
            }
            return new OkObjectResult($"Start test run '{name}' with {count} messages");
        }

        [Singleton]
        [FunctionName(nameof(DefaultSingletonTest))]
        public static async Task ProcessCommand(
            [QueueTrigger(Constants.DefaultTestQueueName)] DoAction command,
            [Table(Constants.DefaultTestTableName)] TableClient table)
        {
            try
            {
                var concurrentCount = Interlocked.Increment(ref _concurrentFunctionsByInstance);
                var start = DateTimeOffset.UtcNow;
                var entity = new ActionEntity(command.TestRun, command.Index, Environment.MachineName, concurrentCount, start, start.Add(command.Duration));
                await Task.WhenAll(
                        table.AddEntityAsync(entity),
                        Task.Delay(command.Duration))
                    .ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentFunctionsByInstance);
            }

        }
    }
}

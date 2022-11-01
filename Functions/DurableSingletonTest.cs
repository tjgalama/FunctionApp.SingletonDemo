using Azure.Data.Tables;
using FunctionApp.SingletonDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionApp.SingletonDemo.Functions
{
    public static class DurableSingletonTest
    {
        private const string OrchestratorName = nameof(DurableSingletonTest) + "Orchestrator";
        private const string ActivityName = nameof(DurableSingletonTest) + "Activity";

        private static int _concurrentFunctionsByInstance;

        [FunctionName(nameof(DurableSingletonTest) + "Start")]
        public static async Task<IActionResult> Start(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "durable")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient orchestrationClient)
        {

            var name = req.Query.TryGetValue("name", out var qValue) &&
                       !string.IsNullOrEmpty(qValue)
                ? qValue.ToString()
                : $"run-durable@{DateTime.Now:T}";

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

            var command = new StartActions
            {
                TestRun = name,
                Count = count,
                ActionDuration = duration
            };

            var instanceId = await orchestrationClient.StartNewAsync(OrchestratorName, command).ConfigureAwait(false);

            return new OkObjectResult($"Start orchestrator with [{command}]  (id:{instanceId})");
        }

        [FunctionName(OrchestratorName)]
        public static async Task Orchestrate(
            [OrchestrationTrigger] IDurableOrchestrationContext orchestrationContext,
            ILogger logger)
        {
            logger = orchestrationContext.CreateReplaySafeLogger(logger);

            var command = orchestrationContext.GetInput<StartActions>();
            var tasks = new List<Task>();
            foreach (var index in Enumerable.Range(0, command.Count))
            {
                var newAction = new DoAction
                {
                    TestRun = command.TestRun,
                    Index = index,
                    Duration = command.ActionDuration
                };
                var newTask = orchestrationContext.CallActivityAsync(ActivityName, newAction);
                tasks.Add(newTask);
            }

            await Task.WhenAll(tasks);
        }

        [Singleton]
        [FunctionName(ActivityName)]
        public static async Task ProcessCommand(
            [ActivityTrigger] DoAction command,
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

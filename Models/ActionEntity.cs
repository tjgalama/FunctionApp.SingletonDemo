using Azure;
using Azure.Data.Tables;
using System;

namespace FunctionApp.SingletonDemo.Models
{
    public class ActionEntity : ITableEntity
    {
        public ActionEntity()
        {
        }

        public ActionEntity(string testRun, int index, string machineName, int concurrentFunctionsByInstance, DateTimeOffset start, DateTimeOffset finish)
        {
            PartitionKey = testRun;
            RowKey = index.ToString("D7");
            MachineName = machineName;
            ConcurrentFunctionsByInstance = concurrentFunctionsByInstance;
            Start = start;
            Finish = finish;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string MachineName { get; set; }
        public int ConcurrentFunctionsByInstance { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? Finish { get; set; }

    }
}

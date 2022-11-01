using System;

namespace FunctionApp.SingletonDemo.Models;

public class StartActions
{
    public string TestRun { get; set; }
    public int Count { get; set; }
    public TimeSpan ActionDuration { get; set; }

    public override string ToString() => $"{TestRun};{Count}";
}
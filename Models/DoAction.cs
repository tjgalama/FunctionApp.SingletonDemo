using System;

namespace FunctionApp.SingletonDemo.Models;

public class DoAction
{
    public string TestRun { get; set; }
    public int Index { get; set; }
    public TimeSpan Duration { get; set; }

    public override string ToString() => $"{TestRun};{Index}";
}
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace MyApp.Backend.Infrastructure;

public class MetricsCollector
{
    private readonly string _controllerName;
    private readonly Summary _rpsSummary;
    private readonly Summary _latencySummary;
    
    public MetricsCollector(string controllerName)
    {
        _controllerName = controllerName;

        _rpsSummary = Metrics.CreateSummary(
            "rps_summary",
            "Records RPS.",
            new SummaryConfiguration()
            {
                MaxAge = new TimeSpan(0, 0, 0, 1),
                LabelNames = new[] { "class_name", "method_name" },
            });

        _latencySummary = Metrics.CreateSummary(
            "elapsed_time_ms_summary",
            "Records latency of methods.",
            new SummaryConfiguration()
            {
                LabelNames = new[] { "class_name", "method_name" },
                Objectives = new List<QuantileEpsilonPair>
                {
                    new QuantileEpsilonPair(0.5, 0.05),
                    new QuantileEpsilonPair(0.95, 0.005),
                    new QuantileEpsilonPair(0.99, 0.001),
                }
            });
    }

    public async Task<ActionResult> ExecuteWithMetrics(string methodName, Func<Task<ActionResult>> handler)
    {
        CollectRps(methodName);
        var stopwatch = Stopwatch.StartNew();

        var result = await handler();

        CollectElapsedTime(methodName, stopwatch);
        return result;
    }

    private void CollectElapsedTime(string methodName, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _latencySummary.Labels(_controllerName, methodName).Observe(stopwatch.ElapsedMilliseconds);
    }

    private void CollectRps(string methodName)
    {
        _rpsSummary.Labels(_controllerName, methodName).Observe(1);
    }
}
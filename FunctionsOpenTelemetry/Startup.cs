using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[assembly: FunctionsStartup(typeof(FunctionsOpenTelemetry.Startup))]
namespace FunctionsOpenTelemetry
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // OpenTelemetry Resource to be associated with logs, metrics and traces
            var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault().AddService("opentelemetry-service");

            // Enable Logging with OpenTelemetry
            builder.Services.AddLogging( (loggingBuilder) => 
                {
                    // Only Warning or above will be sent to Opentelemetry
                    loggingBuilder.AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.Warning);
                }
            );
            
            builder.Services.AddSingleton<ILoggerProvider, OpenTelemetryLoggerProvider>();
            builder.Services.Configure<OpenTelemetryLoggerOptions>( (openTelemetryLoggerOptions) =>
                {
                    openTelemetryLoggerOptions.SetResourceBuilder(openTelemetryResourceBuilder);
                    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                    openTelemetryLoggerOptions.AddConsoleExporter();
                }
            );

            // Enable Tracing with OpenTelemetry
            var openTelemetryTracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(openTelemetryResourceBuilder)
                .SetSampler(new AlwaysOnSampler())
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter()
                .Build();
            builder.Services.AddSingleton(openTelemetryTracerProvider);

            // Enable Metrics with OpenTelemetry
            var openTelemetryMeterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(openTelemetryResourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddMeter(Function1.MyMeter.Name)
                .AddConsoleExporter(consoleOptions =>
                    {
                        consoleOptions.MetricReaderType = MetricReaderType.Periodic;
                        consoleOptions.AggregationTemporality = AggregationTemporality.Cumulative;
                        consoleOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000;
                    })
                .Build();
            builder.Services.AddSingleton(openTelemetryMeterProvider);
        }
    }
}

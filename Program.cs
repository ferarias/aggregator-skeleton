using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MultiproviderTest.Domain;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace MultiproviderTest
{

    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .CreateLogger();

            var connectors = new List<ProviderConnector>();
            for (var i = 0; i < 50; i++)
            {
                connectors.Add(new ProviderConnector { ConnectorCode = "CONN" + i });
            }

            var request = new MyRequest
            {
                CorrelationId = "REQ-001"
            };

            
            System.Console.WriteLine("Starting...");

            var engine = new ConnectorEngine() {CallTimeout = TimeSpan.FromSeconds(5)};

            var results = await engine.SendRequest(request, connectors);

            foreach (var item in results)
            {
                Console.WriteLine($"{item.Provider} {item.Status} {item.Response} {item.Ex?.Message}");
            }
        }
    }
}

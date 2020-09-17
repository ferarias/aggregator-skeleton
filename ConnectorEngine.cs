using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MultiproviderTest.Domain;
using Serilog;
using SerilogTimings;

namespace MultiproviderTest
{
    public class ConnectorEngine
    {
        public TimeSpan CallTimeout { get; set; }

        private static readonly Random Randomizer = new Random();

        public async Task<List<EngineResult<MyResponse>>> SendRequest(
            MyRequest request, 
            IEnumerable<ProviderConnector> connectors)
        {
            var results = new ConcurrentBag<EngineResult<MyResponse>>();
            await connectors.ForEachAsync(
                connector => GetFromProviderTaskAsync(connector, request, CallTimeout),
                (connector, t) => results.Add(t));
            return results.ToList();
        }

        private async Task<EngineResult<MyResponse>> GetFromProviderTaskAsync(ProviderConnector connector, MyRequest request, TimeSpan timeOut)
        {
            if (connector == null)
            {
                Log.Error("!! GetFromProviderTaskAsync (null connector)");
                throw new ArgumentNullException(nameof(connector));
            }

            if (request == null)
            {
                Log.Error($"!! GetFromProviderTaskAsync (null request)");
                throw new ArgumentNullException(nameof(request));
            }

            using (var op = Operation.Begin($"Retrieving data for {connector.ConnectorCode}"))
            {
                try
                {

                    var cts = new CancellationTokenSource();

                    void TimerCallback(object o)
                    {
                        var c = o as ProviderConnector;
                        cts.Cancel();
                        Log.Warning(
                            $"!! GetFromProviderTaskAsync TIMEOUT {request.CorrelationId} provider '{c?.ConnectorCode}'");
                    }


                    await using var timer = new Timer(TimerCallback, connector, timeOut, Timeout.InfiniteTimeSpan);
                    var response = await CallProviderAsync(connector, request, cts.Token);
                    op.Complete();

                    return new EngineResult<MyResponse>
                    {
                        Provider = connector.ConnectorCode,
                        Response = response,
                        Status = "OK",
                        Ex = null
                    };

                }

                catch (TaskCanceledException tce)
                {
                    op.Abandon();
                    return new EngineResult<MyResponse>
                    {
                        Provider = connector.ConnectorCode,
                        Response = null,
                        Status = "TO",
                        Ex = tce
                    };
                }
                catch (Exception ex)
                {
                    op.Abandon();
                    return new EngineResult<MyResponse>
                    {
                        Provider = connector.ConnectorCode,
                        Response = null,
                        Status = "KO",
                        Ex = ex
                    };
                }
            }
        }

        private static async Task<MyResponse> CallProviderAsync(ProviderConnector connector, MyRequest request, CancellationToken cancellationToken)
        {
            using (Operation.Time($"Submitting request for {connector.ConnectorCode}"))
            {

                await Task.Delay(Randomizer.Next(5000), cancellationToken);
                return new MyResponse
                {
                    CorrelationId = request.CorrelationId,
                    Ids = new int[] {1, 2, 3, 4, 5}
                };
            }
        }
    }
}

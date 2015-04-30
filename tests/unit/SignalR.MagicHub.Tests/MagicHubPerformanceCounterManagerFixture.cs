using System.Threading;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Performance;

namespace SignalR.MagicHub.Tests
{
    [TestFixture]
    public class MagicHubPerformanceCounterManagerFixture
    {
        [Test]
        [Explicit] // todo: make this meaningful
        public void Test()
        {
            var manager = new MagicHubPerformanceCounterManager(Mock.Of<ITraceManager>());

            manager.Initialize("Foo", new CancellationToken());

            manager.NumberDispatchedToSignalRTotal.Increment();

        }
    }
}

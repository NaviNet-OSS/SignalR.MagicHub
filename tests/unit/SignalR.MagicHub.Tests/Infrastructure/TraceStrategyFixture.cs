using NUnit.Framework;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.Tests.Infrastructure
{
    [TestFixture]
    public class TraceStrategyFixture
    {
        private readonly TraceStrategy _traceStrategy = new TraceStrategy();

        [Test]
        public void Test_when_trace_message_flag_false_should_return_trace_strategy_false()
        {
            //Arrange   
            const string message = "{\"message\":\"blah\", \"tracing_enabled\":false}";

            //Act
            bool result = _traceStrategy.ShouldTraceMessage(message);

            //Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void Test_when_trace_message_true_false_should_return_trace_strategy_true()
        {
            //Arrange   
            const string message = "{\"message\":\"blah\", \"tracing_enabled\":true}";

            //Act
            bool result = _traceStrategy.ShouldTraceMessage(message);

            //Assert
            Assert.That(result, Is.True);
        }
    }
}
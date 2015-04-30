using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SignalR.MagicHub.Messaging;
using SignalR.MagicHub.Messaging.Filters;
using IMessageBus = Microsoft.AspNet.SignalR.Messaging.IMessageBus;

namespace SignalR.MagicHub.Tests.Messaging
{
    [TestFixture]
    public class MessageDispatcherFixture
    {
        private MessageDispatcher _dispatcher;
        private Mock<IMagicHubFilteringService> _mockFilteringService;
        private Mock<ITraceManager> _mockTraceManager;

        [SetUp]
        public void Setup()
        {
            _mockFilteringService = new Mock<IMagicHubFilteringService>();
            _mockTraceManager = new Mock<ITraceManager>();
            _mockTraceManager.SetupGet(t => t[It.IsAny<string>()]).Returns(new TraceSource("name"));
            _dispatcher = new MessageDispatcher(_mockFilteringService.Object, _mockTraceManager.Object);

        }
        [Test]
        public void Test_subscribe_doesnt_throw()
        {
            // Arrange
            TestDelegate subscribeAction = () =>_dispatcher.Subscribe(new SubscriptionIdentifier("foo", "B=1"),
                (topic, filter, message) => { });
            // Assert
            Assert.That(subscribeAction, Throws.Nothing);
        }

        [Test]
        public void Test_unsubscribe_doesnt_throw()
        {
            // Arrange
            TestDelegate unsubscribeAction = () => _dispatcher.Unsubscribe(new SubscriptionIdentifier("foo", "B=1"));
            
            // Assert
            Assert.That(unsubscribeAction, Throws.Nothing);
        }

        [Test]
        public async void Test_dispatch_dispatches()
        {
            // Arrange
            var subscription = new SubscriptionIdentifier("foo", "B=1");
            MessageBusCallbackDelegate emptyDelegate = (topic, filter, message) => { };
            MessageBusCallbackDelegate testCallbackDelegate = (topic, filter, message) =>
            {
                Assert.Pass("Callback successfully called");
            };
            var mockMessage = new Mock<IMagicHubMessage>();
            _mockFilteringService
                .Setup(f =>
                    f.Filter(
                        It.IsAny<IReadOnlyDictionary<string, object>>(),
                        It.IsAny<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>>()))
                .Returns(Task.FromResult<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>>( new[]
                {
                    new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(
                        subscription,
                        testCallbackDelegate)
                }));

            _dispatcher.Subscribe(subscription, emptyDelegate);
            
            // Act
            await _dispatcher.DispatchMessage(mockMessage.Object);
        }

        [Test]
        public void Test_when_concurrent_subscribe_on_same_topic_should_callback_once()
        {
            //Arrange
            var count = 0;
            var manualResets = Enumerable.Range(0, 10).Select(_ => new ManualResetEvent(false)).ToArray();
            var subscription = new SubscriptionIdentifier("foo", null);

            var testCallbackDelegates = new MessageBusCallbackDelegate[10];
            for(int i =0; i < 10; i++)
            testCallbackDelegates[i]= (topic, filter, message) =>
            {
                Interlocked.Increment(ref count);
                manualResets[i].Set();
            };
            
            var mockMessage = new Mock<IMagicHubMessage>();
            _mockFilteringService
                .Setup(f =>
                    f.Filter(
                        It.IsAny<IReadOnlyDictionary<string, object>>(),
                        It.IsAny<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>>()))
                .Returns(Task.FromResult<IEnumerable<KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>>>(new[]
                {
                    new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(
                        subscription,
                        (topic, filter, message) => Interlocked.Increment(ref count))
                }));

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(i => _dispatcher.Subscribe(subscription, testCallbackDelegates[i]));

            _dispatcher.DispatchMessage(mockMessage.Object);
            WaitHandle.WaitAny(manualResets, 100);

            //Assert
            Assert.That(count, Is.EqualTo(1));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Messaging;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Tests.Messaging.Filters
{
    [TestFixture]
    public class MagicHubFilteringServiceFixture
    {
        private Mock<IFilterExpressionFactory> _mockFilterFactory;
        private MagicHubFilteringService _filteringService;
        private Mock<IReadOnlyDictionary<string, object>> _messageContextMock;
        private Mock<ITraceManager> _mockTraceManager;
        private Mock<TraceListener> _mockTraceListener;

        [SetUp]
        public void Setup()
        {
            _mockTraceManager = new Mock<ITraceManager>();
            _mockTraceListener = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(_mockTraceListener.Object);
            _mockTraceManager.SetupGet((t) => t[AppConstants.SignalRMagicHub]).Returns(ts);
            _mockFilterFactory = new Mock<IFilterExpressionFactory>();
            _filteringService = new MagicHubFilteringService(_mockFilterFactory.Object, _mockTraceManager.Object);
            _messageContextMock = new Mock<IReadOnlyDictionary<string, object>>();
        }
        [Test]
        public async void Test_filter_matches()
        {
            // Arrange
            _mockFilterFactory.Setup(f => f.GetExpressionAsync("Topic = 'bar'"))
                .Returns(() => Task.FromResult(Mock.Of<IFilterExpression>(e => e.EvaluateAsync(It.IsAny<IReadOnlyDictionary<string, object>>()) == Task.FromResult<IComparable>(true))));

            var subscription = new SubscriptionIdentifier("bar", null);
            var subscriptions = new[]
            {
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscription,
                    (topic, filter, message) => { })
            };

            // Act
            var filterTask = await _filteringService.Filter(_messageContextMock.Object, subscriptions);

            // Assert
            Assert.That(filterTask.Any(p => p.Key == subscription), Is.True);

        }

        [Test]
        public async void Test_filter_doesnt_match()
        {
            // Arrange
            _mockFilterFactory.Setup(f => f.GetExpressionAsync("Topic = 'bar'"))
                .Returns(() => Task.FromResult(Mock.Of<IFilterExpression>(e => e.EvaluateAsync(It.IsAny<IReadOnlyDictionary<string, object>>()) == Task.FromResult<IComparable>(false))));
            var subscription = new SubscriptionIdentifier("bar", null);
            var subscriptions = new[]
            {
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscription,
                    (topic, filter, message) => { })
            };
            // Act
            var filterTask = await _filteringService.Filter(_messageContextMock.Object, subscriptions);

            // Assert
            Assert.That(filterTask.Any(p => p.Key == subscription), Is.False);
        }

        /// <summary>
        /// Relies on evaluation, but making sure .Filter does return what was evaluation to true.
        /// </summary>
        [Test]
        public async void Test_filter_matches_one_of_two()
        {
            
            // Arrange
            _mockFilterFactory.Setup(f => f.GetExpressionAsync("Topic = 'topic0' and A"))
                .Returns(() => Task.FromResult(Mock.Of<IFilterExpression>(e => e.EvaluateAsync(It.IsAny<IReadOnlyDictionary<string, object>>()) == Task.FromResult<IComparable>(true))));
            _mockFilterFactory.Setup(f => f.GetExpressionAsync("Topic = 'topic0' and B"))
                .Returns(() => Task.FromResult(Mock.Of<IFilterExpression>(e => e.EvaluateAsync(It.IsAny<IReadOnlyDictionary<string, object>>()) == Task.FromResult<IComparable>(false))));

            var subscriptionFoo = new SubscriptionIdentifier("topic0", "A");
            var subscriptionOffice = new SubscriptionIdentifier("topic0", "B");

            var subscriptions = new[]
            {
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscriptionOffice,
                    (topic, filter, message) => Assert.Fail("You are not supposed to be here.")),
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscriptionFoo,
                    (topic, filter, message) => Assert.Fail("You are not supposed to be here."))
            };

            // Act
            var callbacks = await _filteringService.Filter(_messageContextMock.Object, subscriptions);

            //Assert
            Assert.That(callbacks.Count() == 1, Is.True);
            Assert.That(callbacks.Any(p => p.Key == subscriptionFoo), Is.True);
            Assert.That(callbacks.Any(p => p.Key == subscriptionOffice), Is.False);
        }

        [Test]
        public async void Test_logging_when_filter_fail()
        {
            var subscriptionFoo = new SubscriptionIdentifier("topic0", "A");
            var subscriptionOffice = new SubscriptionIdentifier("topic0", "B");
            _mockFilterFactory.Setup(f => f.GetExpressionAsync("Topic = 'topic0' and A"))
                .Returns(() => Task.FromResult(Mock.Of<IFilterExpression>(e => e.EvaluateAsync(It.IsAny<IReadOnlyDictionary<string, object>>()) == Task.FromResult<IComparable>(true))));
            var subscriptions = new[]
            {
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscriptionOffice,
                    (topic, filter, message) => Assert.Fail("You are not supposed to be here.")),
                new KeyValuePair<SubscriptionIdentifier, MessageBusCallbackDelegate>(subscriptionFoo,
                    (topic, filter, message) => Assert.Fail("You are not supposed to be here."))
            };

            // Act
            var callbacks = await _filteringService.Filter(_messageContextMock.Object, subscriptions);

            // Assert

            // test that we traced a message
            _mockTraceListener.Verify((t) => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()));

            // trace that even though we had an error, the callbacks which were tested successfully came back
            Assert.That(callbacks.Any(p => p.Key == subscriptionFoo), Is.True);


        }
    }



}

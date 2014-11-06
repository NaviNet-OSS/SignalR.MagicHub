using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Infrastructure;
using SignalR.MagicHub.Messaging;
using SignalR.MagicHub.SessionValidator;

namespace SignalR.MagicHub.Tests.Infrastructure
{
    [TestFixture]
    public class MessageHubFixture
    {
        public interface IMockGroup
        {
            void onmessage(string topic, string filter, string message);
        }
        private Mock<IMessageBus> _mockMessageBus;
        private Mock<IHubContext> _mockHubContext;
        private Mock<TraceListener> _mockTraceListener;
        private Mock<IGroupManager> _mockGroupManager;
        private Mock<IHubConnectionContext> _mockClientManager;
        private MessageHub _messageHub;
        private Mock<IConnectionManager> _mockConnectionManager;
        private Mock<ISessionValidatorService> _mockSessionvalidator;
        private Mock<ISessionMappings> _mockSessionMappings;

        [SetUp]
        public void Setup()
        {
            _mockMessageBus = new Mock<IMessageBus>();
            _mockHubContext = new Mock<IHubContext>();
            _mockGroupManager = new Mock<IGroupManager>();
            _mockClientManager = new Mock<IHubConnectionContext>();
            _mockHubContext.Setup(c => c.Groups).Returns(_mockGroupManager.Object);
            _mockHubContext.Setup(c => c.Clients).Returns(_mockClientManager.Object);
            _mockConnectionManager = new Mock<IConnectionManager>();

            _mockConnectionManager.Setup((cm) => cm.GetHubContext<TopicBroker>()).Returns(_mockHubContext.Object);
            _mockSessionMappings = new Mock<ISessionMappings>();
            _mockTraceListener = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(_mockTraceListener.Object);

            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(t => t[AppConstants.SignalRMagicHub]).Returns(ts);

            _mockSessionvalidator = new Mock<ISessionValidatorService>();

            _messageHub = new MessageHub(_mockMessageBus.Object, _mockConnectionManager.Object, traceManager.Object, _mockSessionvalidator.Object, _mockSessionMappings.Object);
        }

        [Test]
        public void Test_dispatch()
        {
            //Arrange
            _mockGroupManager.Setup(g => g.Add("123", "Topic = 'foo'"));
            _mockClientManager.Setup(c => c.Group("Topic = 'foo'")).Verifiable();

            _mockMessageBus.Setup(m => m.Subscribe("foo", null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty)
                           .Callback<string, string, MessageBusCallbackDelegate>(
                               (topic, filter, callback) => callback(topic, null,
                                                                     "{\"message\":\"blah\"}"));

            //Act
            _messageHub.Subscribe("123", "foo").Wait();


            //Assert
            _mockGroupManager.VerifyAll();
            _mockClientManager.VerifyAll();
        }

        [Test]
        public void Test_handles_concurrent_connection_unsubscribe()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            Enumerable.Range(0, 10)
                      .AsParallel()
                      .ForAll(i => _messageHub.Subscribe(i.ToString(CultureInfo.InvariantCulture), "topic").Wait());
            _mockMessageBus.Setup(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.Empty);

            //Act
            Enumerable.Range(0, 10)
                      .AsParallel()
                      .ForAll(i => _messageHub.Unsubscribe(i.ToString(CultureInfo.InvariantCulture), "topic"));

            //Assert
            _mockGroupManager.Verify(m => m.Remove(It.IsAny<string>(), "Topic = 'topic'"), Times.Exactly(10));
        }

        [Test]
        public void Test_handles_concurrent_unsubscribe()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            _messageHub.Subscribe("123", "topic").Wait();
            _mockMessageBus.Setup(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.Empty);

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(_ => _messageHub.Unsubscribe("123", "topic"));

            //Assert
            _mockGroupManager.Verify(m => m.Remove("123", "Topic = 'topic'"), Times.Once());
        }

        [Test]
        public void Test_messagebus_unsubscribes()
        {
            // Arrange
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            _mockMessageBus.Setup(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.Empty)
                           .Verifiable();
            _messageHub.Subscribe("0", "topic").Wait();
            _messageHub.Subscribe("1", "topic").Wait();

            // Act
            _messageHub.Unsubscribe("0", "topic");
            _messageHub.Unsubscribe("1", "topic");

            // Assert
            _mockMessageBus.VerifyAll();
        }

        [Test]
        public void Test_publish_message_to_messagebus()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Publish(It.IsAny<string>(), It.IsAny<string>(), null)).Verifiable();

            //Act
            _messageHub.Publish("trace", "{foo:blah}");

            //Assert
            _mockMessageBus.VerifyAll();
        }

        [Test]
        public void Test_subscribe()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty)
                           .Verifiable();
            _mockGroupManager.Setup(g => g.Add("123", "Topic = 'topic'")).Verifiable();

            //Act
            _messageHub.Subscribe("123", "topic").Wait();

            //Assert
            _mockMessageBus.VerifyAll();
            _mockGroupManager.VerifyAll();
        }

        [Test]
        public void Test_that_subscription_filter_is_propagated()
        {
            // Arrange
            const string message = "{\"message\":\"blah\", \"tracing_enabled\":true}";
            _mockMessageBus
                .Setup(m => m.Subscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBusCallbackDelegate>()))
                .Returns(new Task(() => { }))
                .Callback<string, string, MessageBusCallbackDelegate>(
                    (topic, filter, callback) => callback(topic, null, message));
            // Act
            _messageHub.Subscribe("123", "footopic", "foo = 'bar'");

            // Assert
            _mockMessageBus.Verify(m => m.Subscribe("footopic", "foo = 'bar'", It.IsAny<MessageBusCallbackDelegate>()));
        }

        [Test]
        public void Test_unsubscribe()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            _mockGroupManager.Setup(g => g.Add("123", "Topic = 'topic'")).Verifiable();
            _mockGroupManager.Setup(g => g.Remove("123", "Topic = 'topic'")).Verifiable();
            _mockMessageBus.Setup(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.Empty);

            //Act
            _messageHub.Subscribe("123", "topic").Wait();
            _messageHub.Unsubscribe("123", "topic").Wait();

            //Assert
            _mockGroupManager.VerifyAll();
        }

        [Test]
        public void Test_unsubscribe_connection()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            _mockGroupManager.Setup(g => g.Add(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
            _messageHub.Subscribe("123", "topic").Wait();
            _messageHub.Subscribe("123", "topic2").Wait();

            //Act
            _messageHub.Unsubscribe("123");

            //Assert
            _mockGroupManager.Verify(g => g.Remove("123", It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Test_when_concurrent_subscribe_should_ignore_second()
        {
            //Arrange   
            _mockGroupManager.Setup(g => g.Add("123", "topic"));
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(new Task(() => { }))
                           .Verifiable();

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(_ => _messageHub.Subscribe("123", "topic"));

            //Assert
            _mockMessageBus.VerifyAll();
            _mockGroupManager.Verify(g => g.Add("123", "Topic = 'topic'"), Times.Once());
        }

        [Test]
        public void Test_when_duplicate_subscribe_should_ignore_second()
        {
            //Arrange   
            _mockGroupManager.Setup(g => g.Add("123", "topic"));
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty)
                           .Verifiable();

            //Act
            _messageHub.Subscribe("123", "topic").Wait();
            _messageHub.Subscribe("123", "topic").Wait();

            //Assert
            _mockMessageBus.VerifyAll();
            _mockGroupManager.Verify(g => g.Add("123", "Topic = 'topic'"), Times.Once());
        }

        [Test]
        public void Test_when_duplicate_unsubscribe_should_ignores_second()
        {
            //Arrange   
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            _mockGroupManager.Setup(g => g.Add("123", "topic")).Verifiable();
            _messageHub.Subscribe("123", "topic").Wait();
            _mockMessageBus.Setup(b => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.Empty);

            //Act
            _messageHub.Unsubscribe("123", "topic");
            _messageHub.Unsubscribe("123", "topic");

            //Assert
            _mockGroupManager.Verify(m => m.Remove("123", "Topic = 'topic'"), Times.Once());
        }

        [Test]
        public void Test_when_trace_message_flag_false_should_not_trace_message()
        {
            //Arrange   

            //Act
            _messageHub.Publish("trace", "{\"message\":\"blah\", \"tracing_enabled\":false}");

            //Assert
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.Never());
        }

        [Test]
        public void Test_unsubscribe_throws_and_traces_error_from_messagebus()
        {
            //Arrange   
            _mockMessageBus.Setup((b) => b.Unsubscribe(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(TaskAsyncHelper.FromError<ArgumentNullException>(new ArgumentNullException()));
            _mockMessageBus.Setup(b => b.Subscribe(It.IsAny<string>(), null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.Empty);
            //Act
            _messageHub.Subscribe("123", "foo").Wait();
            Assert.That(() => _messageHub.Unsubscribe("123", "foo").Wait(), Throws.TypeOf<AggregateException>());

            //Assert
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()));
        }

        [Test]
        public void Test_when_trace_message_flag_true_should_trace_message()
        {
            //Arrange   

            //Act
            _messageHub.Publish("trace", "{\"message\":\"blah\", \"tracing_enabled\":true}");

            //Assert
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                "Receiving message ({0}): {1}",
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_when_tracing_message_flag_false_should_not_log_when_picked_up_from_messagebus()
        {
            //Arrange
            _mockGroupManager.Setup(g => g.Add("123", "foo"));
            _mockClientManager.Setup(c => c.Group("foo")).Verifiable();

            _mockMessageBus.Setup(m => m.Subscribe("foo", null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(new Task(() => { }))
                           .Callback<string, string, MessageBusCallbackDelegate>(
                               (topic, filter, callback) => callback(topic, null,
                                                                     "{\"message\":\"blah\", \"tracing_enabled\":false}"));

            //Act
            _messageHub.Subscribe("123", "foo");

            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                "{\"message\":\"blah\", \"tracing_enabled\":true}",
                It.IsAny<object[]>()),
                                      Times.Never());
        }

        [Test]
        public void Test_when_tracing_message_flag_true_should_log_when_picked_up_from_messagebus()
        {
            //Arrange
            _mockGroupManager.Setup(g => g.Add("123", "foo"));
            const string message = "{\"message\":\"blah\", \"tracing_enabled\":true}";
            _mockMessageBus.Setup(m => m.Subscribe("foo", null, It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(new Task(() => { }))
                           .Callback<string, string, MessageBusCallbackDelegate>(
                               (topic, filter, callback) => callback(topic, null, message));

            //Act
            _messageHub.Subscribe("123", "foo");

            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                "Sending message ({0}): {1}", new object[] {"foo", message}),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_when_unsubscribe_called_before_subscribe_should_ignore()
        {
            //Arrange   

            //Act
            _messageHub.Unsubscribe("123", "topic");

            //Assert
            _mockGroupManager.Verify(m => m.Remove("123", "topic"), Times.Never());
        }

        [Test]
        public void Test_message_bus_throws_error_on_subscribe()
        {
            // Arrange
            _mockMessageBus.Setup(
                (b) => b.Subscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBusCallbackDelegate>()))
                           .Returns(TaskAsyncHelper.FromError<InvalidOperationException>(new ArgumentException()));
            AggregateException caughtException = null;
            // Act
            try
            {
                _messageHub.Subscribe("123", "abc").Wait();
            }
            catch (AggregateException thrownException)
            {
                caughtException = thrownException;
            }

            // Assert
            Assert.That(caughtException, Is.Not.Null);
            Assert.That(caughtException.Flatten().InnerException, Is.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Test_message_hub_ctor_without_session_validator_doesnt_throw()
        {
            Assert.That(() => new MessageHub(_mockMessageBus.Object, _mockConnectionManager.Object, Mock.Of<ITraceManager>(), null, Mock.Of<ISessionMappings>()), Throws.Nothing);
        }

        [Test]
        public void Test_session_validator_timing_out_event()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            var mockGroup1 = new Mock<IMockGroup>();
            var mockGroup2 = new Mock<IMockGroup>();
            _mockSessionMappings.Setup((m) => m.GetConnectionIds("foo")).Returns(new[] { "1", "2" });

            _mockClientManager.Setup(c => c.Client("1")).Returns(mockGroup1.Object);
            _mockClientManager.Setup(c => c.Client("2")).Returns(mockGroup2.Object);

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionExpiring += null, new SessionStateEventArgs(mockState));

            // Assert
            mockGroup1.Verify((c) => c.onmessage(TopicBroker.TopicSessionExpiring, "", "{\"expires_at\":\"0001-01-01T00:00:00\"}"));
            mockGroup2.Verify((c) => c.onmessage(TopicBroker.TopicSessionExpiring, "", "{\"expires_at\":\"0001-01-01T00:00:00\"}"));
        }

        [Test]
        public void Test_session_validator_timed_out_event()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            var mockGroup1 = new Mock<IMockGroup>();
            var mockGroup2 = new Mock<IMockGroup>();
            ICollection<string> associatedConnections = new[] { "1", "2" };
            _mockSessionMappings.Setup((m) => m.GetConnectionIds("foo")).Returns(associatedConnections);

            _mockSessionMappings.Setup((m) => m.TryRemoveAll("foo", out associatedConnections)).Returns(true);
            _mockClientManager.Setup(c => c.Client("1")).Returns(mockGroup1.Object);
            _mockClientManager.Setup(c => c.Client("2")).Returns(mockGroup2.Object);

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionExpired += null, new SessionStateEventArgs(mockState) { EventDetails = "Foo!"});

            // Assert
            mockGroup1.Verify((c) => c.onmessage(TopicBroker.TopicSessionExpired, "", "{\"status\":\"Foo!\"}"));
            mockGroup2.Verify((c) => c.onmessage(TopicBroker.TopicSessionExpired, "", "{\"status\":\"Foo!\"}"));
        }

        [Test]
        public void Test_session_validator_kept_alive_event()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            var mockGroup1 = new Mock<IMockGroup>();
            var mockGroup2 = new Mock<IMockGroup>();
            _mockSessionMappings.Setup((m) => m.GetConnectionIds("foo")).Returns(new[] {"1", "2"});

            _mockClientManager.Setup(c => c.Client("1")).Returns(mockGroup1.Object);
            _mockClientManager.Setup(c => c.Client("2")).Returns(mockGroup2.Object);

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionKeptAlive += null, new SessionStateEventArgs(mockState) { OperationSuccess = true});

            // Assert
            mockGroup1.Verify((c) => c.onmessage(TopicBroker.TopicSessionKeptAlive, "", "{\"status\":\"succeeded\"}"), Times.Once);
            mockGroup2.Verify((c) => c.onmessage(TopicBroker.TopicSessionKeptAlive, "", "{\"status\":\"succeeded\"}"), Times.Once);
        }

        [Test]
        public void Test_session_validator_expired_notifies_message_bus()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            ICollection<string> associatedConnections = new[] { "1", "2" };
            _mockSessionMappings.Setup((m) => m.TryRemoveAll("foo", out associatedConnections)).Returns(true);

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionExpired += null, new SessionStateEventArgs(mockState) { EventDetails = "Foo!" });
            
            // Assert
            _mockMessageBus.Verify((b) => b.Publish(TopicBroker.TopicSessionExpired, It.IsAny<string>()));
        }

        [Test]
        public void Test_session_validator_expiring_notifies_message_bus()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            _mockSessionMappings.Setup((m) => m.GetConnectionIds("foo")).Returns(new[] { "1", "2" });

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionExpiring += null, new SessionStateEventArgs(mockState) { ExpiresAt = DateTime.Now });

            // Assert
            _mockMessageBus.Verify((b) => b.Publish(TopicBroker.TopicSessionExpiring, It.IsAny<string>()));
        }

        [Test]
        public void Test_session_validator_kept_alive_notifies_message_bus()
        {
            // Arrange
            var mockState = Mock.Of<ISessionState>((s) => s.SessionKey == "foo");
            _mockSessionMappings.Setup((m) => m.GetConnectionIds("foo")).Returns(new[] { "1", "2" });

            // Act
            _mockSessionvalidator.Raise((v) => v.SessionKeptAlive += null, new SessionStateEventArgs(mockState) { OperationSuccess = true });

            // Assert
            _mockMessageBus.Verify((b) => b.Publish(TopicBroker.TopicSessionKeptAlive, It.IsAny<string>()));
        }
    }
}
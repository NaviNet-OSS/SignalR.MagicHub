using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using Moq;
using NUnit.Framework;
using SignalR.ActiveMQ;
using SignalR.MagicHub.Messaging;

namespace SignalR.ActiveMq.Tests
{
    [TestFixture]
    public class ActiveMqMessageBusFixture
    {
        private const string LOCAL_MESSAGEBUS_URI = @"tcp://localhost:61616";

        private ActiveMqMessageBus _messageBus;
        private Mock<IMockDelegate> _mockCallback;
        private Mock<ISession> _mockSession;
        private Mock<IMessageReceiver> _mockReceiver;


        public interface IMockDelegate
        {
            void Test(string key, string filter, string message);
        }

        [SetUp]
        public void Setup()
        {
            _mockSession = new Mock<ISession>();
            
            _mockReceiver = new Mock<IMessageReceiver>();
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup((c) => c.CreateSession()).Returns(_mockSession.Object);
            mockConnectionFactory.Setup((f) => f.CreateConnection()).Returns(mockConnection.Object);
            _messageBus = new ActiveMqMessageBus(_mockSession.Object, mockConnectionFactory.Object, LOCAL_MESSAGEBUS_URI, _mockReceiver.Object);
            _mockCallback = new Mock<IMockDelegate>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_messageBus != null)
            {
                _messageBus.Dispose();
                _messageBus = null;
            }
        }

        [Test]
        public void Test_minimal_constructor()
        {
            
        }

        [Test]
        public void Test_subscription_with_mock()
        {
            // Act
            _messageBus.Subscribe("foo", "Field1 = 'Bar'", _mockCallback.Object.Test);

            // Assert
            _mockReceiver.Verify((r) => r.TopicSubscribed("foo", "Field1 = 'Bar'", _mockCallback.Object.Test));
        }

        [Test]
        public void Test_subscription_with_selector_matches_message_with_filter()
        {
            //Arrange
            var mockProducer = new Mock<IMessageProducer>();
            mockProducer.Setup((p) => p.CreateTextMessage(It.IsAny<string>()))
                        .Returns<string> ((value) => new ActiveMQTextMessage(value));
            _mockSession.Setup((s) => s.GetTopic(It.IsAny<string>())).Returns(Mock.Of<ITopic>());
            _mockSession.Setup((s) => s.CreateProducer(It.IsAny<ITopic>())).Returns(mockProducer.Object);

            //Act
            _messageBus.Publish(
                "foo-topic",    // topic
                "bar",          // message
                new Dictionary<string, object>() // filter
                {
                    { "foo", 1 }
                });

            //Assert
            
            mockProducer.Verify((p) => p.CreateTextMessage(It.Is<string>(s => s == "bar")));
            mockProducer.Verify((p) => p.Send(It.Is<IMessage>((m) => 
                m.Properties["Topic"].ToString() == "foo-topic" && (int)m.Properties["foo"] == 1)));

        }

        [Test]
        public void Test_publish_without_filter()
        {
            var mockProducer = new Mock<IMessageProducer>();
            mockProducer.Setup((p) => p.CreateTextMessage(It.IsAny<string>()))
                        .Returns<string>((value) => new ActiveMQTextMessage(value));
            _mockSession.Setup((s) => s.GetTopic(It.IsAny<string>())).Returns(Mock.Of<ITopic>());
            _mockSession.Setup((s) => s.CreateProducer(It.IsAny<ITopic>())).Returns(mockProducer.Object);

            //Act
            _messageBus.Publish(
                "foo-topic",    // topic
                "bar");

            //Assert

            mockProducer.Verify((p) => p.CreateTextMessage(It.Is<string>(s => s == "bar")));
            mockProducer.Verify((p) => p.Send(It.Is<IMessage>((m) => m is ITextMessage && ((ITextMessage)m).Text == "bar")));
        }

        [Test]
        public void Test_publish_ioexception()
        {
            // Arrange
            bool hasFailed = false;
            var mockProducer = new Mock<IMessageProducer>();
            mockProducer.Setup((p) => p.CreateTextMessage(It.IsAny<string>())).Returns<string>((value) =>
                {
                    if (hasFailed)
                    {
                        return new ActiveMQTextMessage(value);
                    }
                    hasFailed = true;
                    throw new Apache.NMS.ActiveMQ.IOException();
                });
            _mockSession.Setup((s) => s.CreateProducer(It.IsAny<ITopic>())).Returns(mockProducer.Object);
            _mockSession.Setup((s) => s.GetTopic(It.IsAny<string>())).Returns(Mock.Of<ITopic>());
            _mockSession.Setup((s) => s.CreateProducer(It.IsAny<ITopic>())).Returns(mockProducer.Object);

            // Act
            _messageBus.Publish("foo", "bar");

            // Assert
            _mockSession.Verify((s) => s.Dispose());
        }

        [Test]
        public void Test_unsubscribe()
        {
            _messageBus.Unsubscribe("foo");

            // Assert
            _mockReceiver.Verify((r) => r.TopicUnsubscribed("foo", null));
        }

        [Test]
        public void Test_subscribe_without_filter()
        {
            _messageBus.Subscribe("foo", _mockCallback.Object.Test);

            // Assert
            _mockReceiver.Verify((r) => r.TopicSubscribed("foo", null, _mockCallback.Object.Test));
        }

        [Test]
        public void Test_subscribe_when_receiver_throws()
        {
            // Arrange
            _mockReceiver.Setup(
                (r) => r.TopicSubscribed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBusCallbackDelegate>()))
                         .Throws<Exception>();
            // Act
            var retVal = _messageBus.Subscribe("foo", _mockCallback.Object.Test);

            // Assert
            Assert.That(() => retVal.Wait(), Throws.Exception);
        }

        [Test]
        public void Test_unsubscribe_when_receiver_throws()
        {
            // Arrange
            _mockReceiver.Setup(
                (r) => r.TopicUnsubscribed(It.IsAny<string>(), It.IsAny<string>())).Throws<Exception>();
            // Act
            var retVal = _messageBus.Unsubscribe("foo");

            // Assert
            Assert.That(() => retVal.Wait(), Throws.Exception);
        }
    }
}

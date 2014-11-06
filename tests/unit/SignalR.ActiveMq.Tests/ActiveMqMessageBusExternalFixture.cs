using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SignalR.ActiveMQ;
using SignalR.MagicHub.Messaging;

namespace SignalR.ActiveMq.Tests
{
    /// <summary>
    /// This fixture will eventually have tests which connect to a local AMQ instance
    /// </summary>
    [TestFixture]
    public class ActiveMqMessageBusExternalFixture
    {
        private const string LOCAL_MESSAGEBUS_URI = @"tcp://localhost:61616";

        private ActiveMqMessageBus _messageBus = null;
        private Mock<IMockDelegate> _mockCallback = null;


        public interface IMockDelegate
        {
            void Test(string key, string filter, string message);
        }

        [SetUp]
        public void Setup()
        {
            _messageBus = new ActiveMqMessageBus(LOCAL_MESSAGEBUS_URI, Mock.Of<IMessageReceiver>());
            _mockCallback = new Mock<IMockDelegate>();
        }

        public void Teardown()
        {
            if (_messageBus != null)
            {
                _messageBus.Dispose();
                _messageBus = null;
            }
        }

        [Test]
        [Explicit] //Need local ActiveMq
        public void Test_subscription()
        {
            //Arrange
            _messageBus.Subscribe("foo", null, _mockCallback.Object.Test);

            //Act
            _messageBus.Publish("foo", "Thank you for subscribing");

            //Assert
            _mockCallback.Verify(m => m.Test("foo", It.IsAny<string>(), "Thank you for subscribing"));
        }


        [Test]
        [Explicit] //Need local ActiveMq
        public void Test_subscription_with_selector_matches_message_with_filter()
        {
#pragma warning disable 219
            var v = false;
#pragma warning restore 219
            //Arrange
            _messageBus.Subscribe(
                "foo-topic",                // topic
                "foo = 1",                  // nms filter 
                (a, b, c) =>
                {
                    v = true;
                }); // callback

            //Act
            _messageBus.Publish(
                "foo-topic",    // topic
                "bar",          // message
                new Dictionary<string, object>() // filter
                {
                    { "foo", 1 }
                });

            //Assert
            _mockCallback.Verify(m =>
                m.Test(
                "foo-topic",  // topic
                "foo = 1",
                "bar"),       // message
                Times.Exactly(1));
        }

        [Test]
        [Explicit] //Need local ActiveMq
        public void Test_subscription_with_selector_doesnt_match_filter()
        {
            //Arrange
            _messageBus.Subscribe(
                "foo-topic",                // topic
                "foo = 1",                  // nms filter 
                _mockCallback.Object.Test); // callback

            //Act
            _messageBus.Publish(
                "foo-topic",    // topic
                "bar",          // message
                new Dictionary<string, object>() // filter
                {
                    { "foo", 2 } // this filter does not match
                });

            //Assert
            _mockCallback.Verify(m =>
                m.Test(
                It.IsAny<string>(),  // topic
                It.IsAny<string>(), // filter
                It.IsAny<string>()), // message

                Times.Exactly(0));
        }

        [Test]
        [Explicit] //Need local ActiveMq
        public void Test_subscription_with_selector_doesnt_match_no_filter()
        {
            //Arrange
            _messageBus.Subscribe("foo", "foo = 1", _mockCallback.Object.Test);

            //Act
            _messageBus.Publish("foo", "baz");

            //Assert
            _mockCallback.Verify(m => m.Test("foo", null, "baz"), Times.Exactly(0));
        }
    }
}

using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;
using IMessageBus = Microsoft.AspNet.SignalR.Messaging.IMessageBus;
using MessageBus = SignalR.MagicHub.Messaging.MessageBus;
using Microsoft.AspNet.SignalR.Json;


namespace SignalR.MagicHub.Tests.Messaging
{
    [TestFixture]
    public class MessageBusFixture
    {
        [Test]
        public void Test_concurrent_publish()
        {
            //Arrange
            var count = 0;
            var manualResets = Enumerable.Range(0, 10).Select(_ => new ManualResetEvent(false)).ToArray();
            var messageBus = new MessageBus();

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(i => messageBus.Subscribe("foo" + i, (key, filter, value) =>
                {
                    Interlocked.Increment(ref count);
                    manualResets[i].Set();
                }));
            Enumerable.Range(0, 10).AsParallel().ForAll(i => messageBus.Publish("foo" + i, "{\"message\":\"blah\"}"));
            WaitHandle.WaitAll(manualResets, 100);


            //Assert
            Assert.That(count, Is.EqualTo(10));
        }

        [Test]
        public void Test_publish()
        {
            //Arrange
            var mockSignalRMessageBus = new Mock<IMessageBus>();
            
            mockSignalRMessageBus.Setup(b => b.Publish(It.IsAny<Message>())).Verifiable();
            var messageBus = new MessageBus(mockSignalRMessageBus.Object, JsonSerializer.Create(new JsonSerializerSettings()));

            //Act
            messageBus.Publish("foo", "{\"message\":\"blah\", \"tracing_enabled\":false}");

            //Assert
            mockSignalRMessageBus.VerifyAll();
        }

        [Test]
        public void Test_subscribe()
        {
            //Arrange
            var iscallbackCalled = false;
            var waitEvent = new ManualResetEvent(false);
            var messageBus = new MessageBus();

            //Act
            messageBus.Subscribe("foo", (key, filter, value) =>
                {
                    iscallbackCalled = true;
                    waitEvent.Set();
                });
            messageBus.Publish("foo", "{\"message\":\"blah\"}");
            waitEvent.WaitOne(100);

            //Assert
            Assert.That(iscallbackCalled, Is.True);
        }

        [Test]
        public void Test_unsubscribe()
        {
            //Arrange
            var callbackCalledCount = 0;
            var waitEvent = new AutoResetEvent(false);
            var messageBus = new MessageBus();

            //Act
            messageBus.Subscribe("foo", (key, filter, value) =>
                {
                    Interlocked.Increment(ref callbackCalledCount);
                    waitEvent.Set();
                });
            messageBus.Publish("foo", "{\"message\":\"blah\"}");
            waitEvent.WaitOne(100);
            messageBus.Unsubscribe("foo");
            messageBus.Publish("foo", "{\"message\":\"blah blah blah\"}");
            waitEvent.WaitOne(100);
            Thread.Sleep(100);

            //Assert
            Assert.That(callbackCalledCount, Is.EqualTo(1));
        }

        [Test]
        public void Test_when_concurrent_subscribe_on_same_topic_should_callback_once()
        {
            //Arrange
            var count = 0;
            var manualResets = Enumerable.Range(0, 10).Select(_ => new ManualResetEvent(false)).ToArray();
            var messageBus = new MessageBus();

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(i => messageBus.Subscribe("foo", (key, filter, value) =>
                {
                    Interlocked.Increment(ref count);
                    manualResets[i].Set();
                }));

            messageBus.Publish("foo", "{\"message\":\"blah\"}");
            WaitHandle.WaitAny(manualResets, 100);

            //Assert
            Assert.That(count, Is.EqualTo(1));
        }

    }
}
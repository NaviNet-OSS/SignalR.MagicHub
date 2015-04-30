using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SignalR.MagicHub.Messaging;
using System.Linq;
using System.Threading;
using IMessageBus = Microsoft.AspNet.SignalR.Messaging.IMessageBus;
using MessageBus = SignalR.MagicHub.Messaging.MessageBus;


namespace SignalR.MagicHub.Tests.Messaging
{
    [TestFixture]
    public class MessageBusFixture
    {
        [Test]
        public void Test_publish()
        {
            //Arrange
            var mockSignalRMessageBus = new Mock<IMessageBus>();
            var mockDispatcher = new Mock<IMessageDispatcher>();

            mockSignalRMessageBus.Setup(b => b.Publish(It.IsAny<Message>())).Verifiable();
            var messageBus = new MessageBus(mockSignalRMessageBus.Object, mockDispatcher.Object, JsonSerializer.Create(new JsonSerializerSettings()));

            //Act
            messageBus.Publish("foo", "{\"message\":\"blah\", \"tracing_enabled\":false}");

            //Assert
            mockSignalRMessageBus.VerifyAll();
        }

        [Test]
        public void Test_concurrent_publish()
        {
            //Arrange
            var count = 0;
            var manualResets = Enumerable.Range(0, 10).Select(_ => new ManualResetEvent(false)).ToArray();

            var dispatcher = new Mock<IMessageDispatcher>();
            dispatcher.Setup(s => s.Subscribe(It.IsAny<SubscriptionIdentifier>(), It.IsAny<MessageBusCallbackDelegate>())).Callback(()=>{});
           
            var messageBus = new MessageBus(GlobalHost.DependencyResolver.Resolve<IMessageBus>(),
                dispatcher.Object, GlobalHost.DependencyResolver.Resolve<JsonSerializer>());

            //Act
            Enumerable.Range(0, 10).AsParallel().ForAll(i => messageBus.Subscribe("foo" + i, (key, filter, value) =>
                {
                    Interlocked.Increment(ref count);
                    manualResets[i].Set();
                }));
            Enumerable.Range(0, 10).AsParallel().ForAll(i => messageBus.Publish("foo" + i, "{\"message\":\"blah\"}"));
            WaitHandle.WaitAll(manualResets, 100);

            //Assert
            dispatcher.Verify(g => g.Subscribe(It.Is<SubscriptionIdentifier>(s => s.Topic.StartsWith("foo") && s.Filter == null), It.IsAny<MessageBusCallbackDelegate>()), Times.Exactly(10));
            dispatcher.Verify(g => g.DispatchMessage(It.IsAny<IMagicHubMessage>()), Times.Exactly(10));   
        }       

        [Test]
        public void Test_subscribe()
        {
            //Arrange                        
            var waitEvent = new ManualResetEvent(false);
            
            var callback = new MessageBusCallbackDelegate((key, fltr, value) =>
            {
                waitEvent.Set();
            });

            var dispatcher = new Mock<IMessageDispatcher>();
            dispatcher.Setup(s => s.Subscribe(It.IsAny<SubscriptionIdentifier>(), It.IsAny<MessageBusCallbackDelegate>())).Callback(()=>{});
           
            var messageBus = new MessageBus(GlobalHost.DependencyResolver.Resolve<IMessageBus>(),
                dispatcher.Object, GlobalHost.DependencyResolver.Resolve<JsonSerializer>());

            //Act
            messageBus.Subscribe("foo", callback);            
            messageBus.Publish("foo", "{\"message\":\"blah\"}");
            waitEvent.WaitOne(100);
            
            //Assert
            dispatcher.Verify(g => g.Subscribe(It.Is<SubscriptionIdentifier>(s => s.Topic == "foo" && s.Filter == null), callback), Times.Once());            
        }

        [Test]
        public void Test_unsubscribe()
        {
            //Arrange                        
            var waitEvent = new ManualResetEvent(false);

            var callback = new MessageBusCallbackDelegate((key, fltr, value) =>
            {
                waitEvent.Set();
            });

            var dispatcher = new Mock<IMessageDispatcher>();
            dispatcher.Setup(s => s.Subscribe(It.IsAny<SubscriptionIdentifier>(), It.IsAny<MessageBusCallbackDelegate>())).Callback(() => { });

            var messageBus = new MessageBus(GlobalHost.DependencyResolver.Resolve<IMessageBus>(),
                dispatcher.Object, GlobalHost.DependencyResolver.Resolve<JsonSerializer>());

            //Act
            messageBus.Subscribe("foo", callback);
            messageBus.Publish("foo", "{\"message\":\"blah\"}");
            messageBus.Unsubscribe("foo");
            waitEvent.WaitOne(100);

            //Assert
            dispatcher.Verify(g => g.Subscribe(It.Is<SubscriptionIdentifier>(s => s.Topic == "foo" && s.Filter == null), callback), Times.Once());
            dispatcher.Verify(g => g.DispatchMessage(It.Is<IMagicHubMessage>(m => (string)m.Context["Topic"] == "foo")), Times.Once());  
            dispatcher.Verify(g => g.Unsubscribe(It.Is<SubscriptionIdentifier>(s => s.Topic == "foo" && s.Filter == null)), Times.Once());                         
        }      

    }
}
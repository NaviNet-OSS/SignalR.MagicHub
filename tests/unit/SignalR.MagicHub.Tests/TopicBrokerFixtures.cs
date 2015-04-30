#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Infrastructure;
using SignalR.MagicHub.SessionValidator;

#endregion

namespace SignalR.MagicHub.Tests
{
    [TestFixture]
    public class TopicBrokerFixtures
    {
        [SetUp]
        public void Setup()
        {
            _mockTraceListener = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(_mockTraceListener.Object);

            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(t => t[AppConstants.SignalRMagicHub]).Returns(ts);

            _mockMessageHub = new Mock<IMessageHub>();
            _mockHubReleaser = new Mock<IHubReleaser>();
            _mockSessionStateProvider = new Mock<ISessionStateProvider>();
            _mockSessionMappings = new Mock<ISessionMappings>();
            _mockSessionValidatorService = new Mock<ISessionValidatorService>();
            var mockContext = new HubCallerContext(Mock.Of<IRequest>((x) => x.User == Mock.Of<IPrincipal>((y) => y.Identity == Mock.Of<IIdentity>())), "123");
            
            _topicBroker = new TopicBroker(
                traceManager.Object, 
                _mockMessageHub.Object, 
                _mockSessionValidatorService.Object, 
                _mockSessionStateProvider.Object, 
                _mockSessionMappings.Object, 
                _mockHubReleaser.Object)
            {
                Context = mockContext
            };
        }

        [TearDown]
        public void TearDown()
        {
            _mockMessageHub = null;
            _topicBroker = null;
        }

        private TopicBroker _topicBroker;
        private Mock<IMessageHub> _mockMessageHub;
        private Mock<TraceListener> _mockTraceListener;
        private Mock<IHubReleaser> _mockHubReleaser;
        private Mock<ISessionStateProvider> _mockSessionStateProvider;
        private Mock<ISessionMappings> _mockSessionMappings;
        private Mock<ISessionValidatorService> _mockSessionValidatorService;
        //[Test]
        //public void Test_disconnect_clients()
        //{
        //    //Arrange
        //    _topicBroker = new TopicBroker();
        //    var mockDynamic = new Mock<DynamicObject>();
        //    var mockClients = new Mock<HubConnectionContext>();
        //    mockClients.Setup(c => c.All).Returns(mockDynamic.Object);

        //    _topicBroker.Clients = mockClients.Object;

        //    //Act
        //    _topicBroker.DisconnectAll();

        //    //Assert
        //    mockClients.Verify(c => c.All,Times.Once());
        //}

        [Test]
        public void Test_default_constructor()
        {
            //Arrange
            GlobalHost.DependencyResolver.Register(typeof(IMessageHub), Mock.Of<IMessageHub>);
            GlobalHost.DependencyResolver.Register(typeof(ISessionValidatorService), Mock.Of<ISessionValidatorService>);
            GlobalHost.DependencyResolver.Register(typeof(ISessionStateProvider), Mock.Of<ISessionStateProvider>);
            GlobalHost.DependencyResolver.Register(typeof(ISessionMappings), Mock.Of<ISessionMappings>);

            //Act
            _topicBroker = new TopicBroker();

            //Assert
            Assert.That(_topicBroker, Is.Not.Null);
        }

        [Test]
        public void Test_ondisconnect()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Unsubscribe(It.IsAny<string>())).Verifiable();

            //Act
            _topicBroker.OnDisconnected(true);

            //Assert
            _mockMessageHub.VerifyAll();
        }


        [Test]
        public void Test_send()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Publish(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(TaskAsyncHelper.Empty).Verifiable();

            //Act
            _topicBroker.Send("topic", "{foo:blah}").Wait();

            //Assert
            _mockMessageHub.VerifyAll();
        }

        [Test]
        public void Test_send_keepalive_calls_session_validator_service()
        {
            // Arrange
            _topicBroker.Context = new HubCallerContext(
                Mock.Of<IRequest>(r => r.Cookies == new Dictionary<string, Cookie>()
                {
                    {"goo", new Cookie("goo", "ga")}
                }), 
                "connection-id");

            // Act
            _topicBroker.Send("nnt:session/keep-alive", "foo").Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.KeepAlive(It.Is<IDictionary<string, string>>(d => d["goo"] == "ga")));
        }

        [Test]
        public void Test_send_keepalive_traces_error()
        {
            // Arrange
            var mockRequest = new Mock<IRequest>();
            mockRequest.SetupGet(r => r.Cookies).Throws<Exception>();

            _topicBroker.Context = new HubCallerContext(mockRequest.Object, "connection-id");

            // Act
            Assert.That(() => _topicBroker.Send("nnt:session/keep-alive", "foo").Wait(), Throws.Exception);

            // Assert
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_subscription()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Subscribe(It.IsAny<string>(), It.IsAny<string>(), null)).Returns(TaskAsyncHelper.Empty).Verifiable();

            //Act
            _topicBroker.Subscribe("topic").Wait();

            //Assert
            _mockMessageHub.VerifyAll();
        }


        [Test]
        public void Test_unsubscribe()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Unsubscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(TaskAsyncHelper.Empty).Verifiable();

            //Act
            _topicBroker.Unsubscribe("topic").Wait();

            //Assert
            _mockMessageHub.VerifyAll();
        }

        [Test]
        public void Test_when_publish_to_messagebus_fails_should_log_error()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Publish(It.IsAny<string>(), It.IsAny<string>(), null))
                           .Throws<Exception>()
                           .Verifiable();

            //Act
            var task = _topicBroker.Send("topic", "{foo:blah}");
            Assert.That(new TestDelegate(task.Wait), Throws.Exception);

            //Assert
            _mockMessageHub.VerifyAll();
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_when_subscription_to_messagebus_fails_should_log_error()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Subscribe(It.IsAny<string>(), It.IsAny<string>(), null))
                           .Throws(new Exception())
                           .Verifiable();

            //Act
            var task = _topicBroker.Subscribe("topic");
            Assert.That(new TestDelegate(task.Wait), Throws.Exception);

            //Assert
            _mockMessageHub.VerifyAll();
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_when_unsubscribe_to_messagebus_fails_should_log_error()
        {
            //Arrange
            _mockMessageHub.Setup(m => m.Unsubscribe(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Throws(new Exception())
                           .Verifiable();

            //Act
            var task = _topicBroker.Unsubscribe("topic");
            Assert.That(new TestDelegate(task.Wait), Throws.Exception);

            //Assert
            _mockMessageHub.VerifyAll();
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));
        }

        [Test]
        public void Test_that_hub_releaser_is_called_on_dispose()
        {
            // Act 
            _topicBroker.Dispose();

            // Assert
            _mockHubReleaser.Verify(r => r.Release(_topicBroker));
        }


        [Test]
        public void Test_that_onconnected_tracks_session_when_authenticated()
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User)
                .Returns(Mock.Of<IPrincipal>(u =>
                    u.Identity.IsAuthenticated == true));
            requestMock
                .SetupGet(r => r.Cookies)
                .Returns(new Dictionary<string, Cookie>()
                {
                    { "foo", new Cookie("foo", "bar") }
                });
            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");
            _mockSessionStateProvider
                .Setup(s => 
                    s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                .Returns(sessionState);

            // Act
            _topicBroker.OnConnected().Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.AddTrackedSession(sessionState));
        }

        [Test]
        public void Test_that_onconnected_tracks_nothing_when_no_user()
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User);
            
            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");
            

            // Act
            _topicBroker.OnConnected().Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.AddTrackedSession(sessionState), Times.Never);

        }

        [Test]
        public void Test_that_onconnected_tracks_nothing_when_user_not_authenticated()
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User)
                .Returns(Mock.Of<IPrincipal>(u =>
                    u.Identity.IsAuthenticated == false));

            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");


            // Act
            _topicBroker.OnConnected().Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.AddTrackedSession(sessionState), Times.Never);

        }

        [Test]
        public void Test_that_ondisconnected_doesnt_untrack_when_no_user()
        {
            // Arrange
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User);

            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");


            // Act
            _topicBroker.OnDisconnected(true).Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.RemoveTrackedSession("mysessionkey"), Times.Never);

        }

        [Test]
        public void Test_that_ondisconnected_doesnt_untrack_when_user_not_authenticated()
        {
            // Arrange
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User)
                .Returns(Mock.Of<IPrincipal>(u =>
                    u.Identity.IsAuthenticated == false));

            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");


            // Act
            _topicBroker.OnDisconnected(true).Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.RemoveTrackedSession("mysessionkey"), Times.Never);

        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Test_that_ondisconnected_removes_tracked_session(bool stopCalled)
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User)
                .Returns(Mock.Of<IPrincipal>(u =>
                    u.Identity.IsAuthenticated == true));
            requestMock
                .SetupGet(r => r.Cookies)
                .Returns(new Dictionary<string, Cookie>()
                {
                    { "foo", new Cookie("foo", "bar") }
                });
            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");
            _mockSessionStateProvider
                .Setup(s =>
                    s.GetSessionKey(It.IsAny<IDictionary<string, string>>()))
                .Returns("mysessionkey");

            _mockSessionMappings
                .Setup(m => m.TryRemove("mysessionkey", "five"))
                .Returns(true);
            // Act
            _topicBroker.OnDisconnected(stopCalled).Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.RemoveTrackedSession("mysessionkey"));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Test_that_ondisconnected_doesnt_remove_tracked_session_when_it_isnt_mapped(bool stopCalled)
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User)
                .Returns(Mock.Of<IPrincipal>(u =>
                    u.Identity.IsAuthenticated == true));
            requestMock
                .SetupGet(r => r.Cookies)
                .Returns(new Dictionary<string, Cookie>()
                {
                    { "foo", new Cookie("foo", "bar") }
                });
            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");
            _mockSessionStateProvider
                .Setup(s =>
                    s.GetSessionKey(It.IsAny<IDictionary<string, string>>()))
                .Returns("mysessionkey");

            _mockSessionMappings
                .Setup(m => m.TryRemove("mysessionkey", "five"))
                .Returns(false);
            // Act
            _topicBroker.OnDisconnected(stopCalled).Wait();

            // Assert
            _mockSessionValidatorService.Verify(v => v.RemoveTrackedSession("mysessionkey"), Times.Never);
        }

        [Test]
        public void Test_that_on_reconnected_traces()
        {
            // Arrange
            var sessionState = Mock.Of<ISessionState>(ss => ss.SessionKey == "mysessionkey");
            var requestMock = new Mock<IRequest>();
            requestMock
                .SetupGet(r => r.User);

            _topicBroker.Context = new HubCallerContext(requestMock.Object, "five");

            // Act
            _topicBroker.OnReconnected();

            // Assert 
            _mockTraceListener.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Verbose,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object[]>()),
                                      Times.AtLeast(1));

        }
    }
}
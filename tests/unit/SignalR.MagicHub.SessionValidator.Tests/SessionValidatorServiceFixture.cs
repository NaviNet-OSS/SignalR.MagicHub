using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.SessionValidator.Tests
{
    [TestFixture]
    public class SessionValidatorServiceFixture
    {
        private class EventListener<T> where T : EventArgs
        {
            public List<T> ReceivedEvents = new List<T>();
            public void Handle(object sender, T e)
            {
                ReceivedEvents.Add(e);
            }
        }
        private const string sessionKey = "DA49769D-4503-481B-8B5E-5CB262C7695C";
        private SessionValidatorConfiguration _config;
        private SessionValidatorService _sessionValidator;
        private Mock<ISystemTime> _mockSystemTime;
        private Mock<IMessageHub> _mockMessageHub;
        private Mock<ITraceManager> _mockTraceManager;
        private Mock<TraceListener> _mockTraceListener;
        private const string UserName = "A/username";
        private DateTime _baseTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private Mock<ISessionStateProvider> _mockSessionStateProvider;

        [SetUp]
        public void Setup()
        {
            _config = new SessionValidatorConfiguration
                {
                    SessionValidatorRunFrequencySeconds = 1,
                    WarnUserSessionExpirationSeconds = 120
                };
            _mockSystemTime = new Mock<ISystemTime>();
            _mockSystemTime.SetupGet((x) => x.Now).Returns(_baseTime);

            _mockMessageHub = new Mock<IMessageHub>();

            _mockTraceListener = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(_mockTraceListener.Object);

            _mockSessionStateProvider = new Mock<ISessionStateProvider>();

            _mockTraceManager = new Mock<ITraceManager>();
            _mockTraceManager.SetupGet((t) => t[AppConstants.SignalRMagicHub]).Returns(ts);
            _sessionValidator = new SessionValidatorService(_config, _mockTraceManager.Object, _mockSessionStateProvider.Object)
                {
                    TimeProvider = _mockSystemTime.Object
                };
        }

        [TearDown]
        public void Teardown()
        {
            if (_sessionValidator.IsRunning)
            {
                _sessionValidator.Stop();
            }
        }
        [Test]
        public void Test_stop_throws_when_stopped()
        {
            Assert.That(() => _sessionValidator.Stop(), Throws.InvalidOperationException);
        }

        [Test]
        public void Test_start_throws_when_started()
        {
            _sessionValidator.Start();
            Assert.That(() => _sessionValidator.Start(), Throws.InvalidOperationException);
        }

        [Test]
        public void Test_warn_of_upcoming_expiration()
        {
            // Arrange 
            SessionStateEventArgs calledArgsTimingout = null, calledArgsExpired = null;
            _sessionValidator.SessionExpiring += (sender, args) => calledArgsTimingout = args;
            _sessionValidator.SessionExpired += (sender, args) => calledArgsExpired = args;
            var session = new SessionState(sessionKey, UserName, _baseTime.AddSeconds(5));
            _mockSessionStateProvider
                .Setup((x) => x.GetSessionState(It.IsAny<Dictionary<string, string>>()))
                .Returns(session);
            _mockSessionStateProvider
                .Setup((s) => s.GetSessionKey(It.IsAny<Dictionary<string, string>>()))
                .Returns(sessionKey);
            _sessionValidator.AddTrackedSession(session);
            _mockSessionStateProvider.Setup((x) => x.GetSessionState(sessionKey)).Returns(session);

            // Act
            _sessionValidator.Start();
            
            // Assert
            Assert.That(calledArgsExpired, Is.Null);
            Assert.That(calledArgsTimingout.SessionState.SessionKey, Is.EqualTo(sessionKey));
            Assert.That(calledArgsTimingout.ExpiresAt, Is.EqualTo(_baseTime.AddSeconds(5)));
        }

        [Test]
        public void Test_dont_warn_of_upcoming_expiration_when_nothing_expiring()
        {
            // Arrange
            SessionStateEventArgs calledArgs = null;
            _sessionValidator.SessionExpiring += (sender, args) => calledArgs = args;
            // Act
            _sessionValidator.Start();

            // Assert
            Assert.That(calledArgs, Is.Null);
        }

        [Test]
        public void Test_start_sets_is_running_and_traces()
        {
            // Act
            _sessionValidator.Start();

            // Assert
            Assert.That(_sessionValidator.IsRunning, Is.True);
            _mockTraceListener.Verify((t) => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                "SessionValidator Service starting.",
                It.IsAny<object[]>()));
        }

        [Test]
        public void Test_stop_sets_is_running()
        {
            // Act
            _sessionValidator.Start();
            _sessionValidator.Stop();

            // Assert
            Assert.That(_sessionValidator.IsRunning, Is.False);
            _mockTraceListener.Verify((t) => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Information,
                It.IsAny<int>(),
                "SessionValidator Service stopping.",
                It.IsAny<object[]>()));
        }

        [Test]
        public void Test_KeepAlive_with_cookies_calls_session_queue()
        {
            // Arrange 
            ISessionState session = new SessionState(sessionKey, UserName, _baseTime.AddHours(1));
            _mockSessionStateProvider.Setup((s) => s.GetSessionKey(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session.SessionKey);
            _sessionValidator.AddTrackedSession(session);
            _mockSessionStateProvider.Setup((x) => x.KeepAlive(sessionKey, ref session)).Returns(true);
            // advance time 30 mins
            _mockSystemTime.SetupGet((x) => x.Now).Returns(_baseTime.AddMinutes(30));


            // Act
            _sessionValidator.KeepAlive(new Dictionary<string, string>());

            // Assert
            _mockSessionStateProvider.Verify((x) => x.KeepAlive(sessionKey, ref session));
        }
        [Test]
        public void Test_KeepAlive_calls_session_queue()
        {
            // Arrange 
            ISessionState session = new SessionState(sessionKey, UserName, _baseTime.AddHours(1));
            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.AddTrackedSession(session);
            _mockSessionStateProvider.Setup((x) => x.KeepAlive(sessionKey, ref session)).Returns(true);
            // advance time 30 mins
            _mockSystemTime.SetupGet((x) => x.Now).Returns(_baseTime.AddMinutes(30));


            // Act
            _sessionValidator.KeepAlive(sessionKey);

            // Assert
            _mockSessionStateProvider.Verify((x) => x.KeepAlive(sessionKey, ref session));
        }

        [Test]
        public void Test_KeepAlive_raises_event()
        {
            // Arrange
            bool called = false;
            _sessionValidator.SessionKeptAlive += (sender, args) => called = true;
            _sessionValidator.AddTrackedSession(new SessionState("foo", "foo", DateTime.Now));
            // Act
            _sessionValidator.KeepAlive("foo");

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void Test_KeepAlive_ignores_bogus_session()
        {
            // Arrange
            bool called = false;
            _sessionValidator.SessionKeptAlive += (sender, args) => called = true;
            // Act
            _sessionValidator.KeepAlive("foo");

            // Assert
            Assert.That(called, Is.False);
        }

        [Test]
        public void Test_AddTrackedSession_calls_session_queue()
        {
            // Arrange
            var session = new SessionState(sessionKey, UserName, _baseTime);
            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            // Act
            _sessionValidator.AddTrackedSession(session);

            // Assert
            Assert.That(_sessionValidator.GetTrackedSessions().Any((s) => ReferenceEquals(s, session)), Is.True);
        }



        [Test]
        public void Test_RemoveTrackedSession_calls_session_queue()
        {
            // Arrange 
            var session = new SessionState(sessionKey, UserName, _baseTime);
            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<Dictionary<string, string>>()))
                                     .Returns(session);
            _mockSessionStateProvider.Setup((s) => s.GetSessionKey(It.IsAny<Dictionary<string, string>>()))
                                     .Returns(sessionKey);
            
            _sessionValidator.AddTrackedSession(session);

            // Act
            _sessionValidator.RemoveTrackedSession(sessionKey);

            // Assert
            Assert.That(_sessionValidator.GetTrackedSessions(), Is.Empty);
        }

        [Test]
        public void Test_KillSession_removes_tracked_session()
        {
            //Arrange
            var session = new SessionState(sessionKey, UserName, _baseTime.AddDays(1));
            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.AddTrackedSession(session);
            // Act
            _sessionValidator.KillSession(sessionKey, SessionEndingReason.ADMINISTRATIVE_LOGOUT);

            // Assert
            Assert.That(_sessionValidator.GetTrackedSessions(), Is.Empty);
        }

        [Test]
        public void Test_KillSession_sends_correct_message_for_admin_logout()
        {
            // Arrange 
            SessionStateEventArgs calledArgs = null;
            _sessionValidator.SessionExpired += (sender, args) => calledArgs = args;
            var session = new SessionState(sessionKey, UserName, _baseTime.AddDays(1));

            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.AddTrackedSession(session);

            // Act
            _sessionValidator.KillSession(sessionKey, SessionEndingReason.ADMINISTRATIVE_LOGOUT);

            // Assert
            Assert.That(calledArgs.SessionState.SessionKey, Is.EqualTo(sessionKey));
            Assert.That(calledArgs.EventDetails, Is.EqualTo("ADMINISTRATIVE_LOGOUT"));
        }

        [Test]
        public void Test_KillSession_sends_correct_message_for_expired()
        {
            // Arrange 
            SessionStateEventArgs calledArgs = null;
            var session = new SessionState(sessionKey, UserName, _baseTime.AddDays(1));

            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.AddTrackedSession(session);

            _sessionValidator.SessionExpired += (sender, args) => calledArgs = args;
            // Act
            _sessionValidator.KillSession(sessionKey, SessionEndingReason.EXPIRED);

            // Assert
            Assert.That(calledArgs.SessionState.SessionKey, Is.EqualTo(sessionKey));
            Assert.That(calledArgs.EventDetails, Is.EqualTo("EXPIRED"));
        }

        [Test]
        public void Test_KillSession_sends_correct_message_for_multiple_login()
        {
            // Arrange 
            SessionStateEventArgs calledArgs = null;
            var session = new SessionState(sessionKey, UserName, _baseTime.AddDays(1));

            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.SessionExpired += (sender, args) => calledArgs = args;
            _sessionValidator.AddTrackedSession(session);

            // Act
            _sessionValidator.KillSession(sessionKey, SessionEndingReason.MULTIPLE_LOGIN);

            // Assert
            Assert.That(calledArgs.SessionState.SessionKey, Is.EqualTo(sessionKey));
            Assert.That(calledArgs.EventDetails, Is.EqualTo("MULTIPLE_LOGIN"));
        }

        [Test]
        public void Test_CheckSessions_updates_session()
        {
            // Arrange
            var externalSession = new SessionState("foo", "person", _mockSystemTime.Object.Now.AddDays(1));
            _mockSessionStateProvider.Setup(s => s.GetSessionState("foo"))
                .Returns(externalSession);
            _sessionValidator.AddTrackedSession(new SessionState("foo", "person", _mockSystemTime.Object.Now));

            // Act
            _sessionValidator.Start();
            Task.Delay(1010).Wait(); // wait 1 seconds + buffer

            // Assert
            var sessions = _sessionValidator.GetTrackedSessions();
            Assert.That(sessions.Count(), Is.EqualTo(1));
            Assert.That(sessions.First().Expires, Is.EqualTo(externalSession.Expires));

        }

        [Test]
        public void Test_CheckSessions_runs_and_traces()
        {
            // Act
            _sessionValidator.Start();
            Task.Delay(1001).Wait(); // wait 1 seconds + buffer

            // Assert
            _mockTraceListener.Verify((t) =>
                t.TraceEvent(
                    It.IsAny<TraceEventCache>(),
                    It.IsAny<string>(),
                    TraceEventType.Verbose,
                    It.IsAny<int>(),
                    "SessionValidator checking sessions."),
                Times.Exactly(2));
        }

        [Test]
        public void Test_GetTrackedSessions()
        {
            // Arrange
            var session = new SessionState(sessionKey, UserName, _baseTime.AddDays(1));
            _mockSessionStateProvider.Setup((s) => s.GetSessionState(It.IsAny<IDictionary<string, string>>()))
                                     .Returns(session);
            _sessionValidator.AddTrackedSession(session);
            // Act
            var currentSessions =_sessionValidator.GetTrackedSessions();

            // Assert
            Assert.That(currentSessions.Count(), Is.EqualTo(1));
            Assert.That(currentSessions.Any((s) => ReferenceEquals(s, session)), Is.True);
        }

        [Test]
        public void Test_messagehub_session_started_event()
        {
            // Arrange
            var session = new SessionState(sessionKey, UserName,_mockSystemTime.Object.Now.AddSeconds(60));
            _mockSessionStateProvider.Setup((x) => x.GetSessionState(It.IsAny<IDictionary<string,string>>()))
                                     .Returns(session);

            // Act
            _sessionValidator.AddTrackedSession(session);

            // Assert
            Assert.That(_sessionValidator.GetTrackedSessions().Count(), Is.EqualTo(1));
            Assert.That(
                _sessionValidator.GetTrackedSessions()
                                 .Any(
                                     (s) =>
                                     s.SessionKey == sessionKey && s.Username == UserName ),
                Is.True);
        }

        [Test]
        public void Test_sessionvalidator_expires_session()
        {
            // Arrange
            var session = new SessionState(sessionKey, UserName, _baseTime.AddSeconds(1));
            _mockSessionStateProvider
                .Setup((x) => x.GetSessionState(sessionKey))
                .Returns(session);
            _sessionValidator.AddTrackedSession(session);
            SessionStateEventArgs calledEventArgs = null;
            _sessionValidator.SessionExpired += (sender, args) => calledEventArgs = args;

            // Act
            _sessionValidator.Start();
            _mockSystemTime.SetupGet((x) => x.Now).Returns(DateTime.Now); // simulate time passing
            Task.Delay(10001).Wait();

            // Assert
            Assert.That(calledEventArgs, Is.Not.Null);
            Assert.That(calledEventArgs.EventDetails, Is.EqualTo("EXPIRED"));
            Assert.That(calledEventArgs.SessionState.SessionKey, Is.EqualTo(sessionKey));

            Assert.That(_sessionValidator.GetTrackedSessions().FirstOrDefault((s) => s.SessionKey == sessionKey && s.Expires == _baseTime.AddHours(1)), Is.Null);
        }

        [Test]
        public void Test_sessionvalidator_doesnt_expire_session_when_external_session_updated()
        {
            // Arrange
            var notExpiringSessionState = new SessionState(sessionKey, UserName, _baseTime.AddHours(1));
            var expiringSessionState = new SessionState(sessionKey, UserName, _baseTime);
            _mockSessionStateProvider
                .Setup((s) => s.GetSessionState(sessionKey))
                .Returns(notExpiringSessionState);

            _sessionValidator.AddTrackedSession(notExpiringSessionState);
            
            bool eventCalled = false;
            _sessionValidator.SessionExpired += (sender, args) => eventCalled = true;
            // Act
            _sessionValidator.Start();

            // Assert
            Assert.That(eventCalled, Is.False); 
            Assert.That(_sessionValidator.GetTrackedSessions().FirstOrDefault((s) => s.SessionKey == sessionKey && s.Expires == _baseTime.AddHours(1)), Is.Not.Null);
        }

    }
}

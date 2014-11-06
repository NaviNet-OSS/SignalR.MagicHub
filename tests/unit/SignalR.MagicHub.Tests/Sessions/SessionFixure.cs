using System;
using NUnit.Framework;
using SignalR.MagicHub.SessionValidator;

namespace SignalR.MagicHub.Tests.Sessions
{
    [TestFixture]
    public class SessionFixure
    {
        private const string UserName = "A/username";

        [Test]
        public void Test_constructor()
        {
            // Arrange
            var now = new DateTime(1900, 1, 1);
            var session = new SessionState("foo", UserName, now);
            
            // Assert
            Assert.That(session.SessionKey, Is.EqualTo("foo"));
            Assert.That(session.Expires, Is.EqualTo(now));
            Assert.That(session.Username, Is.EqualTo(UserName));
        }
    }
}

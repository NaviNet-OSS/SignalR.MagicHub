using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using NUnit.Framework;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.SessionValidator.Tests
{
    [TestFixture]
    public class SessionMappingsFixture
    {
        [Test]
        public void Test_addorupdate_adds()
        {
            // Arrange
            var mapping = new SessionMappings();

            // Act
            mapping.AddOrUpdate("foo", "1");

            // Assert
            Assert.That(mapping.TryRemove("foo", "1"), Is.EqualTo(true));
        }

        [Test]
        public void Test_addorupdate_adds_concurrent()
        {
            // Arrange
            var mapping = new SessionMappings();

            // Act
            Enumerable.Range(0, 10).AsParallel().ForAll(_ => mapping.AddOrUpdate("foo", _.ToString()));


            // Assert
            Assert.That(mapping.GetConnectionIds("foo").Count, Is.EqualTo(10));
        }

        [Test]
        public void Test_addorupdate_updates()
        {
            // Arrange
            var mapping = new SessionMappings();

            // Act & Assert
            Assert.That(mapping.AddOrUpdate("foo", "1"), Is.EqualTo(true));
            Assert.That(mapping.AddOrUpdate("foo", "2"), Is.EqualTo(false));

        }

        [Test]
        public void Test_getconnectionids_for_token_that_doesnt_exist()
        {
            // Arrange
            var mapping = new SessionMappings();

            // Assert
            Assert.That(mapping.GetConnectionIds("foo").Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_tryremoveall_removes_all_for_token_but_not_others()
        {
            // Arrange
            var mapping = new SessionMappings();
            mapping.AddOrUpdate("foo", "1");
            mapping.AddOrUpdate("foo", "2");
            mapping.AddOrUpdate("bar", "3");

            // Act
            ICollection<string> removed;
            mapping.TryRemoveAll("foo", out removed);

            // Assert
            Assert.That(removed, Contains.Item("1"));
            Assert.That(removed, Contains.Item("2"));
            Assert.That(mapping.GetConnectionIds("bar"), Contains.Item("3"));
        }

        [Test]
        public void Test_tryremove_bogus_token()
        {
            // Arrange
            var mapping = new SessionMappings();

            // Assert
            Assert.That(mapping.TryRemove("bogus", "1"), Is.False);
        }

        [Test]
        public void Test_tryremove_concurrent_removes_correctly()
        {
            // Arrange
            var mapping = new SessionMappings();
            mapping.AddOrUpdate("foo", "1");
            mapping.AddOrUpdate("foo", "2");
            mapping.AddOrUpdate("foo", "3");
            mapping.AddOrUpdate("foo", "4");
            mapping.AddOrUpdate("foo", "5");
            mapping.AddOrUpdate("foo", "6");
            mapping.AddOrUpdate("foo", "7");
            mapping.AddOrUpdate("bar", "a");
            mapping.AddOrUpdate("bar", "b");

            // Act
            Enumerable.Range(1, 7).AsParallel().ForAll(_ => mapping.TryRemove("foo", _.ToString()));

            // Assert
            Assert.That(mapping.GetConnectionIds("foo"), Is.Empty);
            Assert.That(mapping.GetConnectionIds("bar"), Contains.Item("a"));
            Assert.That(mapping.GetConnectionIds("bar"), Contains.Item("b"));


        }
    }
}

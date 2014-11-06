using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SignalR.MagicHub.Messaging;

namespace SignalR.MagicHub.Tests.Messaging
{
    [TestFixture]
    public class SubscriptionIdentifierFixture
    {
        [Test]
        public void Test_constructor_exception()
        {
            Assert.That(new TestDelegate(() => new SubscriptionIdentifier(null, "foo")),
                        Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void Test_properties_from_topic_filter_constructor()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("foo", "Bar = 'bar'");

            // Assert
            Assert.That(identifier.Topic, Is.EqualTo("foo"));
            Assert.That(identifier.Filter, Is.EqualTo("Bar = 'bar'"));
            Assert.That(identifier.Selector, Is.EqualTo("Topic = 'foo' and Bar = 'bar'"));
        }

        [Test]
        public void Test_properties_from_selector_constructor()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("Topic = 'foo' and Bar = 'bar'");

            // Assert
            Assert.That(identifier.Topic, Is.EqualTo("foo"));
            Assert.That(identifier.Filter, Is.EqualTo("Bar = 'bar'"));
            Assert.That(identifier.Selector, Is.EqualTo("Topic = 'foo' and Bar = 'bar'"));
        }

        [Test]
        public void Test_constructor_equality()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("Topic = 'foo' and Bar = 'bar'");
            var identifier2 = new SubscriptionIdentifier("foo", "Bar = 'bar'");


            // Assert
            Assert.That(identifier, Is.EqualTo(identifier2));
            Assert.That(identifier.GetHashCode(), Is.EqualTo(identifier2.GetHashCode()));
        }

        [Test]
        public void Test_constructor_equals_false_on_wrong_type()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("Topic = 'foo' and Bar = 'bar'");


            // Assert
            Assert.That(identifier, Is.Not.EqualTo("kittens"));
        }

        [Test]
        public void Test_tostring()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("Topic = 'foo' and Bar = 'bar'");

            // Assert
            Assert.That(identifier.ToString() == identifier.Selector);
        }
    }
}

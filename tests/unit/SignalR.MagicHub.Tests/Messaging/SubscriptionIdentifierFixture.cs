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
            string filter = @"{
                                                                    ""LHS"": ""Foo"",
                                                                    ""OP"": ""EQ"",
                                                                    ""RHS"": ""bar""        
                                                                  }";
            var identifier = new SubscriptionIdentifier("foo", filter);


            // Assert
            Assert.That(identifier.Topic, Is.EqualTo("foo"));
            Assert.That(identifier.Filter, Is.EqualTo(filter));
            Assert.That(identifier.Selector, Is.EqualTo("Topic = 'foo' and " + filter));
        }

        [Test]
        public void Test_properties_from_selector_constructor()
        {
            // Arrange
            string filter = @"{
                                ""LHS"": ""Foo"",
                                ""OP"": ""EQ"",
                                ""RHS"": ""bar""        
                              }";
            var identifier = new SubscriptionIdentifier("foo", filter);

            // Assert
            Assert.That(identifier.Topic, Is.EqualTo("foo"));
            Assert.That(identifier.Filter, Is.EqualTo(filter));
            Assert.That(identifier.Selector, Is.EqualTo("Topic = 'foo' and " + filter));
        }

//        [Test]
//        public void Test_constructor_equality()
//        {
//            // Arrange
//            var identifier = new SubscriptionIdentifier(
//                @"{
//                    ""LHS"": {
//                        ""LHS"": ""Topic"",
//                        ""OP"": ""EQ"",
//                        ""RHS"": ""foo""        
//                    },
//                    ""OP"": ""AND"",
//                    "": {
//                        ""LHS"": ""Foo"",
//                        ""OP"": ""EQ"",
//                        ""RHS"": 5        
//                    }
//                }");
//            var identifier2 = new SubscriptionIdentifier("foo", 
//                @"{
//                    ""LHS"": ""Foo"",
//                    ""OP"": ""EQ"",
//                    ""RHS"": 5        
//                  }");


//            // Assert
//            Assert.That(identifier, Is.EqualTo(identifier2));
//            Assert.That(identifier.GetHashCode(), Is.EqualTo(identifier2.GetHashCode()));
//        }

        [Test]
        public void Test_that_equals_returns_false_for_kittens()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("foo", @"{
                                                                    ""LHS"": ""Foo"",
                                                                    ""OP"": ""EQ"",
                                                                    ""RHS"": 5        
                                                                  }");
            var kitten = "meow";

            // Assert
            Assert.That(identifier, Is.Not.EqualTo(kitten));
            // We have shown that a subscription identifier is not a kitten!
        }

        [Test]
        public void Test_constructor_equals_false_on_wrong_type()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("foo", @"{
                                                                    ""LHS"": ""Foo"",
                                                                    ""OP"": ""EQ"",
                                                                    ""RHS"": 5        
                                                                  }");


            // Assert
            Assert.That(identifier, Is.Not.EqualTo("kittens"));
        }

        [Test]
        public void Test_tostring()
        {
            // Arrange
            var identifier = new SubscriptionIdentifier("foo", @"{
                                                                    ""LHS"": ""Foo"",
                                                                    ""OP"": ""EQ"",
                                                                    ""RHS"": 5        
                                                                  }");

            // Assert
            Assert.That(identifier.ToString() == identifier.Selector);
        }
    }
}

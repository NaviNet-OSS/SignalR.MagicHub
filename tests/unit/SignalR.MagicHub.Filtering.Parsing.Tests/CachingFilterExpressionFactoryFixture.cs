using System;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SignalR.MagicHub.Filtering.Expressions;
using SignalR.MagicHub.Messaging.Filters;

namespace SignalR.MagicHub.Filtering.Parsing.Tests
{
    [TestFixture]
    public class CachingFilterExpressionFactoryFixture
    {
        [Test]
        public async void Test_Caching()
        {
            // Arrange
            var mockInnerFactory = new Mock<IFilterExpressionFactory>();
            mockInnerFactory.Setup(f => f.GetExpressionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<IFilterExpression>(new ConstantExpression("Foo")));
            var cache = new MemoryCache(typeof (CachingFilterExpressionFactory).Name);
            var factory = new CachingFilterExpressionFactory(mockInnerFactory.Object, cache);
            bool flag = false;
            var evt = new AutoResetEvent(false);
            factory.DefaultPolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromMilliseconds(1),
                RemovedCallback = arguments => 
                { 
                    flag = true;
                    evt.Set();
                }
            };
            // Act
            var d1 = DateTimeOffset.Now;
            await factory.GetExpressionAsync("foo");

            evt.WaitOne(cache.PollingInterval + cache.PollingInterval);
            
            var timeSpan = DateTimeOffset.Now - d1;
            
            Debug.WriteLine("CacheItem expired after {0}ms", timeSpan.Milliseconds);
            // Assert
            Assert.That(flag, Is.True);
        }

        [Test]
        public async void Test_that_factory_only_called_once()
        {
            // Arrange
            var mockInnerFactory = new Mock<IFilterExpressionFactory>();
            mockInnerFactory.Setup(f => f.GetExpressionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<IFilterExpression>(new ConstantExpression("Foo")));
            var cache = new MemoryCache(typeof(CachingFilterExpressionFactory).Name);
            var factory = new CachingFilterExpressionFactory(mockInnerFactory.Object, cache);

            // Act
            var exp1 = await factory.GetExpressionAsync("Foo = 1");
            var exp2 = await factory.GetExpressionAsync("Foo = 1");

            // Assert
            Assert.That(exp1, Is.SameAs(exp2));
            mockInnerFactory.Verify(f => f.GetExpressionAsync("Foo = 1"), Times.Exactly(1));
        }
    }
}

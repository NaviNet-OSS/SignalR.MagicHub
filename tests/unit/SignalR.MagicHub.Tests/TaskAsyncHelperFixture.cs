using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SignalR.MagicHub.Infrastructure;

namespace SignalR.MagicHub.Tests
{
    [TestFixture]
    public class TaskAsyncHelperFixture
    {
        [Test]
        public void Test_Empty_task()
        {
            // Arrange
            var task = TaskAsyncHelper.Empty;

            // Assert
            Assert.That(task, Is.Not.Null);
        }

        [Test]
        public void Test_True_task()
        {
            // Arrange
            var task = TaskAsyncHelper.True;

            // Assert
            Assert.That(task.Result, Is.True);
        }

        [Test]
        public void Test_FromResult()
        {
            // Arrange
            var task = Task.FromResult(42);

            // Assert
            Assert.That(task.Result, Is.EqualTo(42));
        }

        [Test]
        public void Test_FromError()
        {
            // Arrange
            var ex = new ArgumentNullException("foo");

            // Act
            var task = TaskAsyncHelper.FromError<ArgumentNullException>(ex);

            // Assert
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.EqualTo(ex));
        }

        [Test]
        public void Test_SetUnwrappedException_with_aggregate_exception_sets_inner_exception()
        {
            // Arrange
            var subEx1 = new ArgumentNullException("foo");
            var subEx2 = new InvalidOperationException("bar");
            var ex = new AggregateException(subEx1, subEx2);

            // Act
            var task = TaskAsyncHelper.FromError(ex);

            // Assert
            Assert.That(task.Exception.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(task.Exception.InnerExceptions, Contains.Item(subEx1));
            Assert.That(task.Exception.InnerExceptions, Contains.Item(subEx2));
        }
    }
}

using System;
using System.Diagnostics;
using Moq;
using NUnit.Framework;

namespace SignalR.MagicHub.Tests
{
    public class TraceSourceExtensionsFixture
    {
        [Test]
        public void Test_TraceError_without_optional_message()
        {
            var traceMock = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(traceMock.Object);

            Exception ex = new ArgumentException("foo");
            ts.TraceError(ex);

            traceMock.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                "{3} ExceptionType=\"{0}\" ExceptionMessage=\"{1}\" StackTrace={2}",
                new object[]
                    {
                        ex.GetType().FullName, ex.Message, ex.StackTrace, ""
                    }),
                             Times.AtLeast(1));
        }

        [Test]
        public void Test_TraceError_with_message()
        {
            var traceMock = new Mock<TraceListener>(MockBehavior.Loose);
            var ts = new TraceSource(AppConstants.SignalRMagicHub, SourceLevels.All);
            ts.Listeners.Add(traceMock.Object);

            Exception ex = new ArgumentException("foo");

            ts.TraceError(ex, "bar");

            traceMock.Verify(t => t.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                TraceEventType.Error,
                It.IsAny<int>(),
                "{3} ExceptionType=\"{0}\" ExceptionMessage=\"{1}\" StackTrace={2}",
                new object[]
                    {
                        ex.GetType().FullName, ex.Message, ex.StackTrace, "ErrorOccurred=\"bar\" "
                    }),
                             Times.Exactly(1));
        }
    }
}
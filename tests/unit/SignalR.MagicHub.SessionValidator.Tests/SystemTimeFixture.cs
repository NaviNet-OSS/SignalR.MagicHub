using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SignalR.MagicHub.SessionValidator.Tests
{
    [TestFixture]
    class SystemTimeFixture
    {
        [Test]
        public void Test_GetNow()
        {
            var expected = DateTime.Now;
            var actual = SystemTime.Current.Now;

            Assert.That(actual, Is.EqualTo(expected).Within(TimeSpan.FromMilliseconds(10)));
        }
    }
}

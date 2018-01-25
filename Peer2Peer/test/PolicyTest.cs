using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    [TestClass]
    public class PolicyTest
    {
        [TestMethod]
        public async Task Always()
        {
            var policy = new PolicyAlways<string>();
            Assert.IsTrue(await policy.IsAllowedAsync("foo"));
            Assert.IsFalse(await policy.IsNotAllowedAsync("foo"));
        }

        [TestMethod]
        public async Task Never()
        {
            var policy = new PolicyNever<string>();
            Assert.IsFalse(await policy.IsAllowedAsync("foo"));
            Assert.IsTrue(await policy.IsNotAllowedAsync("foo"));
        }
    }
}

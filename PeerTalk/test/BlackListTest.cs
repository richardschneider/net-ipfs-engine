using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class BlackListTest
    {
        [TestMethod]
        public async Task Allowed()
        {
            var policy = new BlackList<string>();
            policy.Add("c");
            policy.Add("d");
            Assert.IsTrue(await policy.IsAllowedAsync("a"));
            Assert.IsTrue(await policy.IsAllowedAsync("b"));
            Assert.IsFalse(await policy.IsAllowedAsync("c"));
            Assert.IsFalse(await policy.IsAllowedAsync("d"));
        }

        [TestMethod]
        public async Task NotAllowed()
        {
            var policy = new BlackList<string>();
            policy.Add("c");
            policy.Add("d");
            Assert.IsFalse(await policy.IsNotAllowedAsync("a"));
            Assert.IsFalse(await policy.IsNotAllowedAsync("b"));
            Assert.IsTrue(await policy.IsNotAllowedAsync("c"));
            Assert.IsTrue(await policy.IsNotAllowedAsync("d"));
        }

        [TestMethod]
        public async Task Empty()
        {
            var policy = new BlackList<string>();
            Assert.IsTrue(await policy.IsAllowedAsync("a"));
            Assert.IsFalse(await policy.IsNotAllowedAsync("a"));
        }
    }
}

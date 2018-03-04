using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class WhiteListTest
    {
        [TestMethod]
        public async Task Allowed()
        {
            var policy = new WhiteList<string>();
            policy.Add("a");
            policy.Add("b");
            Assert.IsTrue(await policy.IsAllowedAsync("a"));
            Assert.IsTrue(await policy.IsAllowedAsync("b"));
            Assert.IsFalse(await policy.IsAllowedAsync("c"));
            Assert.IsFalse(await policy.IsAllowedAsync("d"));
        }

        [TestMethod]
        public async Task NotAllowed()
        {
            var policy = new WhiteList<string>();
            policy.Add("a");
            policy.Add("b");
            Assert.IsFalse(await policy.IsNotAllowedAsync("a"));
            Assert.IsFalse(await policy.IsNotAllowedAsync("b"));
            Assert.IsTrue(await policy.IsNotAllowedAsync("c"));
            Assert.IsTrue(await policy.IsNotAllowedAsync("d"));
        }

        [TestMethod]
        public async Task Empty()
        {
            var policy = new WhiteList<string>();
            Assert.IsTrue(await policy.IsAllowedAsync("a"));
            Assert.IsFalse(await policy.IsNotAllowedAsync("a"));
        }
    }
}

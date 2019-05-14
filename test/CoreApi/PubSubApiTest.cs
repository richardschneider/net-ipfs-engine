using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class PubSubApiTest
    {

        [TestMethod]
        public void Api_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.PubSub);
        }

        [TestMethod]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid().ToString();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.AreEqual(0, peers.Length);
        }

        [TestMethod]
        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                Assert.IsTrue(topics.Length > 0);
                CollectionAssert.Contains(topics, topic);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        volatile int messageCount = 0;

        [TestMethod]
        public async Task Subscribe()
        {
            messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");

                await Task.Delay(100);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(100);
                Assert.AreEqual(messages.Length, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Multiple_Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();

            Action<IPublishedMessage> processMessage = (msg) =>
            {
                Interlocked.Increment(ref messageCount);
            };
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(100);
                Assert.AreEqual(messages.Length * 2, messageCount);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        volatile int messageCount1 = 0;

        [TestMethod]
        public async Task Unsubscribe()
        {
            messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount1);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");
                await Task.Delay(100);
                Assert.AreEqual(1, messageCount1);

                cs.Cancel();
                await ipfs.PubSub.PublishAsync(topic, "hello world!!!");
                await Task.Delay(100);
                Assert.AreEqual(1, messageCount1);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task Subscribe_BinaryMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, expected);

                await Task.Delay(100);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                await ipfs.StopAsync();
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Subscribe_StreamMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = Guid.NewGuid().ToString();
            var cs = new CancellationTokenSource();
            await ipfs.StartAsync();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSub.PublishAsync(topic, ms);

                await Task.Delay(100);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
                await ipfs.StopAsync();
            }
        }
    }
}

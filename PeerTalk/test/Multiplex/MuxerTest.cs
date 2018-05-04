using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    [TestClass]
    public class MuxerTest
    {
        [TestMethod]
        public void Defaults()
        {
            var muxer = new Muxer();
            Assert.AreEqual(false, muxer.Initiator);
            Assert.AreEqual(true, muxer.Receiver);
            Assert.AreEqual(0, muxer.NextStreamId);
        }

        [TestMethod]
        public void InitiatorReceiver()
        {
            var muxer = new Muxer { Initiator = true };
            Assert.AreEqual(true, muxer.Initiator);
            Assert.AreEqual(false, muxer.Receiver);
            Assert.AreEqual(1, muxer.NextStreamId);

            muxer.Receiver = true;
            Assert.AreEqual(false, muxer.Initiator);
            Assert.AreEqual(true, muxer.Receiver);
            Assert.AreEqual(2, muxer.NextStreamId);
        }

        [TestMethod]
        public async Task NewStream_Send()
        {
            var channel = new MemoryStream();
            var muxer = new Muxer { Channel = channel, Initiator = true };
            var nextId = muxer.NextStreamId;
            var stream = await muxer.CreateStreamAsync("foo");

            // Correct stream id is assigned.
            Assert.AreEqual(nextId, stream.Id);
            Assert.AreEqual(nextId + 2, muxer.NextStreamId);
            Assert.AreEqual("foo", stream.Name);

            // Substreams are managed.
            Assert.AreEqual(1, muxer.Substreams.Count);
            Assert.AreSame(stream, muxer.Substreams[stream.Id]);

            // NewStream message is sent.
            var msg = channel.ToArray();
            CollectionAssert.AreEqual(new byte[] { 0x08, 0x03, (byte)'f', (byte)'o', (byte)'o' }, msg);
        }

        [TestMethod]
        public async Task NewStream_Receive()
        {
            var channel = new MemoryStream();
            var muxer1 = new Muxer { Channel = channel, Initiator = true };
            var foo = await muxer1.CreateStreamAsync("foo");
            var bar = await muxer1.CreateStreamAsync("bar");

            channel.Position = 0;
            var muxer2 = new Muxer { Channel = channel };
            await muxer2.ProcessRequestsAsync();
            Assert.AreEqual(2, muxer2.Substreams.Count);
            Assert.AreEqual("foo", muxer2.Substreams[foo.Id].Name);
            Assert.AreEqual("bar", muxer2.Substreams[bar.Id].Name);
        }

        [TestMethod]
        public async Task AcquireWrite()
        {
            var muxer = new Muxer();
            var tasks = new List<Task<string>>
            {
                Task<string>.Run(async () =>
                {
                    using (await muxer.AcquireWriteAccessAsync())
                    {
                        await Task.Delay(100);
                    }
                    return "step 1";
                }),
                Task<string>.Run(async () =>
                {
                    using (await muxer.AcquireWriteAccessAsync())
                    {
                        await Task.Delay(50);
                    }
                    return "step 2";
                }),
            };

            var done = await Task.WhenAll(tasks);
            Assert.AreEqual("step 1", done[0]);
            Assert.AreEqual("step 2", done[1]);
        }
    }
}

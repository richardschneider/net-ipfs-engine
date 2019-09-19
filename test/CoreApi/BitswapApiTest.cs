﻿using Ipfs.Engine.BlockExchange;
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
    public class BitswapApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        IpfsEngine ipfsOther = TestFixture.IpfsOther;

        [TestMethod]
        public async Task Wants()
        {
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource();
                var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));
                Task wantTask = ipfs.Bitswap.GetAsync(block.Id, cts.Token);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("wanted block is missing");
                    await Task.Delay(100);
                    var w = await ipfs.Bitswap.WantsAsync();
                    if (w.Contains(block.Id))
                        break;
                }

                cts.Cancel();
                var wants = await ipfs.Bitswap.WantsAsync();
                CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
                Assert.IsTrue(wantTask.IsCanceled);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task Unwant()
        {
            await ipfs.StartAsync();
            try
            {
                var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block 2"));
                Task wantTask = ipfs.Bitswap.GetAsync(block.Id);

                var endTime = DateTime.Now.AddSeconds(10);
                while (true)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("wanted block is missing");
                    await Task.Delay(100);
                    var w = await ipfs.Bitswap.WantsAsync();
                    if (w.Contains(block.Id))
                        break;
                }

                await ipfs.Bitswap.UnwantAsync(block.Id);
                var wants = await ipfs.Bitswap.WantsAsync();
                CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
                Assert.IsTrue(wantTask.IsCanceled);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task OnConnect_Sends_WantList()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;
                Console.WriteLine($"this at {local.Addresses.First()}");
                Console.WriteLine($"othr at {remote.Addresses.First()}");

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
                var _ = ipfs.Block.GetAsync(cid);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    var wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    if (wants.Contains(cid))
                        return;
                    await Task.Delay(200);
                }

                Assert.Fail("want list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task Sends_Cancel_on_Unwant()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
                var cts = new CancellationTokenSource();
                var _ = ipfs.Block.GetAsync(cid, cts.Token);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                // Send the want list.
                var endTime = DateTime.Now.AddSeconds(10);
                IEnumerable<Cid> wants = null;
                while (DateTime.Now < endTime && (wants == null || !wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && wants.Contains(cid), "want list not sent");

                // Send the cancel list.
                //cts.Cancel();
                await ipfs.Bitswap.UnwantAsync(cid);
                endTime = DateTime.Now.AddSeconds(10);
                wants = null;
                while (DateTime.Now < endTime && (wants == null || wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && !wants.Contains(cid), "cancel list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task Sends_Cancel_on_Cancellation()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
                var cts = new CancellationTokenSource();
                var _ = ipfs.Block.GetAsync(cid, cts.Token);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                // Send the want list.
                var endTime = DateTime.Now.AddSeconds(10);
                IEnumerable<Cid> wants = null;
                while (DateTime.Now < endTime && (wants == null || !wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && wants.Contains(cid), "want list not sent");

                // Send the cancel list.
                cts.Cancel();
                endTime = DateTime.Now.AddSeconds(10);
                wants = null;
                while (DateTime.Now < endTime && (wants == null || wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && !wants.Contains(cid), "cancel list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task Sends_Cancel_on_Found()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;

                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
                var cts = new CancellationTokenSource();
                var _ = ipfs.Block.GetAsync(cid, cts.Token);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                // Send the want list.
                var endTime = DateTime.Now.AddSeconds(10);
                IEnumerable<Cid> wants = null;
                while (DateTime.Now < endTime && (wants == null || !wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && wants.Contains(cid), "want list not sent");

                // Send the cancel list.
                await ipfs.Block.PutAsync(data);
                endTime = DateTime.Now.AddSeconds(10);
                wants = null;
                while (DateTime.Now < endTime && (wants == null || wants.Contains(cid)))
                {
                    wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    await Task.Delay(200);
                }
                Assert.IsTrue(wants != null && !wants.Contains(cid), "cancel list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnConnect()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.IsFalse(getTask.IsCanceled, "task cancelled");
                Assert.IsFalse(getTask.IsFaulted, "task faulted");
                Assert.IsTrue(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.AreEqual(otherPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreNotEqual(0UL, ledger.DataReceived);
                Assert.AreEqual(0UL, ledger.DataSent);
                Assert.IsTrue(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnConnect_Bitswap1()
        {
            var originalProtocols = (await ipfs.BitswapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitswapService).Protocols;

            (await ipfs.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1 { Bitswap = (await ipfs.BitswapService) }
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap1 { Bitswap = (await ipfsOther.BitswapService) }
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.IsFalse(getTask.IsCanceled, "task cancelled");
                Assert.IsFalse(getTask.IsFaulted, "task faulted");
                Assert.IsTrue(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.AreEqual(otherPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreNotEqual(0UL, ledger.DataReceived);
                Assert.AreEqual(0UL, ledger.DataSent);
                Assert.IsTrue(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();

                (await ipfs.BitswapService).Protocols = originalProtocols;
                (await ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnConnect_Bitswap11()
        {
            var originalProtocols = (await ipfs.BitswapService).Protocols;
            var otherOriginalProtocols = (await ipfsOther.BitswapService).Protocols;

            (await ipfs.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 { Bitswap = (await ipfs.BitswapService) }
            };
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            (await ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 { Bitswap = (await ipfsOther.BitswapService) }
            };
            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
                var block = await getTask;

                Assert.IsFalse(getTask.IsCanceled, "task cancelled");
                Assert.IsFalse(getTask.IsFaulted, "task faulted");
                Assert.IsTrue(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);

                var otherPeer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(otherPeer);
                Assert.AreEqual(otherPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreNotEqual(0UL, ledger.DataReceived);
                Assert.AreEqual(0UL, ledger.DataSent);
                Assert.IsTrue(ledger.IsInDebt);

                // TODO: Timing issue here.  ipfsOther could have sent the block
                // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();

                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();

                (await ipfs.BitswapService).Protocols = originalProtocols;
                (await ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnRequest()
        {
            ipfs.Options.Discovery.DisableMdns = true;
            ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfs.StartAsync();

            ipfsOther.Options.Discovery.DisableMdns = true;
            ipfsOther.Options.Discovery.BootstrapPeers = new MultiAddress[0];
            await ipfsOther.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(10000);
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data, cancel:  cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cancel: cts.Token);

                var block = await ipfs.Block.GetAsync(cid, cancel: cts.Token);
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
                ipfs.Options.Discovery = new DiscoveryOptions();
                ipfsOther.Options.Discovery = new DiscoveryOptions();
            }
        }

        [TestMethod]
        public async Task GetsBlock_Cidv1()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data, "raw", "sha2-512");

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(3000);
                var block = await ipfs.Block.GetAsync(cid, cts.Token);
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task GetBlock_Timeout()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));

            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(300);
                ExceptionAssert.Throws<TaskCanceledException>(() =>
                {
                    var _ = ipfs.Bitswap.GetAsync(block.Id, cts.Token).Result;
                });

                Assert.AreEqual(0, (await ipfs.Bitswap.WantsAsync()).Count());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task PeerLedger()
        {
            await ipfs.StartAsync();
            try
            {
                var peer = await ipfsOther.LocalPeer;
                var ledger = await ipfs.Bitswap.LedgerAsync(peer);
                Assert.IsNotNull(ledger);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

    }
}

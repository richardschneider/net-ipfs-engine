using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    
    [TestClass]
    public class Dht1Test
    {
        Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        [TestMethod]
        public async Task SeedsRoutingTableFromSwarm()
        {
            var swarm = new Swarm { LocalPeer = self };
            var peer = await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                Assert.IsTrue(dht.RoutingTable.Contains(peer));
            }
            finally
            {
                await dht.StopAsync();
            }
        }

        [TestMethod]
        public async Task AddDiscoveredPeerToRoutingTable()
        {
            var swarm = new Swarm { LocalPeer = self };
            var dht = new Dht1 { Swarm = swarm };
            await dht.StartAsync();
            try
            {
                var peer = await swarm.RegisterPeerAsync("/ip4/127.0.0.1/tcp/4001/ipfs/QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h");
                Assert.IsTrue(dht.RoutingTable.Contains(peer));
            }
            finally
            {
                await dht.StopAsync();
            }
        }
    }
}

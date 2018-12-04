using Ipfs;
using PeerTalk;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace PeerTalkBenchmarks
{
    public class RegisterPeer
    {
        Swarm swarm = new Swarm();
        Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };
        Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
        };

        [Params(10, 20, 50)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            swarm.LocalPeer = self;
            var addresses = new MultiAddress[N];
            for (var i = 0; i < N; ++i)
            {
                addresses[i] = new MultiAddress($"/ip6/::1/tcp/{i + 4000}");
            }
            other.Addresses = addresses;
        }

        [Benchmark]
        public Peer Register() => swarm.RegisterPeer(other);
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    [TestClass]
    public class CertTest
    {
        [TestMethod]
        public async Task Create_Rsa()
        {
            var ipfs = TestFixture.Ipfs;
            var keychain = await ipfs.KeyChain();
            var key = await ipfs.Key.CreateAsync("alice", "rsa", 512);
            try
            {
                var cert = await keychain.CreateCertificateAsync("alice");
                File.WriteAllBytes(@"\tmp\alice-rsa.cer", cert);
            }
            finally
            {
                await ipfs.Key.RemoveAsync("alice");
            }
        }

        [TestMethod]
        public async Task Create_Secp256k1()
        {
            var ipfs = TestFixture.Ipfs;
            var keychain = await ipfs.KeyChain();
            var key = await ipfs.Key.CreateAsync("alice", "secp256k1", 0);
            try
            {
                var cert = await keychain.CreateCertificateAsync("alice");
                File.WriteAllBytes(@"\tmp\alice-secp256k1.cer", cert);
            }
            finally
            {
                await ipfs.Key.RemoveAsync("alice");
            }
        }

        [TestMethod]
        public async Task Create_Ed25519()
        {
            var ipfs = TestFixture.Ipfs;
            var keychain = await ipfs.KeyChain();
            var key = await ipfs.Key.CreateAsync("alice", "ed25519", 0);
            try
            {
                var cert = await keychain.CreateCertificateAsync("alice");
                File.WriteAllBytes(@"\tmp\alice-ed25519.cer", cert);
            }
            finally
            {
                await ipfs.Key.RemoveAsync("alice");
            }
        }

    }
}

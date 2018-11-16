using Common.Logging;
using Ipfs;
using Ipfs.Registry;
using Org.BouncyCastle.Security;
using PeerTalk.Cryptography;
using PeerTalk.Protocols;
using ProtoBuf;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.SecureCommunication
{
    /// <summary>
    ///   Creates a secure connection with a peer.
    /// </summary>
    public class Secio1 : IEncryptionProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Secio1));

        /// <inheritdoc />
        public string Name { get; } = "secio";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            await EncryptAsync(connection, cancel);
        }

        /// <inheritdoc />
        public async Task<Stream> EncryptAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            var stream = connection.Stream;
            var localPeer = connection.LocalPeer;
            connection.RemotePeer = connection.RemotePeer ?? new Peer();
            var remotePeer = connection.RemotePeer;

            // =============================================================================
            // step 1. Propose -- propose cipher suite + send pubkey + nonce
            var rng = new SecureRandom();
            var localNonce = new byte[16];
            rng.NextBytes(localNonce);
            var localProposal = new Secio1Propose
            {
                Nonce = localNonce,
                Exchanges = "P-256,P-384,P-521",
                Ciphers = "AES-256,AES-128",
                Hashes = "SHA256,SHA512",
                PublicKey = Convert.FromBase64String(localPeer.PublicKey)
            };

            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, localProposal, PrefixStyle.Fixed32BigEndian);
            await stream.FlushAsync();

            // =============================================================================
            // step 1.1 Identify -- get identity from their key
            var remoteProposal = ProtoBuf.Serializer.DeserializeWithLengthPrefix<Secio1Propose>(stream, PrefixStyle.Fixed32BigEndian);
            var remoteId = MultiHash.ComputeHash(remoteProposal.PublicKey, "sha2-256");
            if (remotePeer.Id == null)
            {
                remotePeer.Id = remoteId;
            }
            else if (remoteId != remotePeer.Id)
            {
                throw new Exception($"Expected peer '{remotePeer.Id}', got '{remoteId}'");
            }

            // =============================================================================
            // step 1.2 Selection -- select/agree on best encryption parameters
            // to determine order, use cmp(H(remote_pubkey||local_rand), H(local_pubkey||remote_rand)).
            //   oh1 := hashSha256(append(proposeIn.GetPubkey(), nonceOut...))
            //   oh2 := hashSha256(append(myPubKeyBytes, proposeIn.GetRand()...))
            //   order := bytes.Compare(oh1, oh2)
            byte[] oh1;
            byte[] oh2;
            using (var hasher = MultiHash.GetHashAlgorithm("sha2-256"))
            using (var ms = new MemoryStream())
            {
                ms.Write(remoteProposal.PublicKey, 0, remoteProposal.PublicKey.Length);
                ms.Write(localProposal.Nonce, 0, localProposal.Nonce.Length);
                ms.Position = 0;
                oh1 = hasher.ComputeHash(ms);
            }
            using (var hasher = MultiHash.GetHashAlgorithm("sha2-256"))
            using (var ms = new MemoryStream())
            {
                ms.Write(localProposal.PublicKey, 0, localProposal.PublicKey.Length);
                ms.Write(remoteProposal.Nonce, 0, remoteProposal.Nonce.Length);
                ms.Position = 0;
                oh2 = hasher.ComputeHash(ms);
            }
            int order = 0;
            for (int i = 0; order == 0 && i < oh1.Length; ++i)
            {
                order = oh1[i].CompareTo(oh2[i]);
            }
            if (order == 0)
                throw new Exception("Same keys and nonces; talking to self");
            var curveName = SelectBest(order, localProposal.Exchanges, remoteProposal.Exchanges);
            if (curveName == null)
                throw new Exception("Cannot agree on a key exchange.");

            var cipherName = SelectBest(order, localProposal.Ciphers, remoteProposal.Ciphers);
            if (cipherName == null)
                throw new Exception("Cannot agree on a chipher.");

            var hashName = SelectBest(order, localProposal.Hashes, remoteProposal.Hashes);
            if (hashName == null)
                throw new Exception("Cannot agree on a hash.");

            // =============================================================================
            // step 2. Exchange -- exchange (signed) ephemeral keys. verify signatures.

            // Generate EphemeralPubKey
            var localEphemeralKey = EphermalKey.Generate(curveName);
            var localEphemeralPublicKey = localEphemeralKey.PublicKeyBytes();

            // Send Exchange packet
            var localExchange = new Secio1Exchange();
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, localProposal);
                ProtoBuf.Serializer.Serialize(ms, remoteProposal);
                ms.Write(localEphemeralPublicKey, 0, localEphemeralPublicKey.Length);
                localExchange.Signature = connection.LocalPeerKey.Sign(ms.ToArray());
            }
            localExchange.EPublicKey = localEphemeralPublicKey;
            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, localExchange, PrefixStyle.Fixed32BigEndian);
            await stream.FlushAsync(cancel);

            // Receive their Exchange packet
            var remoteExchange = ProtoBuf.Serializer.DeserializeWithLengthPrefix<Secio1Exchange>(stream, PrefixStyle.Fixed32BigEndian);

            // =============================================================================
            // step 2.1. Verify -- verify their exchange packet is good.
            var remotePeerKey = Key.CreatePublicKeyFromIpfs(remoteProposal.PublicKey);
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, remoteProposal);
                ProtoBuf.Serializer.Serialize(ms, localProposal);
                ms.Write(remoteExchange.EPublicKey, 0, remoteExchange.EPublicKey.Length);
                remotePeerKey.Verify(ms.ToArray(), remoteExchange.Signature);
            }
            var remoteEphemeralKey = EphermalKey.CreatePublicKeyFromIpfs(curveName, remoteExchange.EPublicKey);

            // =============================================================================
            // step 2.2. Keys -- generate keys for mac + encryption
            var sharedSecret = localEphemeralKey.GenerateSharedSecret(remoteEphemeralKey);
            StretchedKey.Generate(cipherName, hashName, sharedSecret, out StretchedKey k1, out StretchedKey k2);
            if (order < 0)
            {
                StretchedKey tmp = k1;
                k1 = k2;
                k2 = tmp;
            }

            // =============================================================================
            // step 2.3. MAC + Cipher -- prepare MAC + cipher
            var secureStream = new Secio1Stream(stream, cipherName, hashName, k1, k2);

            // =============================================================================
            // step 3. Finish -- send expected message to verify encryption works (send local nonce)

            // Send thier nonce,
            await secureStream.WriteAsync(remoteProposal.Nonce, 0, remoteProposal.Nonce.Length, cancel);
            await secureStream.FlushAsync(cancel);

            // Receive our nonce.
            var verification = new byte[localNonce.Length];
            await secureStream.ReadAsync(verification, 0, verification.Length, cancel);
            if (!localNonce.SequenceEqual(verification))
            {
                throw new Exception($"SECIO verification message failure.");
            }

            log.Debug("Secure session established");

            // Fill in the remote peer
            remotePeer.PublicKey = Convert.ToBase64String(remoteProposal.PublicKey);

            // Set secure task done
            connection.Stream = secureStream;
            connection.SecurityEstablished.SetResult(true);
            return secureStream;
        }

        string SelectBest(int order, string local, string remote)
        {
            var first = order < 0 ? remote.Split(',') : local.Split(',');
            string[] second = order < 0 ? local.Split(',') : remote.Split(',');
            return first.FirstOrDefault(f => second.Contains(f));
        }
    }
}

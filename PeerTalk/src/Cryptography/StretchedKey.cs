using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;

namespace PeerTalk.Cryptography
{
    public class StretchedKey
    {
        public byte[] IV { get; set; }
        public byte[] MacKey { get; set; }
        public byte[] CipherKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherName"></param>
        /// <param name="hashName"></param>
        /// <param name="secret"></param>
        /// <param name="k1"></param>
        /// <param name="k2"></param>
        /// <remarks>
        ///   The is no spec for this.  Copied https://github.com/libp2p/go-libp2p-crypto/blob/0f79fbebcb64f746a636aba79ece0635ec5919e9/key.go#L183
        /// </remarks>
        public static void Generate(string cipherName, string hashName, byte[] secret, out StretchedKey k1, out StretchedKey k2)
        {
            int cipherKeySize;
            int ivSize;
            switch (cipherName)
            {
                case "AES-128":
                    ivSize = 16;
                    cipherKeySize = 16;
                    break;
                case "AES-256":
                    ivSize = 16;
                    cipherKeySize = 32;
                    break;
                case "Blowfish":
                    ivSize = 8;
                    cipherKeySize = 32;
                    break;
                default:
                    throw new NotSupportedException($"Cipher '{cipherName}' is not supported.");
            }
            var hmacKeySize = 20;
            var need = 2 * (ivSize + cipherKeySize + hmacKeySize);

            var hmac = new HMac(DigestUtilities.GetDigest(hashName));
            var kp = new KeyParameter(secret);
            var seed = Encoding.ASCII.GetBytes("key expansion");
            hmac.Init(kp);
            var a = new byte[hmac.GetMacSize()];
            var b = new byte[hmac.GetMacSize()];

            hmac.BlockUpdate(seed, 0, seed.Length);
            hmac.DoFinal(a, 0);

            int j = 0;
            var result = new byte[need];
            while (j < need)
            {
                hmac.Reset();
                hmac.BlockUpdate(a, 0, a.Length);
                hmac.BlockUpdate(seed, 0, seed.Length);
                hmac.DoFinal(b, 0);

                var todo = b.Length;
                if (j + todo > need)
                    todo = need - j;
                Buffer.BlockCopy(b, 0, result, j, todo);
                j += todo;

                hmac.Reset();
                hmac.BlockUpdate(a, 0, a.Length);
                hmac.DoFinal(a, 0);
            }

            int half = need / 2;
            k1 = new StretchedKey
            {
                IV = new byte[ivSize],
                CipherKey = new byte[cipherKeySize],
                MacKey = new byte[hmacKeySize]
            };
            Buffer.BlockCopy(result, 0, k1.IV, 0, ivSize);
            Buffer.BlockCopy(result, ivSize, k1.CipherKey, 0, cipherKeySize);
            Buffer.BlockCopy(result, ivSize + cipherKeySize, k1.MacKey, 0, hmacKeySize);

            k2 = new StretchedKey
            {
                IV = new byte[ivSize],
                CipherKey = new byte[cipherKeySize],
                MacKey = new byte[hmacKeySize]
            };
            Buffer.BlockCopy(result, half, k2.IV, 0, ivSize);
            Buffer.BlockCopy(result, half + ivSize, k2.CipherKey, 0, cipherKeySize);
            Buffer.BlockCopy(result, half + ivSize + cipherKeySize, k2.MacKey, 0, hmacKeySize);
        }
    }
}

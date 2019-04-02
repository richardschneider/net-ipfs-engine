using System;
using System.Runtime.InteropServices;
using System.Security;
namespace Ipfs.Engine.Cryptography
{
    /// <summary>
    ///   Extensions for a <see cref="SecureString"/>.
    /// </summary>
    public static class SecureStringExtensions
    {
        /// <summary>
        ///   Use the plain bytes of a <see cref="SecureString"/>.
        /// </summary>
        /// <param name="s">The secure string to access.</param>
        /// <param name="action">
        ///   A function to call with the plain bytes.
        /// </param>
        public static void UseSecretBytes(this SecureString s, Action<byte[]> action)
        {
            var length = s.Length;
            var p = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            var plain = new byte[length];
            try
            {
                Marshal.Copy(p, plain, 0, length);
                action(plain);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(p);
                p = IntPtr.Zero;
                Array.Clear(plain, 0, length);
            }
        }

    }
}

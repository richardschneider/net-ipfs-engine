using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   A cryptographic key.
    /// </summary>
    public class CryptoKeyDto
    {
        /// <summary>
        ///   The key's local name.
        /// </summary>
        public string Name;

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Id;
    }

    /// <summary>
    ///   A list of cryptographic keys.
    /// </summary>
    public class CryptoKeysDto
    {
        /// <summary>
        ///   A list of cryptographic keys.
        /// </summary>
        public IEnumerable<CryptoKeyDto> Keys;
    }

    /// <summary>
    ///   A cryptographic key.
    /// </summary>
    public class CryptoKeyRenameDto
    {
        /// <summary>
        ///   The key's local name.
        /// </summary>
        public string Was;

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Now;

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Id;

        /// <summary>
        ///   Indicates that a existing key was overwritten.
        /// </summary>
        public bool Overwrite;
    }

    /// <summary>
    ///   Manages the cryptographic keys.
    /// </summary>
    public class KeyController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public KeyController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   List all the keys.
        /// </summary>
        [HttpGet, HttpPost, Route("key/list")]
        public async Task<CryptoKeysDto> List()
        {
            var keys = await IpfsCore.Key.ListAsync(Cancel);
            return new CryptoKeysDto
            {
                Keys = keys.Select(key => new CryptoKeyDto
                {
                    Name = key.Name,
                    Id = key.Id.ToString()
                })
            };
        }

        /// <summary>
        ///   Create a new key.
        /// </summary>
        /// <param name="arg">
        ///   The name of the key.
        /// </param>
        /// <param name="type">
        ///   "rsa"
        /// </param>
        /// <param name="size">
        ///   The key size in bits, if the type requires it.
        /// </param>
        [HttpGet, HttpPost, Route("key/gen")]
        public async Task<CryptoKeyDto> Create(
            string arg,
            string type,
            int size)
        {
            if (String.IsNullOrWhiteSpace(arg))
                throw new ArgumentNullException("arg", "The key name is required.");
            if (String.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException("type", "The key type is required.");

            var key = await IpfsCore.Key.CreateAsync(arg, type, size, Cancel);
            return new CryptoKeyDto
            {
                Name = key.Name,
                Id = key.Id.ToString()
            };
        }

        /// <summary>
        ///   Remove a key.
        /// </summary>
        /// <param name="arg">
        ///   The name of the key.
        /// </param>
        [HttpGet, HttpPost, Route("key/rm")]
        public async Task<CryptoKeysDto> Remove(string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                throw new ArgumentNullException("arg", "The key name is required.");

            var key = await IpfsCore.Key.RemoveAsync(arg, Cancel);
            var dto = new CryptoKeysDto();
            if (key != null)
            {
                dto.Keys = new[] { new CryptoKeyDto
                {
                    Name = key.Name,
                    Id = key.Id.ToString()
                }};
            }

            return dto;
        }

        /// <summary>
        ///   Rename a key.
        /// </summary>
        /// <param name="arg">
        ///   The old and new key name.
        /// </param>
        [HttpGet, HttpPost, Route("key/rename")]
        public async Task<CryptoKeyRenameDto> Rename(string[] arg)
        {
            if (arg.Length != 2)
                throw new ArgumentException("Missing the old and/or new key name.");

            var key = await IpfsCore.Key.RenameAsync(arg[0], arg[1], Cancel);
            var dto = new CryptoKeyRenameDto
            {
                Was = arg[0],
                Now = arg[1],
                Id = key.Id.ToString()
                // TODO: Overwrite
            };
            return dto;
        }

        // TODO: import
        // TODO: export
    }
}

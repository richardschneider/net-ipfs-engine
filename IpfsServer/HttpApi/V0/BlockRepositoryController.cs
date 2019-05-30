using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   A wrapped version number.
    /// </summary>
    public class VersionBlockRepositoryDto
    {
        /// <summary>
        ///   The version number.
        /// </summary>
        public string Version;
    }

    /// <summary>
    ///    Manages all the blocks in teh repository.
    /// </summary>
    public class BlockRepositoryController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BlockRepositoryController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   Garbage collection.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/gc")]
        public Task GarbageCollection()
        {
            return IpfsCore.BlockRepository.RemoveGarbageAsync(Cancel);
        }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/stat")]
        public Task<RepositoryData> Statistics()
        {
            return IpfsCore.BlockRepository.StatisticsAsync(Cancel);
        }

        /// <summary>
        ///   Verify that the blocks are not corrupt.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/verify")]
        public Task Verify()
        {
            return IpfsCore.BlockRepository.VerifyAsync(Cancel);
        }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/version")]
        public async Task<VersionBlockRepositoryDto> Version()
        {
            return new VersionBlockRepositoryDto
            {
                Version = await IpfsCore.BlockRepository.VersionAsync(Cancel)
            };
        }
    }
}

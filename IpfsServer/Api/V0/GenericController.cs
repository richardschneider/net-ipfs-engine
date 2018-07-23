using Ipfs;
using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ipfs.Server.Api.V0
{
    [Route("api/v0/")]
    [Produces("application/json")]
    public class GenericController : Controller
    {
        ICoreApi ipfs;

        public GenericController(ICoreApi ipfs)
        {
            this.ipfs = ipfs;
        }

        [HttpGet, HttpPost, Route("id")]
        public async Task<PeerDto> Get()
        {
            var peer = await ipfs.Generic.IdAsync();
            return new PeerDto(peer);
        }

        [HttpGet, HttpPost, Route("version")]
        public async Task<Dictionary<string, string>> Version()
        {
            return await ipfs.Generic.VersionAsync();
        }

        [HttpGet(), HttpPost(), Route("resolve")]
        public async Task<PathDto> Resolve(string arg, bool recursive = false)
        {
            var path = await ipfs.Generic.ResolveAsync(arg, recursive);
            return new PathDto(path);
        }

        [HttpGet, HttpPost, Route("shutdown")]
        public async Task Shutdown()
        {
            await ipfs.Generic.ShutdownAsync();

            // TODO: return a response then shutdown the server
            Program.Shutdown();
        }

#if false
        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
#endif
    }
}

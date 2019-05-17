using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ipfs.Server.Pages
{
    /// <summary>
    ///   Not used.
    /// </summary>
    public class IndexModel : PageModel
    {
        ICoreApi ipfs;

        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        public IndexModel(ICoreApi ipfs)
        {
            this.ipfs = ipfs;
        }

        public string NodeId = "foo-bar";

        /// <summary>
        ///   Build the model.
        /// </summary>
        public async Task OnGetAsync()
        {
            var peer = await ipfs.Generic.IdAsync();
            NodeId = peer.Id.ToString();
        }
    }
}

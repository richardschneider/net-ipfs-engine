using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;

namespace Ipfs.Server.Pages
{
    /// <summary>
    ///   An IPFS file or directory.
    /// </summary>
    public class IpfsModel : PageModel
    {
        ICoreApi ipfs;
        IFileSystemNode node;

        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        /// <param name="ipfs">
        ///   An object that implements the ICoreApi, typically an IpfsEngine.
        /// </param>
        public IpfsModel(ICoreApi ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///   The IPFS path to a file or directry.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Path { get; set; }
        
        /// <summary>
        ///   The parts of the <see cref="Path"/>.
        /// </summary>
        public IEnumerable<string> PathParts
        {
            get
            {
                return Path
                    .Split('/')
                    .Where(p => !String.IsNullOrWhiteSpace(p));
            }
        }

        /// <summary>
        ///   The parent path.
        /// </summary>
        public string Parent
        {
            get
            {
                var parts = PathParts.ToList();
                parts.RemoveAt(parts.Count - 1);
                return String.Join('/', parts);
            }
        }

        /// <summary>
        ///   A sequence of files for the directory.
        /// </summary>
        public IEnumerable<IFileSystemLink> Files
        {
            get
            {
                return node.Links.Where(l => !l.IsDirectory);
            }
        }

        /// <summary>
        ///   A sequence of sub-directories for the directory.
        /// </summary>
        public IEnumerable<IFileSystemLink> Directories
        {
            get
            {
                return node.Links.Where(l => l.IsDirectory);
            }
        }
        
        /// <summary>
        ///   Get the file or directory.
        /// </summary>
        /// <remarks>
        ///   Returns the contents of the file or a page listing the directory.
        /// </remarks>
        public async Task<IActionResult> OnGetAsync(CancellationToken cancel)
        {
            if (String.IsNullOrWhiteSpace(Path))
            {
                return NotFound();
            }

            try
            {
                node = await ipfs.FileSystem.ListFileAsync(Path, cancel);
            }
            catch (ArgumentException e)
            {
                return NotFound(e.Message);
            }

            // If a directory, then display a page with directory contents or if
            // the directory contain "index.html", redirect to the page.
            if (node.IsDirectory)
            {
                if (!Path.EndsWith("/") && node.Links.Any(l => l.Name == "index.html"))
                {
                    return Redirect($"/ipfs/{Path}/index.html");
                }

                Path = Path.TrimEnd('/');
                return Page();
            }

            // If a file, send it.
            var etag = new EntityTagHeaderValue("\"" + node.Id + "\"", isWeak: false);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(Path, out string contentType))
            {
                contentType = "application/octet-stream";
            }
            var stream = await ipfs.FileSystem.ReadFileAsync(node.Id, cancel);
            Response.Headers.Add("cache-control", new StringValues("public, max-age=31536000, immutable"));
            Response.Headers.Add("etag", new StringValues(etag.Tag));
            return File(stream, contentType);
        }
    }
}

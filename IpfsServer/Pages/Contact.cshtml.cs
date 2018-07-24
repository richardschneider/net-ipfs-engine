using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ipfs.Server.Pages
{
    /// <summary>
    ///   Contact details.
    /// </summary>
    public class ContactModel : PageModel
    {
        /// <summary>
        ///   Message to display.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///   Build the model.
        /// </summary>
        public void OnGet()
        {
            Message = "Your contact page.";
        }
    }
}

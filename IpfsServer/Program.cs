using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ipfs.Server
{
    /// <summary>
    ///   Manages an IPFS server.
    /// </summary>
    class Program
    {
        static CancellationTokenSource cancel = new CancellationTokenSource();

        /// <summary>
        ///   Main entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            try
            {
                BuildWebHost(args)
                    .RunAsync(cancel.Token)
                    .Wait();
            }
            catch (TaskCanceledException)
            {
                // eat it
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message); // TODO: better error handling
            }
        }

        static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        /// <summary>
        ///   Stop the program.
        /// </summary>
        public static void Shutdown()
        {
            cancel.Cancel();
        }
    }
}

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
    public class Program
    {
        static CancellationTokenSource cancel = new CancellationTokenSource();

        public static void Main(string[] args)
        {
            try
            {
                BuildWebHost(args).RunAsync(cancel.Token).Wait();
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

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        public static void Shutdown()
        {
            cancel.Cancel();
        }
    }
}

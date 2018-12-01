using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk;
using PeerTalk.Protocols;
using PeerTalk.Transports;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spike
{
    class Program
    {
        static void Main(string[] args)
        {
            // set logger factory
            var properties = new Common.Logging.Configuration.NameValueCollection();
            properties["level"] = "DEBUG";
            properties["showLogName"] = "true";
            properties["showDateTime"] = "true";
            properties["dateTimeFormat"] = "HH:mm:ss.fff";
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

            var test = new BitswapApiTest();
            test.GetsBlock_OnConnect().Wait();
        }

    }
}
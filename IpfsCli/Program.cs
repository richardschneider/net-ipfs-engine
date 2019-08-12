﻿using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
using Ipfs.Http;
using Ipfs.CoreApi;
using Ipfs.Engine;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace Ipfs.Cli
{
    [Command("csipfs")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand("init", typeof(InitCommand))]
    [Subcommand("add", typeof(AddCommand))]
    [Subcommand("cat", typeof(CatCommand))]
    [Subcommand("get", typeof(GetCommand))]
    [Subcommand("ls", typeof(LsCommand))]
    [Subcommand("refs", typeof(RefsCommand))]
    [Subcommand("id", typeof(IdCommand))]
    [Subcommand("object", typeof(ObjectCommand))]
    [Subcommand("block", typeof(BlockCommand))]
    [Subcommand("files", typeof(FilesCommand))]
    [Subcommand("daemon", typeof(DaemonCommand))]
    [Subcommand("resolve", typeof(ResolveCommand))]
    [Subcommand("name", typeof(NameCommand))]
    [Subcommand("key", typeof(KeyCommand))]
    [Subcommand("dns", typeof(DnsCommand))]
    [Subcommand("pin", typeof(PinCommand))]
    [Subcommand("pubsub", typeof(PubsubCommand))]
    [Subcommand("bootstrap", typeof(BootstrapCommand))]
    [Subcommand("swarm", typeof(SwarmCommand))]
    [Subcommand("dht", typeof(DhtCommand))]
    [Subcommand("config", typeof(ConfigCommand))]
    [Subcommand("version", typeof(VersionCommand))]
    [Subcommand("shutdown", typeof(ShutdownCommand))]
    [Subcommand("update", typeof(UpdateCommand))]
    [Subcommand("bitswap", typeof(BitswapCommand))]
    [Subcommand("stats", typeof(StatsCommand))]
    [Subcommand("repo", typeof(RepoCommand))]
    class Program : CommandBase
    {
        static bool debugging;
        static bool tracing;

        public static int Main(string[] args)
        {
            var exitCode = -1;
            var startTime = DateTime.Now;

            // Need to setup common.logging early.
            debugging = args.Any(s => s == "--debug");
            tracing = args.Any(s => s == "--trace");
            var properties = new Common.Logging.Configuration.NameValueCollection();
            properties["level"] = tracing ? "TRACE" : (debugging ? "DEBUG" : "OFF");
            properties["showLogName"] = "true";
            properties["showDateTime"] = "true";
            properties["dateTimeFormat"] = "HH:mm:ss.fff";
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

            try
            {
                var app = new CommandLineApplication<Program>();
                app.HelpTextGenerator = new Help();
                app.Conventions.UseDefaultConventions();
                exitCode = app.Execute(args);
            }
            catch (Exception e)
            {
                for (; e != null; e = e.InnerException)
                {
                    Console.Error.WriteLine(e.Message);
                    if (debugging || tracing)
                    {
                        Console.WriteLine();
                        Console.WriteLine(e.StackTrace);
                    }
                }
                exitCode = 1;
            }

            var took = DateTime.Now - startTime;
            //Console.Write($"Took {took.TotalSeconds} seconds.");

            return exitCode;
        }

        [Option("--api <url>",  Description = "Use a specific API instance")]
        public string ApiUrl { get; set;  } = IpfsClient.DefaultApiUri.ToString();

        [Option("-L|--local", Description = "Run the command locally, instead of using the daemon")]
        public bool UseLocalEngine { get; set; }

        [Option("--enc", Description = "The output type (json, xml, or text)")]
        public string OutputEncoding { get; set; } = "text";

        [Option("--debug", Description = "Show debugging info")]
        public bool Debug { get; set; }  // just for documentation, already parsed in Main

        [Option("--trace", Description = "Show tracing info")]
        public bool Trace { get; set; }  // just for documentation, already parsed in Main

        [Option("--time", Description = "Show how long the command took")]
        public bool ShowTime { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }

        ICoreApi coreApi;
        public ICoreApi CoreApi
        {
            get
            {
                if (coreApi == null)
                {
                    if (UseLocalEngine)
                    {
                        // TODO: Add option --pass
                        string passphrase = "this is not a secure pass phrase";
                        var engine = new IpfsEngine(passphrase.ToCharArray());
                        engine.StartAsync().Wait();
                        coreApi = engine;
                    }
                    else
                    {
                        coreApi = new IpfsClient(ApiUrl);
                    }
                }

                return coreApi;
            }
        }

        public int Output<T>(CommandLineApplication app, T data, Action<T, TextWriter> text)
            where T: class
        {
            if (text == null)
            {
                OutputEncoding = "json";
            }

            switch (OutputEncoding.ToLowerInvariant())
            {
                case "text":
                    text(data, app.Out);
                    break;

                case "json":
                    var x = new JsonSerializer();
                    x.Formatting = Formatting.Indented;
                    x.Serialize(app.Out, data);
                    break;

                default:
                    app.Error.WriteLine($"Unknown output encoding '{OutputEncoding}'");
                    return 1;
            }

            return 0;
        }

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <summary>
        ///   Generate help text by using the default help text generated.
        /// </summary>
        /// <remarks>
        ///   Allows the footer to be command specific.
        /// </remarks>
        class Help : DefaultHelpTextGenerator
        {

            /// <summary>
            ///   Use command specific footer.
            /// </summary>
            /// <param name="application"></param>
            /// <param name="output"></param>
            protected override void GenerateFooter(CommandLineApplication application, TextWriter output)
            {
                // TODO
            }
        }
    }

}

﻿using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
using Ipfs.Api;
using Ipfs.CoreApi;
using Ipfs.Engine;
using McMaster.Extensions.CommandLineUtils;
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

namespace Ipfs.Cli
{
    [Command("ipfs")]
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
    [Subcommand("bootstrap", typeof(BootstrapCommand))]
    [Subcommand("swarm", typeof(SwarmCommand))]
    [Subcommand("dht", typeof(DhtCommand))]
    [Subcommand("config", typeof(ConfigCommand))]
    [Subcommand("version", typeof(VersionCommand))]
    [Subcommand("shutdown", typeof(ShutdownCommand))]
    [Subcommand("update", typeof(UpdateCommand))]
    class Program : CommandBase
    {
        public static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        [Option("--api <url>",  Description = "Use a specific API instance")]
        public string ApiUrl { get; set;  } = IpfsClient.DefaultApiUri.ToString();

        [Option("-L|--local", Description = "Run the command locally, instead of using the daemon")]
        public bool LocaleEngine { get; set; }

        [Option("--debug", Description = "Show debugging info")]
        public bool Debug {
            set
            {
                var properties = new Common.Logging.Configuration.NameValueCollection();
                properties["level"] = value ? "DEBUG" : "OFF";
                properties["showLogName"] = "true";
                properties["showDateTime"] = "true";
                LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);
            }
        }

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
                    if (LocaleEngine)
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

        private static string GetVersion()
            => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }

}
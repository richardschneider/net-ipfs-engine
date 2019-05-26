using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Publish/subscribe to messages on a given topic")]
    [Subcommand("ls", typeof(PubsubListCommand))]
    [Subcommand("peers", typeof(PubsubPeersCommand))]
    [Subcommand("pub", typeof(PubsubPublishCommand))]
    [Subcommand("sub", typeof(PubsubSubscribeCommand))]
    class PubsubCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "List subscribed topics by name")]
    class PubsubListCommand : CommandBase
    {
        PubsubCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var topics = await Program.CoreApi.PubSub.SubscribedTopicsAsync();
            return Program.Output(app, topics, (data, writer) =>
            {
                foreach (var topic in topics)
                {
                    writer.WriteLine(topic);
                }
            });
        }
    }

    [Command(Description = "List peers that are pubsubbing with")]
    class PubsubPeersCommand : CommandBase
    {
        [Argument(0, "topic", "The topic of interest")]
        public string Topic { get; set; }

        PubsubCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var peers = await Program.CoreApi.PubSub.PeersAsync(Topic);
            return Program.Output(app, peers, null);
        }
    }

    [Command(Description = "Publish a message on a topic")]
    class PubsubPublishCommand : CommandBase
    {
        [Argument(0, "topic", "The topic of interest")]
        [Required]
        public string Topic { get; set; }

        [Argument(1, "message", "The data to publish")]
        [Required]
        public string Message { get; set; }
        PubsubCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            await Program.CoreApi.PubSub.PublishAsync(Topic, Message);
            return 0;
        }
    }

    [Command(Description = "Subscribe to messages on a topic")]
    class PubsubSubscribeCommand : CommandBase
    {
        [Argument(0, "topic", "The topic of interest")]
        [Required]
        public string Topic { get; set; }

        PubsubCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var cts = new CancellationTokenSource();
            await Program.CoreApi.PubSub.SubscribeAsync(Topic, (m) =>
            {
                Program.Output(app, m, (data, writer) =>
                {
                    writer.WriteLine(Encoding.UTF8.GetString(data.DataBytes));
                });
            }, cts.Token);

            // Never return, just print messages received.
            await Task.Delay(-1);

            // Keep compiler happy.
            return 0;
        }
    }
}

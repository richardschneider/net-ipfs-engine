using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Show info on an IPFS peer [WIP]")]
    class IdCommand : CommandBase
    {
        [Argument(0, "peerid", "The IPFS peer ID")]
        public string PeerId { get; set; }

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            // TODO: optional peer id
            var peer = await Parent.CoreApi.Generic.IdAsync();
            using (JsonWriter writer = new JsonTextWriter(app.Out))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("ID");
                writer.WriteValue(peer.Id.ToBase58());
                writer.WritePropertyName("PublicKey");
                writer.WriteValue(peer.PublicKey);
                writer.WritePropertyName("Adddresses");
                writer.WriteStartArray();
                foreach (var a in peer.Addresses)
                {
                    writer.WriteValue(a.ToString());
                }
                writer.WriteEndArray();
                writer.WritePropertyName("AgentVersion");
                writer.WriteValue(peer.AgentVersion);
                writer.WritePropertyName("ProtocolVersion");
                writer.WriteValue(peer.ProtocolVersion);
                writer.WriteEndObject();
            }
            return 0;
        }

    }
}

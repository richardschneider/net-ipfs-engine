using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Manage private keys")]
    [Subcommand("list", typeof(KeyListCommand))]
    [Subcommand("rm", typeof(KeyRemoveCommand))]
    [Subcommand("gen", typeof(KeyCreateCommand))]
    [Subcommand("rename", typeof(KeyRenameCommand))]
    [Subcommand("export", typeof(KeyExportCommand))]
    [Subcommand("import", typeof(KeyImportCommand))]
    class KeyCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "List the keys")]
    class KeyListCommand : CommandBase
    {
        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var keys = await Program.CoreApi.Key.ListAsync();
            return Program.Output(app, keys, (data, writer) =>
            {
                foreach (var key in data)
                {
                    writer.WriteLine($"{key.Id} {key.Name}");
                }
            });
        }
    }

    [Command(Description = "Remove the key")]
    class KeyRemoveCommand : CommandBase
    {
        [Argument(0, "name", "The name of the key")]
        [Required]
        public string Name { get; set; }

        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var key = await Program.CoreApi.Key.RemoveAsync(Name);
            if (key == null)
            {
                app.Error.WriteLine($"The key '{Name}' is not defined.");
                return 1;
            }

            return Program.Output(app, key, (data, writer) =>
            {
                writer.WriteLine($"Removed {data.Id} {data.Name}");
            });
        }
    }

    [Command(Description = "Rename the key")]
    class KeyRenameCommand : CommandBase
    {
        [Argument(0, "name", "The name of the key")]
        [Required]
        public string Name { get; set; }

        [Argument(1, "new-name", "The new name of the key")]
        [Required]
        public string NewName { get; set; }

        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var key = await Program.CoreApi.Key.RenameAsync(Name, NewName);
            if (key == null)
            {
                app.Error.WriteLine($"The key '{Name}' is not defined.");
                return 1;
            }

            return Program.Output(app, key, (data, writer) =>
            {
                writer.WriteLine($"Renamed to {data.Name}");
            });
        }
    }

    [Command(Description = "Create a key")]
    class KeyCreateCommand : CommandBase
    {
        [Argument(0, "name", "The name of the key")]
        [Required]
        public string Name { get; set; }

        [Option("-t|--type", Description = "The type of the key [rsa, ed25519]")]
        public string KeyType { get; set; } = "rsa";

        [Option("-s|--size", Description = "The key size")]
        public int KeySize { get; set; }

        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var key = await Program.CoreApi.Key.CreateAsync(Name, KeyType, KeySize);
            return Program.Output(app, key, (data, writer) =>
            {
                writer.WriteLine($"{data.Id} {data.Name}");
            });
        }
    }

    [Command(Description = "Export the key to a PKCS #8 PEM file")]
    class KeyExportCommand : CommandBase
    {
        [Argument(0, "name", "The name of the key")]
        [Required]
        public string Name { get; set; }

        [Option("-o|--output", Description = "The file name for the PEM file")]
        public string OutputBasePath { get; set; }

        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var pass = Prompt.GetPassword("Password for PEM file?");
            var pem = await Program.CoreApi.Key.ExportAsync(Name, pass.ToCharArray());
            if (OutputBasePath == null)
            {
                app.Out.Write(pem);
            }
            else
            {
                var path = OutputBasePath;
                if (!Path.HasExtension(path))
                    path = Path.ChangeExtension(path, ".pem");
                using (var writer = File.CreateText(path))
                {
                    writer.Write(pem);
                }
            }

            return 0;
        }

    }

    [Command(Description = "Import the key from a PKCS #8 PEM file")]
    class KeyImportCommand : CommandBase
    {
        [Argument(0, "name", "The name of the key")]
        [Required]
        public string Name { get; set; }

        [Argument(1, "path", "The path to the PEM file")]
        [Required]
        public string PemPath { get; set; }

        KeyCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var pem = File.ReadAllText(PemPath);
            var pass = Prompt.GetPassword("Password for PEM file?");
            var key = await Program.CoreApi.Key.ImportAsync(Name, pem, pass.ToCharArray());
            return Program.Output(app, key, (data, writer) =>
            {
                writer.WriteLine($"{data.Id} {data.Name}");
            });
        }
    }

}

using Microsoft.EntityFrameworkCore;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ipfs.Engine
{
    class Repository : DbContext
    {
        static ILog log = LogManager.GetLogger(typeof(Repository));
#if !NETSTANDARD14
        public static readonly LoggerFactory MyLoggerFactory
            = new LoggerFactory(new ILoggerProvider[] 
            {
                new MSCommonLoggingProvider(log)
            });
#endif

        public RepositoryOptions Options { get; set; } = new RepositoryOptions();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!Directory.Exists(Options.Folder))
            {
                log.DebugFormat("creating folder '{0}'", Options.Folder);
                Directory.CreateDirectory(Options.Folder);
            }
//            log.DebugFormat("using '{0}'", Options.DatabaseName);
            optionsBuilder
#if !NETSTANDARD14
//                .UseLoggerFactory(MyLoggerFactory)
#endif
                .UseSqlite($"Data Source={Options.DatabaseName}");
        }

        public async Task CreateAsync(CancellationToken cancel = default(CancellationToken))
        {
            log.Debug("applying migrations");
            await this.Database.MigrateAsync(cancel);
        }

        public async Task<bool> DeleteAsync(CancellationToken cancel = default(CancellationToken))
        {
            log.DebugFormat("removing '{0}'", Options.DatabaseName);
            return await this.Database.EnsureDeletedAsync(cancel);
        }

        public DbSet<Config> Configs { get; set; }
        public DbSet<Cryptography.KeyInfo> Keys { get; set; }
        public DbSet<Cryptography.EncryptedKey> EncryptedKeys { get; set; }
        public DbSet<BlockInfo> BlockInfos { get; set; }
        public DbSet<BlockValue> BlockValues { get; set; }

        public class Config
        {
            [Key]
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class BlockInfo
        {
            [Key]
            public string Cid { get; set; }
            public bool Pinned { get; set; }
            public long DataSize { get; set; }
        }

        public class BlockValue
        {
            [Key]
            public string Cid { get; set; }
            public byte[] Data { get; set; }
        }
    }
}

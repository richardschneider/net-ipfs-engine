using Microsoft.EntityFrameworkCore;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    class Repository : DbContext
    {
        static ILog log = LogManager.GetLogger(typeof(Repository));

        /// <summary>
        ///   The directory of the repository.
        /// </summary>
        public string Folder { get; set; } 
            = Path.Combine(
                Environment.GetEnvironmentVariable("HOME") ?? 
                Environment.GetEnvironmentVariable("HOMEPATH"),
                ".csipfs");

        string databaseFqn;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!Directory.Exists(Folder))
            {
                log.DebugFormat("creating folder '{0}'", Folder);
                Directory.CreateDirectory(Folder);
            }
            databaseFqn = Path.Combine(Folder, "ipfs.db");
            log.DebugFormat("using '{0}'", databaseFqn);
            optionsBuilder.UseSqlite($"Data Source={databaseFqn}");
        }

        public async Task CreateAsync(CancellationToken cancel = default(CancellationToken))
        {
            log.Debug("applying migrations");
            await this.Database.MigrateAsync(cancel);
        }

        public async Task<bool> DeleteAsync(CancellationToken cancel = default(CancellationToken))
        {
            log.DebugFormat("removing '{0}'", databaseFqn);
            return await this.Database.EnsureDeletedAsync(cancel);
        }

        public DbSet<Config> Configs { get; set; }

        public class Config
        {
            [Key]
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}

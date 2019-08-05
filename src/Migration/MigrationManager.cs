using Common.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.Migration
{
    /// <summary>
    ///   Allows migration of the repository. 
    /// </summary>
    public class MigrationManager
    {
        static ILog log = LogManager.GetLogger(typeof(MigrationManager));

        readonly IpfsEngine ipfs;

        /// <summary>
        ///   Creates a new instance of the <see cref="MigrationManager"/> class
        ///   for the specifed <see cref="IpfsEngine"/>.
        /// </summary>
        public MigrationManager(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;

            Migrations = typeof(MigrationManager).Assembly
                .GetTypes()
                .Where(x => typeof(IMigration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => (IMigration)Activator.CreateInstance(x))
                .OrderBy(x => x.Version)
                .ToList();
        }

        /// <summary>
        ///   The list of migrations that can be performed.
        /// </summary>
        public List<IMigration> Migrations { get; private set; }

        /// <summary>
        ///   Gets the latest supported version number of a repository.
        /// </summary>
        public int LatestVersion => Migrations.Last().Version;

        /// <summary>
        ///   Gets the current vesion number of the  repository.
        /// </summary>
        public int CurrentVersion
        {
            get
            {
                var path = VersionPath();
                if (File.Exists(path))
                {
                    using (var reader = new StreamReader(path))
                    {
                        var s = reader.ReadLine();
                        return int.Parse(s, CultureInfo.InvariantCulture);
                    }
                }

                return 0;
            }
            private set
            {
                File.WriteAllText(VersionPath(), value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        ///   Upgrade/downgrade to the specified version.
        /// </summary>
        /// <param name="version">
        ///   The required version of the repository.
        /// </param>
        /// <param name="cancel">
        /// </param>
        /// <returns></returns>
        public async Task MirgrateToVersionAsync(int version, CancellationToken cancel = default(CancellationToken))
        {
            if (version != 0 && !Migrations.Any(m => m.Version == version))
            {
                throw new ArgumentOutOfRangeException("version", $"Repository version '{version}' is unknown.");
            }

            var currentVersion = CurrentVersion;
            var increment = CurrentVersion < version ? 1 : -1;
            while (currentVersion != version)
            {
                var nextVersion = currentVersion + increment;
                log.InfoFormat("Migrating to version {0}", nextVersion);

                if (increment > 0)
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == nextVersion);
                    if (migration.CanUpgrade)
                    {
                        await migration.UpgradeAsync(ipfs, cancel);
                    }
                }
                else if (increment < 0)
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == currentVersion);
                    if (migration.CanDowngrade)
                    {
                        await migration.DowngradeAsync(ipfs, cancel);
                    }
                }

                CurrentVersion = nextVersion;
                currentVersion = nextVersion;
            }
        }

        /// <summary>
        ///   Gets the FQN of the version file.
        /// </summary>
        /// <returns>
        ///   The path to the version file.
        /// </returns>
        string VersionPath()
        {
            return Path.Combine(ipfs.Options.Repository.ExistingFolder(), "version");
        }
    }
}

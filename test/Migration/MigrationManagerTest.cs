using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Ipfs.Engine.Migration
{
    [TestClass]
    public class MigrationManagerTest
    {
        [TestMethod]
        public void HasMigrations()
        {
            var migrator = new MigrationManager(TestFixture.Ipfs);
            var migrations = migrator.Migrations;
            Assert.AreNotEqual(0, migrations.Count);
        }

        [TestMethod]
        public void MirgrateToUnknownVersion()
        {
            var migrator = new MigrationManager(TestFixture.Ipfs);
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                migrator.MirgrateToVersionAsync(int.MaxValue).Wait();
            });
        }

        [TestMethod]
        public async Task MigrateToLowestThenHighest()
        {
            using (var ipfs = new TempNode())
            {
                var migrator = new MigrationManager(ipfs);
                await migrator.MirgrateToVersionAsync(0);
                Assert.AreEqual(0, migrator.CurrentVersion);

                await migrator.MirgrateToVersionAsync(migrator.LatestVersion);
                Assert.AreEqual(migrator.LatestVersion, migrator.CurrentVersion);
            }
        }
    }
}
 
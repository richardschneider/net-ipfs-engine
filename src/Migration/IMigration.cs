using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.Migration
{
    /// <summary>
    ///   Provides a migration path to the repository.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        ///   The repository version that is created.
        /// </summary>
        int Version { get; }

        /// <summary>
        ///   Indicates that an upgrade can be performed.
        /// </summary>
        bool CanUpgrade { get; }

        /// <summary>
        ///   Indicates that an downgrade can be performed.
        /// </summary>
        bool CanDowngrade { get; }

        /// <summary>
        ///   Upgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to upgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task UpgradeAsync(IpfsEngine ipfs, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Downgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to downgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task DowngradeAsync(IpfsEngine ipfs, CancellationToken cancel = default(CancellationToken));
    }
}

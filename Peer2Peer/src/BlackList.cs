using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    /// <summary>
    ///   A sequence of targets that are not approved.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    /// <remarks>
    ///   Only targets that are not defined will pass.
    /// </remarks>
    public class BlackList<T> : ConcurrentBag<T>, IPolicy<T>
        where T : IEquatable<T>
    {
        /// <inheritdoc />
        public Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(!this.Contains(target));
        }

        /// <inheritdoc />
        public async Task<bool> IsNotAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            var q = await IsAllowedAsync(target, cancel);
            return !q;
        }
    }
}

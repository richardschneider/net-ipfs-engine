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
    ///   A sequence of targets that are approved.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    /// <remarks>
    ///   Only targets that are defined will pass.  If no targets are defined, then anything
    ///   passes.
    /// </remarks>
    public class WhiteList<T> : ConcurrentBag<T>, IPolicy<T>
        where T : IEquatable<T>
    {
        /// <inheritdoc />
        public Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(this.IsEmpty || this.Contains(target));
        }

        /// <inheritdoc />
        public async Task<bool> IsNotAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            var q = await IsAllowedAsync(target, cancel);
            return !q;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A base for defining a policy.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public abstract class Policy<T> : IPolicy<T>
    {
        /// <inheritdoc />
        public abstract Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken));

        /// <inheritdoc />
        public async Task<bool> IsNotAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            var q = await IsAllowedAsync(target, cancel);
            return !q;
        }
    }

    /// <summary>
    ///   A rule that always passes.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public class PolicyAlways<T> : Policy<T>
    {
        /// <inheritdoc />
        public override Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    ///   A rule that always fails.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    public class PolicyNever<T> : Policy<T>
    {
        /// <inheritdoc />
        public override Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(false);
        }
    }
}

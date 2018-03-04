using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A rule that must be enforced.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object that the rule applies to.
    /// </typeparam>
    interface IPolicy<T>
    {
        /// <summary>
        ///   Determines if the target passes the rule.
        /// </summary>
        /// <param name="target">
        ///   An object to test against the rule.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   <b>true</b> if the <paramref name="target"/> passes the rule.
        /// </returns>
        Task<bool> IsAllowedAsync(T target, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Determines if the target fails the rule.
        /// </summary>
        /// <param name="target">
        ///   An object to test against the rule.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   <b>true</b> if the <paramref name="target"/> fails the rule.
        /// </returns>
        Task<bool> IsNotAllowedAsync(T target, CancellationToken cancel = default(CancellationToken));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer
{
    /// <summary>
    ///   A service can be started and stopped.
    /// </summary>
    public interface IService
    {
        /// <summary>
        ///   Start the service.
        /// </summary>
        void Start();

        /// <summary>
        ///   Stop the service.
        /// </summary>
        void Stop();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A service is async and can be started and stopped.
    /// </summary>
    public interface IService
    {
        /// <summary>
        ///   Start the service.
        /// </summary>
        Task StartAsync();

        /// <summary>
        ///   Stop the service.
        /// </summary>
        Task StopAsync();
    }
}

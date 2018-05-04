using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    /// <summary>
    ///   The purpose of the multiplex message.
    /// </summary>
    /// <seealso cref="Header"/>
    public enum PacketType : byte
    {
        /// <summary>
        ///   Create a new stream.
        /// </summary>
        NewStream = 0,

        /// <summary>
        ///   A message from the "receiver".
        /// </summary>
        MessageReceiver = 1,

        /// <summary>
        ///   A message from the "initiator".
        /// </summary>
        MessageInitiator = 2,

        /// <summary>
        ///   Close the stream from the "receiver".
        /// </summary>
        CloseReceiver = 3,

        /// <summary>
        ///   Close the stream from the "initiator".
        /// </summary>
        CloseInitiator = 4,

        /// <summary>
        ///   Reset the stream from the "receiver".
        /// </summary>
        ResetReceiver = 5,

        /// <summary>
        ///   Reset the stream from the "initiator".
        /// </summary>
        ResetInitiator = 6
    }
}

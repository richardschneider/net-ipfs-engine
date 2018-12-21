using Ipfs;
using Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   Manages the peer connections in a <see cref="Swarm"/>.
    /// </summary>
    /// <remarks>
    ///   Enforces that only one connection exists to the <see cref="Peer"/>.  This
    ///   prevents the race condition when two simultaneously connect to each other.
    ///   <para>
    ///   TODO: Enforces a maximum number of open connections.
    ///   </para>
    /// </remarks>
    public class ConnectionManager
    {
        static ILog log = LogManager.GetLogger(typeof(ConnectionManager));

        /// <summary>
        ///   The connections to other peers. Key is the base58 hash of the peer ID.
        /// </summary>
        ConcurrentDictionary<string, PeerConnection> connections = new ConcurrentDictionary<string, PeerConnection>();

        string Key(Peer peer) => peer.Id.ToBase58();

        /// <summary>
        ///   Gets the current connections.
        /// </summary>
        public IEnumerable<PeerConnection> Connections => connections.Values;

        /// <summary>
        ///   Determines if a connection exists to the specified peer.
        /// </summary>
        /// <param name="peer">
        ///   Another peer.
        /// </param>
        /// <returns>
        ///   <b>true</b> if there is a connection to the <paramref name="peer"/> and
        ///   the connection is active; otherwise <b>false</b>.
        /// </returns>
        public bool IsConnected(Peer peer)
        {
            return TryGet(peer, out PeerConnection _);
        }

        /// <summary>
        ///    Gets the connection to the peer.
        /// </summary>
        /// <param name="peer">
        ///   A peer.
        /// </param>
        /// <param name="connection">
        ///   The connection to the peer.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection exists; otherwise <b>false</b>.
        /// </returns>
        /// <remarks>
        ///   If the connection's underlaying <see cref="PeerConnection.Stream"/>
        ///   is closed, then the connection is removed.
        /// </remarks>
        public bool TryGet(Peer peer, out PeerConnection connection)
        {
            if (!connections.TryGetValue(Key(peer), out connection))
            {
                return false;
            }

            // Is nolonger active.
            if (connection.Stream == null || !connection.Stream.CanRead || !connection.Stream.CanWrite)
            {
                Remove(connection);
                connection = null;
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Adds a new connection.
        /// </summary>
        /// <param name="connection">
        ///   The <see cref="PeerConnection"/> to add.
        /// </param>
        /// <returns>
        ///   The connection that should be used.
        /// </returns>
        /// <remarks>
        ///   If a connection already exists to the peer, the specified
        ///   <paramref name="connection"/> is closed and existing connection
        ///   is returned.
        /// </remarks>
        public PeerConnection Add(PeerConnection connection)
        {
            if (connections.TryGetValue(Key(connection.RemotePeer), out PeerConnection existing))
            {
                // If the same connection.
                if (connection == existing)
                {
                    return connection;
                }

                // If existing is dead, then use current connection.
                if (existing.Stream == null || !existing.Stream.CanRead || !existing.Stream.CanWrite)
                {
                    var address = connection.RemotePeer.ConnectedAddress;
                    Remove(existing);
                    connection.RemotePeer.ConnectedAddress = address;
                    // fall thru to add logic
                }
                else
                {
                    var address = existing.RemotePeer.ConnectedAddress;
                    log.Debug($"duplicate {connection.RemoteAddress}, keeping {existing.RemoteAddress}");
                    connection.Dispose();
                    existing.RemotePeer.ConnectedAddress = address;
                    return existing;
                }
            }

            if (!connections.TryAdd(Key(connection.RemotePeer), connection))
            {
                // This case should not happen.
                connection.Dispose();
                return connections[Key(connection.RemotePeer)];
            }

            return connection;
        }

        /// <summary>
        ///   Remove a connection.
        /// </summary>
        /// <param name="connection">
        ///   The <see cref="PeerConnection"/> to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the connection was removed; otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        ///    The <paramref name="connection"/> is removed from the list of
        ///    connections and is closed.
        /// </remarks>
        public bool Remove(PeerConnection connection)
        {
            if (connection == null)
            {
                return false;
            }

            var q = connections.TryRemove(Key(connection.RemotePeer), out PeerConnection _);
            connection.Dispose();

            return q;
        }

        /// <summary>
        ///   Remove the connection to the peer.
        /// </summary>
        /// <param name="peer">
        ///   The peer to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection was removed; otherwise, <b>false</b>.
        /// </returns>
        public bool Remove(Peer peer)
        {
            var connection = connections.Values.FirstOrDefault(c => c.RemotePeer.Id == peer.Id);
            return Remove(connection);
        }

        /// <summary>
        ///   Remove the connection to the peer ID.
        /// </summary>
        /// <param name="id">
        ///   The ID of a <see cref="Peer"/> to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection was removed; otherwise, <b>false</b>.
        /// </returns>
        public bool Remove(MultiHash id)
        {
            var connection = connections.Values.FirstOrDefault(c => c.RemotePeer.Id == id);
            return Remove(connection);
        }

        /// <summary>
        ///   Removes and closes all connections.
        /// </summary>
        public void Clear()
        {

            for (var connection = connections.Values.LastOrDefault(); 
                connection != null; 
                connection = connections.Values.LastOrDefault())
            {
                Remove(connection);
            }
        }
    }
}

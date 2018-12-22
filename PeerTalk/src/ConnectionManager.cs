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
        ConcurrentDictionary<string, List<PeerConnection>> connections = new ConcurrentDictionary<string, List<PeerConnection>>();

        string Key(Peer peer) => peer.Id.ToBase58();
        string Key(MultiHash id) => id.ToBase58();

        /// <summary>
        ///   Gets the current connections.
        /// </summary>
        public IEnumerable<PeerConnection> Connections => connections.Values
            .SelectMany(c => c)
            .Where(c => c.Stream != null && c.Stream.CanRead && c.Stream.CanWrite);

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
            connection = null;
            if (!connections.TryGetValue(Key(peer), out List<PeerConnection> conns))
            {
                return false;
            }

            connection = conns
                .Where(c => c.Stream != null && c.Stream.CanRead && c.Stream.CanWrite)
                .FirstOrDefault();

            return connection != null;
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
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (connection.RemotePeer == null)
                throw new ArgumentNullException("connection.RemotePeer");
            if (connection.RemotePeer.Id == null)
                throw new ArgumentNullException("connection.RemotePeer.Id");

            connections.AddOrUpdate(
                Key(connection.RemotePeer),
                (key) => new List<PeerConnection> { connection },
                (key, conns) =>
                {
                    if (!conns.Contains(connection))
                    {
                        conns.Add(connection);
                    }
                    return conns;
                }
            );

            connection.Closed += (s, e) => Remove(e);
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

            if (!connections.TryGetValue(Key(connection.RemotePeer), out List<PeerConnection> originalConns))
            {
                connection.Dispose();
                return false;
            }
            if (!originalConns.Contains(connection))
            {
                connection.Dispose();
                return false;
            }

            var newConns = new List<PeerConnection>();
            newConns.AddRange(originalConns.Where(c => c != connection));
            connections.TryUpdate(Key(connection.RemotePeer), newConns, originalConns);

            connection.Dispose();
            if (newConns.Count > 0)
            {
                var last = newConns.Last();
                last.RemotePeer.ConnectedAddress = last.RemoteAddress;
            }
            return true;
        }

        /// <summary>
        ///   Remove and close all connection tos the peer ID.
        /// </summary>
        /// <param name="id">
        ///   The ID of a <see cref="Peer"/> to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection was removed; otherwise, <b>false</b>.
        /// </returns>
        public bool Remove(MultiHash id)
        {
            if (!connections.TryRemove(Key(id), out List<PeerConnection> conns))
            {
                return false;
            }
            foreach (var conn in conns)
            {
                conn.Dispose();
            }
            return true;
        }

        /// <summary>
        ///   Removes and closes all connections.
        /// </summary>
        public void Clear()
        {
            var conns = connections.Values.SelectMany(c => c).ToArray();
            foreach (var conn in conns)
            {
                Remove(conn);
            }
        }
    }
}

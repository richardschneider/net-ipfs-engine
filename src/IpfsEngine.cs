using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ipfs.CoreApi;
using Ipfs.Engine.CoreApi;

namespace Ipfs.Engine
{
    /// <summary>
    ///   TODO
    /// </summary>
    public partial class IpfsEngine : ICoreApi
    {
        static ILog log = LogManager.GetLogger(typeof(IpfsEngine));

        Repository repository;

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class.
        /// </summary>
        public IpfsEngine()
        {
            // Init the core api inteface.
            Bitswap = new BitswapApi(this);
            Block = new BlockApi(this);
            Config = new ConfigApi(this);
            Dag = new DagApi(this);
            Dht = new DhtApi(this);
            FileSystem = new FileSystemApi(this);
            Generic = new GenericApi(this);
            Key = new KeyApi(this);
            Name = new NameApi(this);
            Object = new ObjectApi(this);
            Pin = new PinApi(this);
            PubSub = new PubSubApi(this);
            Swarm = new SwarmApi(this);
        }

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; private set; }

        /// <inheritdoc />
        public IBlockApi Block { get; private set; }

        /// <inheritdoc />
        public IConfigApi Config { get; private set; }

        /// <inheritdoc />
        public IDagApi Dag { get; private set; }

        /// <inheritdoc />
        public IDhtApi Dht { get; private set; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; private set; }

        /// <inheritdoc />
        public IGenericApi Generic { get; private set; }

        /// <inheritdoc />
        public IKeyApi Key { get; private set; }

        /// <inheritdoc />
        public INameApi Name { get; private set; }

        /// <inheritdoc />
        public IObjectApi Object { get; private set; }

        /// <inheritdoc />
        public IPinApi Pin { get; private set; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; private set; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; private set; }

        internal async Task<Repository> Repository(CancellationToken cancel = default(CancellationToken))
        {
            if (repository != null)
            {
                return await Task.FromResult(repository);
            }

            lock (this)
            {
                if (repository == null)
                {
                    repository = new Repository
                    {

                    };
                }
            }
            await repository.CreateAsync(cancel);
            return repository;
        }

    }
}

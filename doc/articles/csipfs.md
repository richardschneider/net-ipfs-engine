# IPFS CLI

## Install

The `csipfs` tool is available on [Nuget](https://www.nuget.org/packages/csipfs/) and
is installed with dotnet.

    > dotnet tool install --global csipfs

## Usage

`csipfs` is a tool to control your IPFS node.

```
> csipfs --help

Usage: csipfs [options] [command]

Options:
  --version    Show version information
  --help       Show help information
  --api <url>  Use a specific API instance
  -L|--local   Run the command locally, instead of using the daemon
  --enc        The output type (json, xml, or text)
  --debug      Show debugging info
  --trace      Show tracing info
  --time       Show how long the command took

Commands:
  add          Add a file to IPFS
  bitswap      Manage swapped blocks
  block        Manage raw blocks
  bootstrap    Manage bootstrap peers
  cat          Show IPFS file data
  config       Manage the configuration
  daemon       Start a long running IPFS deamon
  dht          Query the DHT for values or peers
  dns          Resolve DNS link
  files        Manage the mfs (Mutable File System) [WIP]
  get          Download IPFS data
  id           Show info on an IPFS peer
  init         Initialize ipfs local configuration [WIP]
  key          Manage private keys
  ls           List links
  name         Manage IPNS names
  object       Manage IPFS objects
  pin          Manage data in local storage [WIP]
  pubsub       Publish/subscribe to messages on a given topic
  refs         List hashes of links [WIP]
  repo         Manage the IPFS repository
  resolve      Resolve any type of name
  shutdown     Stop the IPFS deamon
  stats        Query IPFS statistics
  swarm        Manage connections to the p2p network
  update       Download the latest version [WIP]
  version      Show version information

Run 'csipfs [command] --help' for more information about a command.
```

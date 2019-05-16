# net-ipfs-engine

[![build status](https://ci.appveyor.com/api/projects/status/github/richardschneider/net-ipfs-engine?branch=master&svg=true)](https://ci.appveyor.com/project/richardschneider/net-ipfs-engine) 
[![travis build](https://travis-ci.org/richardschneider/net-ipfs-engine.svg?branch=master)](https://travis-ci.org/richardschneider/net-ipfs-engine)
[![CircleCI](https://circleci.com/gh/richardschneider/net-ipfs-engine.svg?style=svg)](https://circleci.com/gh/richardschneider/net-ipfs-engine)
[![Coverage Status](https://coveralls.io/repos/richardschneider/net-ipfs-engine/badge.svg?branch=master&service=github)](https://coveralls.io/github/richardschneider/net-ipfs-engine?branch=master)
[![Version](https://img.shields.io/nuget/v/Ipfs.Engine.svg)](https://www.nuget.org/packages/Ipfs.Engine)
[![docs](https://cdn.rawgit.com/richardschneider/net-ipfs-engine/master/doc/images/docs-latest-green.svg)](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html)


An embedded [IPFS](https://ipfs.io) engine implemented in C#.  It implements the 
[IPFS Core API](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.CoreApi.html) 
which makes it possible to create a decentralised 
and distributed application without relying on an "IPFS daemon".
Basically, your application becomes an IPFS node.

More information, including the class reference, is on the [Project](https://richardschneider.github.io/net-ipfs-engine/) web site.
This is **BETA CODE** and breaking changes will occur.

[![IPFS Core API](https://github.com/ipfs/interface-ipfs-core/raw/master/img/badge.png)](https://github.com/ipfs/interface-ipfs-core)


## Features

- An embedded .Net implementation of IPFS, no need for a "IPFS daemon"
- Targets 
  - .NET Framework 4.6.1
  - .NET Standard 1.4
  - .NET Standard 2.0
- Supports [asynchronous I/O](https://richardschneider.github.io/net-ipfs-engine/articles/async.html)
- Supports [cancellation](https://richardschneider.github.io/net-ipfs-engine/articles/cancellation.html)
- Comprehensive [documentation](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html)
- C# style access to the [ipfs core interface](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.CoreApi.html)

## Getting started

Published releases are available on [NuGet](https://www.nuget.org/packages/Ipfs.Engine/).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package Ipfs.Engine
    

## Usage

```csharp
using Ipfs.Engine;

var ipfs = new IpfsEngine();

const string filename = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv/about";
string text = await ipfs.FileSystem.ReadAllTextAsync(filename);
```

## Related projects

- [IPFS Core](https://github.com/richardschneider/net-ipfs-core)
- [IPFS HTTP Client](https://github.com/richardschneider/net-ipfs-http-client)
- [Peer Talk](https://github.com/richardschneider/peer-talk)

## Sponsors
<img src="doc/images/atlascity.io-logo.png" width="200" alt="https://atlascity.io" />

##### [AtlasCity.io](https://github.com/atlascity) - Developing blockchain business solutions

# License
Copyright © 2018 Richard Schneider (makaretu@gmail.com)

The IPFS Engine is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](https://github.com/richardschneider/net-ipfs-engine/blob/master/LICENSE) file for more information.

<a href="https://www.buymeacoffee.com/kmXOxKJ4E" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/yellow_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

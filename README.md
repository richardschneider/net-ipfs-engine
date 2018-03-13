# net-ipfs-engine

[![build status](https://ci.appveyor.com/api/projects/status/github/richardschneider/net-ipfs-engine?branch=master&svg=true)](https://ci.appveyor.com/project/richardschneider/net-ipfs-engine) 
[![travis build](https://travis-ci.org/richardschneider/net-ipfs-engine.svg?branch=master)](https://travis-ci.org/richardschneider/net-ipfs-engine)
[![CircleCI](https://circleci.com/gh/richardschneider/net-ipfs-engine.svg?style=svg)](https://circleci.com/gh/richardschneider/net-ipfs-engine)
[![Coverage Status](https://coveralls.io/repos/richardschneider/net-ipfs-engine/badge.svg?branch=master&service=github)](https://coveralls.io/github/richardschneider/net-ipfs-engine?branch=master)
[![Version](https://img.shields.io/nuget/v/Ipfs.Engine.svg)](https://www.nuget.org/packages/Ipfs.Engine)
[![docs](https://cdn.rawgit.com/richardschneider/net-ipfs-engine/master/doc/images/docs-latest-green.svg)](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html)


An embedded IPFS engine implemented in C#. More information, including the class reference, is on the [Project](https://richardschneider.github.io/net-ipfs-engine/) web site.

[![IPFS Core API](https://github.com/ipfs/interface-ipfs-core/raw/master/img/badge.png)](https://github.com/ipfs/interface-ipfs-core)

This is **ALPHA CODE** and is not yet ready for prime time. 

## Features

- An embedded .Net implementation of IPFS, no need for a "IPFS daemon"
- Targets 
  - .NET Framework 4.6.1, 
  - ~~.NET Standard 1.4~~ see [issue #7](https://github.com/richardschneider/net-ipfs-engine/issues/7)
  - .NET Standard 2.0
- Supports [asynchronous I/O](https://richardschneider.github.io/net-ipfs-engine/articles/async.html)
- Supports [cancellation](https://richardschneider.github.io/net-ipfs-engine/articles/cancellation.html)
- Comprehensive [documentation](https://richardschneider.github.io/net-ipfs-engine/articles/intro.html)
- C# style access to the [ipfs core interface](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.CoreApi.html)

## Getting started

Published releases are available on [NuGet](https://www.nuget.org/packages/Ipfs.Engine/).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package Ipfs.Engine
    
For the latest build or older non-released builds see [Continuous Integration](https://github.com/richardschneider/net-ipfs-engine/wiki/Continuous-Integration).

## Usage

```csharp
using Ipfs.Engine;

var ipfs = new IpfsEngine();

const string filename = "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about";
string text = await ipfs.FileSystem.ReadAllTextAsync(filename);
```

## Related projects

- [IPFS Core](https://github.com/richardschneider/net-ipfs-core)
- [IPFS API](https://github.com/richardschneider/net-ipfs-api)

# License
Copyright � 2018 Richard Schneider (makaretu@gmail.com)

The IPFS Engine is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](https://github.com/richardschneider/net-ipfs-engine/blob/master/LICENSE) file for more information.
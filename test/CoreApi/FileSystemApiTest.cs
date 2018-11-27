using Ipfs.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class FileSystemApiTest
    {

        [TestMethod]
        public async Task AddText()
        {
            var ipfs = TestFixture.Ipfs;
            var node = (UnixFileSystem.FileSystemNode) await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
            Assert.AreEqual("", node.Name);
            Assert.AreEqual(0, node.Links.Count());

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("hello world", text);

            var actual = await ipfs.FileSystem.ListFileAsync(node.Id);
            Assert.AreEqual(node.Id, actual.Id);
            Assert.AreEqual(node.IsDirectory, actual.IsDirectory);
            Assert.AreEqual(node.Links.Count(), actual.Links.Count());
            Assert.AreEqual(node.Size, actual.Size);
        }

        [TestMethod]
        public async Task AddEmptyText()
        {
            var ipfs = TestFixture.Ipfs;
            var node = (UnixFileSystem.FileSystemNode)await ipfs.FileSystem.AddTextAsync("");
            Assert.AreEqual("QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH", (string)node.Id);
            Assert.AreEqual("", node.Name);
            Assert.AreEqual(0, node.Links.Count());

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("", text);
        
            var actual = await ipfs.FileSystem.ListFileAsync(node.Id);
            Assert.AreEqual(node.Id, actual.Id);
            Assert.AreEqual(node.IsDirectory, actual.IsDirectory);
            Assert.AreEqual(node.Links.Count(), actual.Links.Count());
            Assert.AreEqual(node.Size, actual.Size);
        }

        [TestMethod]
        public async Task AddEmpty_Check_Object()
        {
            // see https://github.com/ipfs/js-ipfs-unixfs/pull/25
            var ipfs = TestFixture.Ipfs;
            var node = await ipfs.FileSystem.AddTextAsync("");
            var block = await ipfs.Object.GetAsync(node.Id);
            var expected = new byte[] { 0x08, 0x02, 0x18, 0x00 };
            Assert.AreEqual(node.Id, block.Id);
            CollectionAssert.AreEqual(expected, block.DataBytes);
        }

        [TestMethod]
        public async Task AddDuplicateWithPin()
        {
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions();
            options.Pin = true;
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
            var pins = await ipfs.Pin.ListAsync();
            CollectionAssert.Contains(pins.ToArray(), node.Id);

            options.Pin = false;
            node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
            Assert.AreEqual(0, node.Links.Count());
            pins = await ipfs.Pin.ListAsync();
            CollectionAssert.DoesNotContain(pins.ToArray(), node.Id);
        }

        [TestMethod]
        public async Task Add_SizeChunking()
        {
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            options.Pin = true;
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.AreEqual("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn", (string)node.Id);
            Assert.AreEqual(false, node.IsDirectory);
            Assert.AreEqual(4, links.Length);
            Assert.AreEqual("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6", (string)links[0].Id);
            Assert.AreEqual("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN", (string)links[1].Id);
            Assert.AreEqual("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6", (string)links[2].Id);
            Assert.AreEqual("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE", (string)links[3].Id);

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("hello world", text);
        }

        [TestMethod]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var node = (UnixFileSystem.FileSystemNode)ipfs.FileSystem.AddFileAsync(path).Result;
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Id);
                Assert.AreEqual(0, node.Links.Count());
                Assert.AreEqual(Path.GetFileName(path), node.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void AddFile_CidEncoding()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var options = new AddFileOptions
                {
                    Encoding = "base32"
                };
                var node = ipfs.FileSystem.AddFileAsync(path, options).Result;
                Assert.AreEqual("base32", node.Id.Encoding);
                Assert.AreEqual(1, node.Id.Version);
                Assert.AreEqual(0, node.Links.Count());

                var text = ipfs.FileSystem.ReadAllTextAsync(node.Id).Result;
                Assert.AreEqual("hello world", text);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void AddFile_Large()
        {
            AddFile(); // warm up

            var path = "star_trails.mp4";
            var ipfs = TestFixture.Ipfs;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var node = ipfs.FileSystem.AddFileAsync(path).Result;
            stopWatch.Stop();
            Console.WriteLine("Add file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);

            Assert.AreEqual("QmeZkAUfUFPq5YWGBan2ZYNd9k59DD1xW62pGJrU3C6JRo", (string)node.Id);

            var k = 8 * 1024;
            var buffer1 = new byte[k];
            var buffer2 = new byte[k];
            stopWatch.Restart();
            using (var localStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var ipfsStream = ipfs.FileSystem.ReadFileAsync(node.Id).Result)
            {
                while (true)
                {
                    var n1 = localStream.Read(buffer1, 0, k);
                    var n2 = ipfsStream.Read(buffer2, 0, k);
                    Assert.AreEqual(n1, n2);
                    if (n1 == 0)
                        break;
                    for (var i = 0; i < n1; ++i)
                    {
                        if (buffer1[i] != buffer2[i])
                            Assert.Fail("data not the same");
                    }
                }
            }
            stopWatch.Stop();
            Console.WriteLine("Readfile file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);
        }


        [TestMethod]
        public async Task AddFile_Wrap()
        {
            var path = "hello.txt";
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var options = new AddFileOptions
                {
                    Wrap = true
                };
                var node = await ipfs.FileSystem.AddFileAsync(path, options);
                Assert.AreEqual("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ", (string)node.Id);
                Assert.AreEqual(true, node.IsDirectory);
                Assert.AreEqual(1, node.Links.Count());
                Assert.AreEqual("hello.txt", node.Links.First().Name);
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Links.First().Id);
                Assert.AreEqual(19, node.Links.First().Size);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public async Task Add_Raw()
        {
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                RawLeaves = true
            };
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            Assert.AreEqual("zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn", (string)node.Id);
            Assert.AreEqual(11, node.Size);
            Assert.AreEqual(0, node.Links.Count());
            Assert.AreEqual(false, node.IsDirectory);

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("hello world", text);
        }

        [TestMethod]
        public async Task Add_RawAndChunked()
        {
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                RawLeaves = true,
                ChunkSize = 3
            };
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.AreEqual("QmUuooB6zEhMmMaBvMhsMaUzar5gs5KwtVSFqG4C1Qhyhs", (string)node.Id);
            Assert.AreEqual(false, node.IsDirectory);
            Assert.AreEqual(4, links.Length);
            Assert.AreEqual("zb2rhm6D8PTYoMh7PSFKbCxxcD1yjWPD5KPr6nVRuw9ymDyUL", (string)links[0].Id);
            Assert.AreEqual("zb2rhgo7y6J7p76kCrXs4pmmMQx56fZeWJkC3sfbjeay4UruU", (string)links[1].Id);
            Assert.AreEqual("zb2rha4Pd2AruByr2RwzhRCVxRCqBC67h7ukTJd99jCjUtmyM", (string)links[2].Id);
            Assert.AreEqual("zb2rhn6eZLLj7vdVizbNxpASGoVw4vcSmc8avHXmDMVu5ZA6Q", (string)links[3].Id);

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            Assert.AreEqual("hello world", text);
        }

        [TestMethod]
        public async Task Add_OnlyHash()
        {
            var ipfs = TestFixture.Ipfs;
            var nodes = new string[] {
                "QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn",
                "QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6",
                "QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN",
                "QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6",
                "QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE"
            };
            foreach (var n in nodes) {
                await ipfs.Block.RemoveAsync(n, ignoreNonexistent: true);
            }

            var options = new AddFileOptions
            {
                ChunkSize = 3,
                OnlyHash = true,
            };
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.AreEqual(nodes[0], (string)node.Id);
            Assert.AreEqual(nodes.Length - 1, links.Length);
            for (var i = 0; i < links.Length; ++i)
            {
                Assert.AreEqual(nodes[i+1], (string)links[i].Id);
            }

            // TODO: Need a method to test that the CId is not held locally.
            //foreach (var n in nodes)
            //{
            //    Assert.IsNull(await ipfs.Block.StatAsync(n));
            //}
        }

        [TestMethod]
        public async Task ReadWithOffset()
        {
            var text = "hello world";
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            var node = await ipfs.FileSystem.AddTextAsync(text, options);

            for (var offset = 0; offset <= text.Length; ++offset)
            {
                using (var data = await ipfs.FileSystem.ReadFileAsync(node.Id, offset))
                using (var reader = new StreamReader(data))
                {
                    var s = reader.ReadToEnd();
                    Assert.AreEqual(text.Substring(offset), s);
                }
            }
        }

        [TestMethod]
        public async Task Read_RawWithLength()
        {
            var text = "hello world";
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                RawLeaves = true
            };
            var node = await ipfs.FileSystem.AddTextAsync(text, options);

            for (var offset = 0; offset < text.Length; ++offset)
            {
                for (var length = text.Length + 1; 0 < length; --length)
                {
                    using (var data = await ipfs.FileSystem.ReadFileAsync(node.Id, offset, length))
                    using (var reader = new StreamReader(data))
                    {
                        var s = reader.ReadToEnd();
                        Assert.AreEqual(text.Substring(offset, Math.Min(11 - offset, length)), s, $"o={offset} l={length}");
                    }
                }
            }
        }

        [TestMethod]
        public async Task Read_ChunkedWithLength()
        {
            var text = "hello world";
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            var node = await ipfs.FileSystem.AddTextAsync(text, options);

            for (var length = text.Length + 1; 0 < length; --length)
            {
                using (var data = await ipfs.FileSystem.ReadFileAsync(node.Id, 0, length))
                using (var reader = new StreamReader(data))
                {
                    var s = reader.ReadToEnd();
                    Assert.AreEqual(text.Substring(0, Math.Min(11, length)), s, $"l={length}");
                }
            }
        }

        [TestMethod]
        public void AddDirectory()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, false).Result;
                Assert.IsTrue(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.AreEqual(2, files.Length);
                Assert.AreEqual("alpha.txt", files[0].Name);
                Assert.AreEqual("beta.txt", files[1].Name);
                Assert.IsFalse(files[0].IsDirectory);
                Assert.IsFalse(files[1].IsDirectory);

                Assert.AreEqual("alpha", ipfs.FileSystem.ReadAllTextAsync(files[0].Id).Result);
                Assert.AreEqual("beta", ipfs.FileSystem.ReadAllTextAsync(files[1].Id).Result);

                Assert.AreEqual("alpha", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/alpha.txt").Result);
                Assert.AreEqual("beta", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/beta.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        public void AddDirectoryRecursive()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, true).Result;
                Assert.IsTrue(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.AreEqual(3, files.Length);
                Assert.AreEqual("alpha.txt", files[0].Name);
                Assert.AreEqual("beta.txt", files[1].Name);
                Assert.AreEqual("x", files[2].Name);
                Assert.IsFalse(files[0].IsDirectory);
                Assert.IsFalse(files[1].IsDirectory);
                Assert.IsTrue(files[2].IsDirectory);
                Assert.AreNotEqual(0, files[0].Size);
                Assert.AreNotEqual(0, files[1].Size);

                var rootFiles = ipfs.FileSystem.ListFileAsync(dir.Id).Result.Links.ToArray();
                Assert.AreEqual(3, rootFiles.Length);
                Assert.AreEqual("alpha.txt", rootFiles[0].Name);
                Assert.AreEqual("beta.txt", rootFiles[1].Name);
                Assert.AreEqual("x", rootFiles[2].Name);

                var xfiles = ipfs.FileSystem.ListFileAsync(rootFiles[2].Id).Result.Links.ToArray();
                Assert.AreEqual(2, xfiles.Length);
                Assert.AreEqual("x.txt", xfiles[0].Name);
                Assert.AreEqual("y", xfiles[1].Name);

                var yfiles = ipfs.FileSystem.ListFileAsync(xfiles[1].Id).Result.Links.ToArray();
                Assert.AreEqual(1, yfiles.Length);
                Assert.AreEqual("y.txt", yfiles[0].Name);
                Assert.IsFalse(yfiles[0].IsDirectory);

                Assert.AreEqual("x", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/x/x.txt").Result);
                Assert.AreEqual("y", ipfs.FileSystem.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        public void AddDirectory_WithHashAlgorithm()
        {
            var ipfs = TestFixture.Ipfs;
            var alg = "keccak-512";
            var options = new AddFileOptions { Hash = alg };
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, false, options).Result;
                Assert.IsTrue(dir.IsDirectory);
                Assert.AreEqual(alg, dir.Id.Hash.Algorithm.Name);

                foreach (var link in dir.Links)
                {
                    Assert.AreEqual(alg, link.Id.Hash.Algorithm.Name);
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        public void AddDirectory_WithCidEncoding()
        {
            var ipfs = TestFixture.Ipfs;
            var encoding = "base32z";
            var options = new AddFileOptions { Encoding = encoding };
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.FileSystem.AddDirectoryAsync(temp, false, options).Result;
                Assert.IsTrue(dir.IsDirectory);
                Assert.AreEqual(encoding, dir.Id.Encoding);

                foreach (var link in dir.Links)
                {
                    Assert.AreEqual(encoding, link.Id.Encoding);
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        [Ignore("Still not working")] // TODO
        public async Task ReadFromNetwork()
        {
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();

            try
            {
                var folder = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv";
                await ipfs.Block.RemoveAsync(folder, true);


                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)); // TODO
                var text = await ipfs.FileSystem.ReadAllTextAsync($"{folder}/about", cts.Token);
                StringAssert.Contains(text, "IPFS -- Inter-Planetary File system");
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        public static string MakeTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var x = Path.Combine(temp, "x");
            var xy = Path.Combine(x, "y");
            Directory.CreateDirectory(temp);
            Directory.CreateDirectory(x);
            Directory.CreateDirectory(xy);

            File.WriteAllText(Path.Combine(temp, "alpha.txt"), "alpha");
            File.WriteAllText(Path.Combine(temp, "beta.txt"), "beta");
            File.WriteAllText(Path.Combine(x, "x.txt"), "x");
            File.WriteAllText(Path.Combine(xy, "y.txt"), "y");
            return temp;
        }
    }
}
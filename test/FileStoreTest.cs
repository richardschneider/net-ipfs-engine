using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ipfs.Engine
{
    [TestClass]
    public class FileStoreTest
    {
        class Entity
        {
            public int Number;
            public string Value;
        }

        Entity a = new Entity { Number = 1, Value = "a" };
        Entity b = new Entity { Number = 2, Value = "b" };

        FileStore<int, Entity> Store
        {
            get
            {
                var folder = Path.Combine(TestFixture.Ipfs.Options.Repository.Folder, "test-filestore");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                return new FileStore<int, Entity>
                {
                    Folder = folder,
                    NameToKey = name => name.ToString(),
                    Serialize = (s, e) =>
                    {
                        var x = JsonConvert.SerializeObject(e);
                        var b = Encoding.UTF8.GetBytes(x);
                        s.Write(b, 0, b.Length);
                        return Task.CompletedTask;
                    },
                    Deserialize = (s) =>
                    {
                        var buffer = new byte[1024];
                        var n = s.Read(buffer, 0, buffer.Length);
                        var b = Encoding.UTF8.GetString(buffer, 0, n);
                        return Task.FromResult(JsonConvert.DeserializeObject<Entity>(b));
                    }
                };
            }
        }

        [TestMethod]
        public async Task PutAndGet()
        {
            var store = Store;

            await store.PutAsync(a.Number, a);
            await store.PutAsync(b.Number, b);

            var a1 = await store.GetAsync(a.Number);
            Assert.AreEqual(a.Number, a1.Number);
            Assert.AreEqual(a.Value, a1.Value);

            var b1 = await store.GetAsync(b.Number);
            Assert.AreEqual(b.Number, b1.Number);
            Assert.AreEqual(b.Value, b1.Value);
        }

        [TestMethod]
        public async Task TryGet()
        {
            var store = Store;
            await store.PutAsync(3, a);
            var a1 = await store.GetAsync(3);
            Assert.AreEqual(a.Number, a1.Number);
            Assert.AreEqual(a.Value, a1.Value);

            var a3 = await store.TryGetAsync(42);
            Assert.IsNull(a3);
        }

        [TestMethod]
        public void Get_Unknown()
        {
            var store = Store;

            ExceptionAssert.Throws<KeyNotFoundException>(() =>
            {
                var _ = Store.GetAsync(42).Result;
            });
        }

        [TestMethod]
        public async Task Remove()
        {
            var store = Store;
            await store.PutAsync(4, a);
            Assert.IsNotNull(await store.TryGetAsync(4));

            await store.RemoveAsync(4);
            Assert.IsNull(await store.TryGetAsync(4));
        }

        [TestMethod]
        public async Task Remove_Unknown()
        {
            var store = Store;
            await store.RemoveAsync(5);
        }

        [TestMethod]
        public async Task Length()
        {
            var store = Store;
            await store.PutAsync(6, a);
            var length = await store.LengthAsync(6);
            Assert.IsTrue(length.HasValue);
            Assert.AreNotEqual(0, length.Value);
        }

        [TestMethod]
        public async Task Length_Unknown()
        {
            var store = Store;
            var length = await store.LengthAsync(7);
            Assert.IsFalse(length.HasValue);
        }
    }
}

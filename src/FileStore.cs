using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   A repository for name value pairs.
    /// </summary>
    /// <typeparam name="TName">
    ///   The type used for a unique name.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The type used for the value.
    /// </typeparam>
    public class FileStore<TName, TValue>
        where TValue : class
    {

        /// <summary>
        ///   The fully qualififed path to a directory
        ///   that stores the name value pairs.
        /// </summary>
        /// <value>
        ///   A fully qualified path.
        /// </value>
        /// <remarks>
        ///   The directory must already exist.
        /// </remarks>
        public string Folder { get; set; }

        /// <summary>
        ///   A function that converts the name to a case insensitive key name.
        /// </summary>
        public Func<TName, string> NameToKey { get; set; }

        /// <summary>
        ///   Sends the value to the stream.
        /// </summary>
        public Func<Stream, TValue, Task> Serialize { get; set; }

        /// <summary>
        ///   Retrieves the value from the stream.
        /// </summary>
        public Func<Stream, Task<TValue>> Deserialize { get; set; }

        /// <summary>
        ///   Try to get the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <typeparamref name="TValue"/> or <b>null</b> if the <paramref name="name"/>
        ///   does not exist.
        /// </returns>
        public async Task<TValue> TryGetAsync(TName name, CancellationToken cancel = default(CancellationToken))
        {
            var path = GetPath(name);
            if (!File.Exists(path))
            {
                return null;
            }

            using (var content = File.OpenRead(path))
            {
                return await Deserialize(content);
            }
        }

        /// <summary>
        ///   Get the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <typeparamref name="TValue"/>
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///   When the <paramref name="name"/> does not exist.
        /// </exception>
        public async Task<TValue> GetAsync(TName name, CancellationToken cancel = default(CancellationToken))
        {
            var value = await TryGetAsync(name, cancel);
            if (value == null)
                throw new KeyNotFoundException($"Missing '{name}'.");

            return value;
        }

        /// <summary>
        ///   Put the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The unique name of the entity.
        /// </param>
        /// <param name="value">
        ///   The entity.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   If <paramref name="name"/> already exists, it's value is overwriten.
        /// </remarks>
        public async Task PutAsync (TName name, TValue value, CancellationToken cancel = default(CancellationToken))
        {
            var path = GetPath(name);

            using (var stream = File.Create(path))
            {
                await Serialize(stream, value);
            }
        }

        /// <summary>
        ///   Remove the value with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   A non-existent <paramref name="name"/> does nothing.
        /// </remarks>
        public Task RemoveAsync(TName name, CancellationToken cancel = default(CancellationToken))
        {
            var path = GetPath(name);
            File.Delete(path);
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Get's the serialised length of the entity.
        /// </summary>
        /// <param name="name">
        ///   The unique name of the entity.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a nullable long.
        /// </returns>
        /// <remarks>
        ///   Return a null when the <paramref name="name"/> does not exist.
        /// </remarks>
        public Task<long?> LengthAsync(TName name, CancellationToken cancel = default(CancellationToken))
        {
            var path = GetPath(name);
            var fi = new FileInfo(path);
            long? length = null;
            if (fi.Exists)
                length = fi.Length;
            return Task.FromResult(length);
        }

        /// <summary>
        ///   Local file system path of the name.
        /// </summary>
        string GetPath(TName name)
        {
            return Path.Combine(Folder, NameToKey(name));
        }


    }
}

using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer.Protocols
{
    /// <summary>
    ///   A name with a semantic version.
    /// </summary>
    /// <remarks>
    ///   Implements value type equality.
    /// </remarks>
    public class VersionedName : IEquatable<VersionedName>, IComparable<VersionedName>
    {
        /// <summary>
        ///   The name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   The semantic version.
        /// </summary>
        public SemVersion Version { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <summary>
        ///   Parse
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static VersionedName Parse (string s)
        {
            var parts = s.Split('/').Where(p => p.Length > 0).ToArray();
            return new VersionedName
            {
                Name = string.Join("/", parts, 0, parts.Length - 1),
                Version = SemVersion.Parse(parts[parts.Length - 1])
            };
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as VersionedName;
            return (that == null)
                ? false
                : this.Name == that.Name && this.Version == that.Version;
        }

        /// <inheritdoc />
        public bool Equals(VersionedName that)
        {
            return this.Name == that.Name && this.Version == that.Version;
        }

        /// <summary>
        ///   Value equality.
        /// </summary>
        public static bool operator ==(VersionedName a, VersionedName b)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (object.ReferenceEquals(a, null)) return false;
            if (object.ReferenceEquals(b, null)) return false;

            return a.Equals(b);
        }

        /// <summary>
        ///   Value inequality.
        /// </summary>
        public static bool operator !=(VersionedName a, VersionedName b)
        {
            if (object.ReferenceEquals(a, b)) return false;
            if (object.ReferenceEquals(a, null)) return true;
            if (object.ReferenceEquals(b, null)) return true;

            return !a.Equals(b);
        }

        /// <inheritdoc />
        public int CompareTo(VersionedName that)
        {
            if (that == null) return 1;
            if (this.Name == that.Name) return this.Version.CompareTo(that.Version);
            return this.Name.CompareTo(that.Name);
        }
    }
}

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 0649 // disable warning about unassinged fields
#pragma warning disable 0169// disable warning about unassinged fields

namespace Ipfs.Engine.UnixFileSystem
{
    enum DataType
    {
        Raw = 0,
        Directory = 1,
        File = 2,
        Metadata = 3,
        Symlink = 4,
        HAMTShard = 5
    };

    /// <summary>
    ///   The ProtoBuf data that is stored in a DAG.
    /// </summary>
    [ProtoContract]
    internal class DataMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public DataType Type;

        [ProtoMember(2, IsRequired = false)]
        public byte[] Data;

        [ProtoMember(3, IsRequired = false)]
        public ulong? FileSize;

        [ProtoMember(4, IsRequired = false)]
        public ulong[] BlockSizes;

#pragma warning disable 0649 // disable warning about unassinged fields
        [ProtoMember(5, IsRequired = false)]
        public ulong? HashType;

#pragma warning disable 0649 // disable warning about unassinged fields
        [ProtoMember(6, IsRequired = false)]
        public ulong? Fanout;
    }
}

/*
 *module.exports = `message Data
    {
  enum DataType
    {
        Raw = 0;
        Directory = 1;
        File = 2;
        Metadata = 3;
        Symlink = 4;
        HAMTShard = 5;
    }
    required DataType Type = 1;
  optional bytes Data = 2;
  optional uint64 filesize = 3;
  repeated uint64 blocksizes = 4;
  optional uint64 hashType = 5;
  optional uint64 fanout = 6;
}
message Metadata
{
    required string MimeType = 1;
}
*/

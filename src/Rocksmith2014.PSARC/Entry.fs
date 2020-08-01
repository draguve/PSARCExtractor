﻿namespace Rocksmith2014.PSARC

open System.IO
open Rocksmith2014.Common.Interfaces

type Entry =
    { NameDigest : byte[]
      zIndexBegin : uint32
      Length : uint64
      Offset : uint64
      ID : int32 }

    member this.Write (writer: IBinaryWriter) =
        writer.WriteBytes this.NameDigest
        writer.WriteUInt32 this.zIndexBegin
        writer.WriteUInt64 this.Length
        writer.WriteUInt64 this.Offset
        writer.WriteInt32 this.ID

type NamedEntry =
    { Name: string
      Data: Stream }
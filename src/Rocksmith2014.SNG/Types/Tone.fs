﻿namespace Rocksmith2014.SNG

open Interfaces

[<Struct>]
type Tone =
    { Time : float32
      ToneId : int32 }

    interface IBinaryWritable with
        member this.Write(writer) =
            writer.WriteSingle this.Time
            writer.WriteInt32 this.ToneId

    static member Read(reader : IBinaryReader) =
        { Time = reader.ReadSingle()
          ToneId = reader.ReadInt32() }
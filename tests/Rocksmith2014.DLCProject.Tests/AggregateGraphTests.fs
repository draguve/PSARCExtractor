module AggregateGraphTests

open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System.IO

let private sl = { XML = "showlights.xml" }

let private project = { testProject with Arrangements = [ Vocals testJVocals; Showlights sl; Instrumental testLead ] }

[<Tests>]
let aggregateGraphTests =
    testList "Aggregate Graph Tests" [
        test "Can be serialized" {
            use stream = new MemoryStream()
            let graph = AggregateGraph.create PC project
            AggregateGraph.serialize stream graph
        
            stream.Position <- 0L
            let str = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())
        
            Expect.isFalse (str.Contains "\r\n") "Unix newline is used"
            Expect.stringEnds str "." "Does not end with a newline"
        }
        
        test "Can be parsed from a string" {
            use stream = new MemoryStream()
            let graph = AggregateGraph.create PC project
            AggregateGraph.serialize stream graph
        
            stream.Position <- 0L
            let str = using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())
            let parsed = AggregateGraph.parse str
        
            Expect.equal graph parsed "Parsed graph is equal to serialized graph."
        }
        
        test "Graph items are created correctly: Main sound bank (PC)" {
            let a = AggregateGraph.create PC project
            let bnk = a.Items |> List.find (fun x -> x.Name = "song_sometest")
        
            Expect.equal bnk.Canonical "/audio/windows" "Canonical is correct"
            Expect.equal bnk.RelPath "/audio/windows/song_sometest.bnk" "Relative path is correct"
            Expect.stringEnds ((Option.get bnk.LLID).ToString()) "-0000-0000-0000-000000000000" "LLID ending is all zeroes"
            Expect.equal (Option.get bnk.LogPath) "/audio/song_sometest.bnk" "Logical path is correct"
            Expect.sequenceEqual bnk.Tags [ "audio"; "wwise-sound-bank"; "dx9" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Preview sound bank (Mac)" {
            let a = AggregateGraph.create Mac project
            let bnk = a.Items |> List.find (fun x -> x.Name = "song_sometest_preview")
        
            Expect.equal bnk.Canonical "/audio/mac" "Canonical is correct"
            Expect.equal bnk.RelPath "/audio/mac/song_sometest_preview.bnk" "Relative path is correct"
            Expect.equal (Option.get bnk.LogPath) "/audio/song_sometest_preview.bnk" "Logical path is correct"
            Expect.sequenceEqual bnk.Tags [ "audio"; "wwise-sound-bank"; "macos" ] "Has correct tags"
        }

        test "Graph items are created correctly: Custom audio sound bank" {
            let project = { project with Arrangements = [ Instrumental { testLead with CustomAudio = Some { Path = "Test.wem"; Volume = 0. } } ]}
            let a = AggregateGraph.create PC project
            let bnk = a.Items |> List.find (fun x -> x.Name = "song_sometest_lead")
        
            Expect.equal bnk.Canonical "/audio/windows" "Canonical is correct"
            Expect.equal bnk.RelPath "/audio/windows/song_sometest_lead.bnk" "Relative path is correct"
            Expect.equal (Option.get bnk.LogPath) "/audio/song_sometest_lead.bnk" "Logical path is correct"
        }
        
        test "Graph items are created correctly: Lead arrangement SNG (PC)" {
            let a = AggregateGraph.create PC project
            let item =
                a.Items
                |> List.find (fun x -> x.Name = "sometest_lead" && x.Tags = [ "application"; "musicgame-song" ])
        
            Expect.equal item.Canonical "/songs/bin/generic" "Canonical is correct"
            Expect.equal item.RelPath "/songs/bin/generic/sometest_lead.sng" "Relative path is correct"
            Expect.equal (Option.get item.LogPath) "/songs/bin/sometest_lead.sng" "Logical path is correct"
        }
        
        test "Graph items are created correctly: Lead arrangement SNG (Mac)" {
            let a = AggregateGraph.create Mac project
            let item =
                a.Items
                |> List.find (fun x -> x.Name = "sometest_lead" && List.contains "musicgame-song" x.Tags)
        
            Expect.equal item.Canonical "/songs/bin/macos" "Canonical is correct"
            Expect.equal item.RelPath "/songs/bin/macos/sometest_lead.sng" "Relative path is correct"
            Expect.equal (Option.get item.LogPath) "/songs/bin/sometest_lead.sng" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "application"; "musicgame-song"; "macos" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Lead arrangement JSON" {
            let a = AggregateGraph.create PC project
            let item =
                a.Items
                |> List.find (fun x -> x.Name = "sometest_lead" && x.Tags = [ "database"; "json-db" ])
        
            Expect.equal item.Canonical "/manifests/songs_dlc_sometest" "Canonical is correct"
            Expect.equal item.RelPath "/manifests/songs_dlc_sometest/sometest_lead.json" "Relative path is correct"
        }
        
        test "Graph items are created correctly: Show lights" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "sometest_showlights")
        
            Expect.equal item.Canonical "/songs/arr" "Canonical is correct"
            Expect.equal item.RelPath "/songs/arr/sometest_showlights.xml" "Relative path is correct"       
            Expect.equal (Option.get item.LogPath) "/songs/arr/sometest_showlights.xml" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "application"; "xml" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Album art small" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "album_sometest_64")
        
            Expect.equal item.Canonical "/gfxassets/album_art" "Canonical is correct"
            Expect.equal item.RelPath "/gfxassets/album_art/album_sometest_64.dds" "Relative path is correct"       
            Expect.equal (Option.get item.LogPath) "/gfxassets/album_art/album_sometest_64.dds" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "dds"; "image" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Album art medium" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "album_sometest_128")
        
            Expect.equal item.Canonical "/gfxassets/album_art" "Canonical is correct"
            Expect.equal item.RelPath "/gfxassets/album_art/album_sometest_128.dds" "Relative path is correct"       
            Expect.equal (Option.get item.LogPath) "/gfxassets/album_art/album_sometest_128.dds" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "dds"; "image" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Album art large" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "album_sometest_256")
        
            Expect.equal item.Canonical "/gfxassets/album_art" "Canonical is correct"
            Expect.equal item.RelPath "/gfxassets/album_art/album_sometest_256.dds" "Relative path is correct"       
            Expect.equal (Option.get item.LogPath) "/gfxassets/album_art/album_sometest_256.dds" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "dds"; "image" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: Custom font" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "lyrics_sometest")
        
            Expect.equal item.Canonical "/assets/ui/lyrics/sometest" "Canonical is correct"
            Expect.equal item.RelPath "/assets/ui/lyrics/sometest/lyrics_sometest.dds" "Relative path is correct"       
            Expect.equal (Option.get item.LogPath) "/assets/ui/lyrics/sometest/lyrics_sometest.dds" "Logical path is correct"
            Expect.sequenceEqual item.Tags [ "dds"; "image" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: X-Block" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "sometest")
        
            Expect.equal item.Canonical "/gamexblocks/nsongs" "Canonical is correct"
            Expect.equal item.RelPath "/gamexblocks/nsongs/sometest.xblock" "Relative path is correct"       
            Expect.sequenceEqual item.Tags [ "emergent-world"; "x-world" ] "Has correct tags"
        }
        
        test "Graph items are created correctly: HSAN" {
            let a = AggregateGraph.create PC project
            let item = a.Items |> List.find (fun x -> x.Name = "songs_dlc_sometest")
        
            Expect.equal item.Canonical "/manifests/songs_dlc_sometest" "Canonical is correct"
            Expect.equal item.RelPath "/manifests/songs_dlc_sometest/songs_dlc_sometest.hsan" "Relative path is correct"       
            Expect.sequenceEqual item.Tags [ "database"; "hsan-db" ] "Has correct tags"
        }
    ]

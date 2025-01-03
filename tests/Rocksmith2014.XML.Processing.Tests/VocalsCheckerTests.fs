module VocalsCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let vocalsTests =
    testList "Arrangement Checker (Vocals)" [
        testCase "Detects character not in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test+"); Vocal(100, 50, "Nope:あ") })

            let result = VocalsChecker.check false vocals

            Expect.equal result.Head.Type (LyricWithInvalidChar 'あ') "Issue type is correct"

        testCase "Accepts characters in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test+"); Vocal(100, 50, "ÄöÖÅå"); Vocal(200, 50, "àè- +?&#\"") })

            let result = VocalsChecker.check false vocals

            Expect.isEmpty result "Checker returned an empty list"

        testCase "Detects lyric that is too long (ASCII)" <| fun _ ->
            let lyric = String.replicate 48 "A"
            let vocals = ResizeArray(seq { Vocal(0, 10, "Test+"); Vocal(0, 50, lyric) })

            let result = VocalsChecker.check false vocals

            Expect.equal result.Head.Type (LyricTooLong lyric) "Issue type is correct"

        testCase "Detects lyric that is too long (non-ASCII)" <| fun _ ->
            let lyric = String.replicate 16 "あ" // 48 bytes in UTF8
            let vocals = ResizeArray(seq { Vocal(0, 100, "Test+"); Vocal(0, 50, lyric) })

            let result = VocalsChecker.check true vocals

            Expect.hasLength result 1 "One issue created"
            Expect.equal result.Head.Type (LyricTooLong lyric) "Issue type is correct"

        testCase "Detects lyrics without line breaks" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Line"); Vocal(0, 100, "Test+") })

            let result = VocalsChecker.check true vocals

            Expect.hasLength result 1 "One issue created"
            Expect.equal result.Head.Type LyricsHaveNoLineBreaks "Issue type is correct"
    ]

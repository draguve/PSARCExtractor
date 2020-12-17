﻿module Rocksmith2014.XML.Processing.Tests.ArrangementCheckerTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing.ArrangementChecker

let toneChanges = ResizeArray(seq { ToneChange("test", 5555, 1uy) })
let sections = ResizeArray(seq { Section("noguitar", 6000, 1s); Section("riff", 6500, 1s) })
let chordTemplates = ResizeArray(seq { ChordTemplate("", "", [| 2y; 2y; -1y; -1y; -1y; -1y |], [| 2y; 2y; -1y; -1y; -1y; -1y |]) })
let testArr = InstrumentalArrangement(Sections = sections, ChordTemplates = chordTemplates)
testArr.Tones.Changes <- toneChanges

[<Tests>]
let eventTests =
    testList "Arrangement Checker (Events)" [
        testCase "Detects missing applause end event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type ApplauseEventWithoutEnd "Correct issue type"

        testCase "Detects unexpected crowd speed event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("e2", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type (EventBetweenIntroApplause "e2") "Correct issue type"

        testCase "Detects unexpected intro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("E3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type (EventBetweenIntroApplause "E3") "Correct issue type"

        testCase "Detects unexpected outro applause event" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type (EventBetweenIntroApplause "D3") "Correct issue type"

        testCase "Detects multiple unexpected events" <| fun _ ->
            let xml = InstrumentalArrangement()
            xml.Events.Add(Event("E3", 1000))
            xml.Events.Add(Event("D3", 2000))
            xml.Events.Add(Event("e0", 3000))
            xml.Events.Add(Event("e1", 3000))
            xml.Events.Add(Event("E13", 5000))

            let results = checkCrowdEventPlacement xml

            Expect.hasLength results 3 "Three issues created"
    ]

[<Tests>]
let noteTests =
    testList "Arrangement Checker (Notes)" [
        testCase "Detects unpitched slide note with linknext" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 12y)
                                          Note(Fret = 12y, Time = 100)})
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects note with both harmonic and pinch harmonic" <| fun _ ->
            let notes = ResizeArray(seq { Note(IsPinchHarmonic = true, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects notes beyond 23rd fret without ignore status" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 23y)
                                          Note(Fret = 24y)
                                          Note(Fret = 23y, IsIgnore = true)
                                          Note(Fret = 24y, IsIgnore = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 2 "Two issues created"

        testCase "Detects harmonic note on 7th fret with sustain" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 7y, IsHarmonic = true, Sustain = 200); Note(Fret = 7y, IsHarmonic = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Ignores harmonic note on 7th fret with sustain when ignore set" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 7y, IsHarmonic = true, Sustain = 200, IsIgnore = true) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects note with missing bend values" <| fun _ ->
            let bendValues = ResizeArray(seq { BendValue() })
            let notes = ResizeArray(seq { Note(Fret = 7y, BendValues = bendValues) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects tone change that occurs on a note" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 5555) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects note inside noguitar section" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 6000) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects linknext fret mismatch" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100)
                                          Note(Fret = 5y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type LinkNextFretMismatch "Correct issue type"

        testCase "Detects note linked to a chord" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100) })
            let cn = ResizeArray(seq { Note(Fret = 1y, Time = 1100) })
            let chords = ResizeArray(seq { Chord(Time = 1100, ChordNotes = cn) })
            let level = Level(Notes = notes, Chords = chords)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type NoteLinkedToChord "Correct issue type"

        testCase "Detects linknext slide fret mismatch" <| fun _ ->
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, SlideTo = 4y)
                                          Note(Fret = 5y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type LinkNextSlideMismatch "Correct issue type"

        testCase "Detects linknext bend value mismatch" <| fun _ ->
            let bv1 = ResizeArray(seq { BendValue(1050, 1f) })
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, BendValues = bv1)
                                          Note(Fret = 1y, Time = 1100) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type LinkNextBendMismatch "Correct issue type"

        testCase "Does not produce false positive when no bend value at note time" <| fun _ ->
            let bv1 = ResizeArray(seq { BendValue(1000, 1f); BendValue(1050, 0f) })
            let bv2 = ResizeArray(seq { BendValue(1150, 1f) })
            let notes = ResizeArray(seq { Note(Fret = 1y, Time = 1000, IsLinkNext = true, Sustain = 100, BendValues = bv1)
                                          Note(Fret = 1y, Time = 1100, Sustain = 100, BendValues = bv2) })
            let level = Level(Notes = notes)

            let results = checkNotes testArr level

            Expect.hasLength results 0 "No issues created"
    ]

[<Tests>]
let chordTests =
    testList "Arrangement Checker (Chords)" [
        testCase "Detects chord with inconsistent chord note sustains" <| fun _ ->
            let cn = ResizeArray(seq { Note(Sustain = 200); Note(Sustain = 400) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord note with linknext and unpitched slide" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsLinkNext = true, SlideUnpitchTo = 10y) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord note with both harmonic and pinch harmonic" <| fun _ ->
            let cn = ResizeArray(seq { Note(IsHarmonic = true, IsPinchHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord beyond 23rd fret without ignore" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 23y); Note(Fret = 24y) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects harmonic chord note on 7th fret with sustain" <| fun _ ->
            let cn = ResizeArray(seq { Note(Fret = 7y, Sustain = 200, IsHarmonic = true) })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects tone change that occurs on a chord" <| fun _ ->
            let cn = ResizeArray(seq { Note() })
            let chords = ResizeArray(seq { Chord(ChordNotes = cn, Time = 5555) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord at the end of handshape" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(1s, 6500, 7000) })
            let chords = ResizeArray(seq { Chord(ChordId = 1s, Time = 7000) })
            let level = Level(Chords = chords, HandShapes = hs)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord inside noguitar section" <| fun _ ->
            let chords = ResizeArray(seq { Chord(Time = 6100) })
            let level = Level(Chords = chords)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord note linknext slide fret mismatch" <| fun _ ->
            let cn = ResizeArray(seq { Note(Time = 1000, Sustain = 100, IsLinkNext = true, Fret = 1y, SlideTo = 3y) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn, IsLinkNext = true) })
            let notes = ResizeArray(seq { Note(Time = 1100, Fret = 12y) })
            let level = Level(Chords = chords, Notes = notes)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"

        testCase "Detects chord note linknext bend value mismatch" <| fun _ ->
            let bv = ResizeArray(seq { BendValue(1050, 1f) })
            let cn = ResizeArray(seq { Note(Time = 1000, Sustain = 100, IsLinkNext = true, Fret = 1y, BendValues = bv) })
            let chords = ResizeArray(seq { Chord(Time = 1000, ChordNotes = cn, IsLinkNext = true) })
            let notes = ResizeArray(seq { Note(Time = 1100, Fret = 1y) })
            let level = Level(Chords = chords, Notes = notes)

            let results = checkChords testArr level

            Expect.hasLength results 1 "One issue created"
    ]

[<Tests>]
let handshapeTests =
    testList "Arrangement Checker (Handshapes)" [
        testCase "Detects fingering that does not match anchor position" <| fun _ ->
            let hs = ResizeArray(seq { HandShape(0s, 1000, 1500) })
            let anchors = ResizeArray(seq { Anchor(2y, 500) })
            let level = Level(HandShapes = hs, Anchors = anchors)

            let results = checkHandshapes testArr level

            Expect.hasLength results 1 "One issue created"
    ]

[<Tests>]
let anchorTests =
    testList "Arrangement Checker (Anchors)" [
        testCase "Ignores anchor exactly on note" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 100) })
            let notes = ResizeArray(seq { Note(Time = 100, Fret = 1y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors level

            Expect.hasLength results 0 "No issues created"

        testCase "Detects anchor before note" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 99) })
            let notes = ResizeArray(seq { Note(Time = 100, Fret = 1y) })
            let level = Level(Notes = notes, Anchors = anchors)

            let results = checkAnchors level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type (AnchorNotOnNote -1) "Correct issue type"

        testCase "Detects anchor after chord" <| fun _ ->
            let anchors = ResizeArray(seq { Anchor(1y, 102) })
            let chords = ResizeArray(seq { Chord(Time = 100) })
            let level = Level(Chords = chords, Anchors = anchors)

            let results = checkAnchors level

            Expect.hasLength results 1 "One issue created"
            Expect.equal results.[0].Type (AnchorNotOnNote 2) "Correct issue type"
    ]

[<Tests>]
let vocalsTests =
    testList "Arrangement Checker (Vocals)" [
        testCase "Detects character not in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test"); Vocal(100, 50, "Nope:あ")})

            let result = checkVocals vocals

            Expect.isSome result "Checker returned an issue"

        testCase "Accepts character in default font" <| fun _ ->
            let vocals = ResizeArray(seq { Vocal(0, 50, "Test"); Vocal(100, 50, "ÄöÖÅå"); Vocal(200, 50, "àè- +?&#\"") })

            let result = checkVocals vocals

            Expect.isNone result "Checker returned None"
    ]

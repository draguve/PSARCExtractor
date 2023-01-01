open System
open System.IO
open Microsoft.VisualBasic
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PsarcImportTypes
open Rocksmith2014.XML
open DLCBuilder.Utils
open DLCBuilder.GeneralTypes

type Platform = PC | Mac

type LevelCountGeneration =
    | Simple
    | MLModel
    /// Generates the same number of levels for all phrases. For testing purposes.
    | Constant of levelCount: int
//
// type AudioConversionType =
//     | ToWav
//     | ToOgg

type Locale =
    { Name: string
      ShortName: string }

type AppId =
    | AppId of uint64


type Configuration =
    {
      RemoveDDOnImport: bool
      CreateEOFProjectOnImport: bool
      ConvertAudio: AudioConversionType option
    }


// let rec getAllFiles dir pattern =
//     seq { yield! Directory.EnumerateFiles(dir, pattern)
//           for d in Directory.EnumerateDirectories(dir) do
//               yield! getAllFiles d pattern }
//
// getAllFiles "Downloads" "*.psarc"
// |> Seq.iter (printfn "%s")
/// Removes DD from the arrangements.

let progress = fun () -> printfn "Progress"

let importPsarc config targetFolder (psarcPath: string)  =
    async {
        Directory.CreateDirectory(targetFolder) |> ignore
        let! r = PsarcImporter.import progress psarcPath targetFolder
        let project = r.GeneratedProject

        let audioExtension =
            match config.ConvertAudio with
            | None ->
                "wem"
            | Some conv ->
                convertProjectAudioFromWem conv project
                conv.ToExtension

        // Remove DD levels
        if config.RemoveDDOnImport then
            let instrumentalData =
                r.ArrangementData
                |> List.choose (fun (arr, data) ->
                    match data with
                    | ImportedData.Instrumental data ->
                        Some (Arrangement.getFile arr, data)
                    | _ ->
                        None)

            do! removeDD instrumentalData

        let audioFilePath = Path.ChangeExtension(project.AudioFile.Path, audioExtension)
        let previewFilePath = Path.ChangeExtension(project.AudioPreviewFile.Path, audioExtension)
        let oggFileName =
            match config.ConvertAudio with
            | Some ToOgg ->
                Path.GetFileName(audioFilePath)
            | _ ->
                String.Empty

        // Create EOF project
        // if config.CreateEOFProjectOnImport then
        //     let eofProjectPath = Path.Combine(Path.GetDirectoryName(r.ProjectPath), "notes.eof")
        //     let eofTracks = createEofTrackList r.ArrangementData
        //     EOFProjectWriter.writeEofProject oggFileName eofProjectPath eofTracks

        let audioFile = { project.AudioFile with Path = audioFilePath }
        let previewFile = { project.AudioPreviewFile with Path = previewFilePath }
        let project = { project with AudioFile = audioFile; AudioPreviewFile = previewFile }

        return { r with GeneratedProject = project }
    }


[<EntryPoint>]
let main argv =
    let config = {
        RemoveDDOnImport = true
        CreateEOFProjectOnImport = false
        ConvertAudio = Some ToOgg
    }
    importPsarc config "C:\\Users\\ritwi\\Github\\Testing\\" "C:\\Users\\ritwi\\Github\\Rocksmith\\Downloads\\73718\\PleaseComeHomeForChristmas_AChristmasCelebrationOfHope.psarc" |> Async.RunSynchronously |> ignore
    Console.Read()

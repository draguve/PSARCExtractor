open System
open System.IO
open Microsoft.VisualBasic
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PsarcImportTypes
open Rocksmith2014.XML
open DLCBuilder.Utils
open DLCBuilder.GeneralTypes

type Configuration =
    {
      RemoveDDOnImport: bool
      CreateEOFProjectOnImport: bool
      ConvertAudio: AudioConversionType option
    }

let progress = fun () -> ()

let importPsarc config targetFolder (psarcPath: string)  =
    async {
        Directory.CreateDirectory(targetFolder) |> ignore
        let! r = PsarcImporter.import progress psarcPath targetFolder
        let project = r.GeneratedProject

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

        let audioExtension =
            match config.ConvertAudio with
            | None ->
                "wem"
            | Some conv ->
                convertProjectAudioFromWem conv project
                conv.ToExtension
        let audioFilePath = Path.ChangeExtension(project.AudioFile.Path, audioExtension)
        let previewFilePath = Path.ChangeExtension(project.AudioPreviewFile.Path, audioExtension)
        let oggFileName =
            match config.ConvertAudio with
            | Some ToOgg ->
                Path.GetFileName(audioFilePath)
            | _ ->
                String.Empty

        let audioFile = { project.AudioFile with Path = audioFilePath }
        let previewFile = { project.AudioPreviewFile with Path = previewFilePath }
        let project = { project with AudioFile = audioFile; AudioPreviewFile = previewFile }

        return { r with GeneratedProject = project }
    }

let config = {
    RemoveDDOnImport = true
    CreateEOFProjectOnImport = false
    ConvertAudio = None
}

let processFile filename outputDir =
   try
       let directory = outputDir
       printfn $"%s{directory}"
       importPsarc config directory filename |> Async.RunSynchronously |> ignore
   with ex ->
        printfn $"Problem could not decompress %s{filename}"
        printfn $"Exception: %s{ex.Message}"
        printfn $"Stack Trace: %s{ex.StackTrace}"

[<EntryPoint>]
let main argv =
    // Expecting two arguments: input file path and output directory
    if argv.Length < 2 then
        printfn "Usage: <input file path> <output directory>"
        1 // Exit with error
    else
        let inputFilePath = argv.[0]
        let outputDir = argv.[1]
        processFile inputFilePath outputDir
        0

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

let rec getAllFiles dir pattern =
    seq { yield! Directory.EnumerateFiles(dir, pattern)
          for d in Directory.EnumerateDirectories(dir) do
              yield! getAllFiles d pattern }


let progress = fun () -> printfn "Progress"

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

let config = {
    RemoveDDOnImport = true
    CreateEOFProjectOnImport = false
    ConvertAudio = None
}

let processFile filename =
   try
       let directory = Path.Combine(Path.GetDirectoryName(filename:string),Path.GetFileNameWithoutExtension(filename:string))
       printfn $"%s{directory}"
       importPsarc config directory filename |> Async.RunSynchronously |> ignore
       File.Delete(filename)
   with ex ->
       printfn $"Problem could not decompress %s{filename}"
[<EntryPoint>]
let main argv =
    getAllFiles "Downloads" "*.psarc" |> Seq.iter processFile
    0

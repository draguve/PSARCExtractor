open System
open System.IO
open Microsoft.VisualBasic
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PsarcImportTypes
open Rocksmith2014.XML
open DLCBuilder.Utils
open DLCBuilder.GeneralTypes
open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.SNG
open Rocksmith2014.Common.Manifest
open PsarcImportUtils

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

let printList lst = lst |> List.iter (fun item -> printfn "%s" item)
let filterFilesWithExtension extension = List.filter (String.endsWith extension)

let processXblock outputDir (xblock:string) psarcContents (psarc:PSARC) platform =
    async {
        try
            printfn $"Processing {xblock}"
            let dlcKey = Path.GetFileNameWithoutExtension(xblock)
            let subDir = Path.Combine(outputDir, dlcKey)
            Directory.CreateDirectory(subDir) |> ignore
            printfn $"DLC KEY {dlcKey}"
            let artFile = List.filter (String.endsWith "256.dds") psarcContents |> List.find (String.contains dlcKey)
            do! psarc.InflateFile(artFile, $"{subDir}/cover.dds")
            let toTargetPath filename = Path.Combine(subDir, filename)


            let! sngs =
                psarcContents
                |> List.filter(String.contains dlcKey)
                |> filterFilesWithExtension "sng"
                |> List.map (fun file ->
                    async {
                        use! stream = psarc.GetEntryStream(file)
                        let! sng = SNG.fromStream stream platform
                        return file, sng
                    })
                |> Async.Sequential

            let! fileAttributes =
                psarcContents
                |> List.filter(String.contains dlcKey)
                |> filterFilesWithExtension "json"
                |> List.map (fun file ->
                    async {
                        use! stream = psarc.GetEntryStream(file)
                        let! manifest = Manifest.fromJsonStream stream
                        return file, Manifest.getSingletonAttributes manifest
                    })
                |> Async.Sequential

            let! targetAudioFilesById =
                psarcContents
                |> List.filter(String.contains dlcKey)
                |> filterFilesWithExtension "bnk"
                |> List.map (fun bankName ->
                    async {
                        let! volume, id = getVolumeAndFileId psarc platform bankName
                        let targetFilename = createTargetAudioFilename bankName

                        let audio =
                            { Path = toTargetPath targetFilename
                              Volume = Math.Round(float volume, 1) }

                        return string id, audio
                    })
                |> Async.Sequential

            let targetAudioFiles = targetAudioFilesById |> Array.map snd

            let mainAudio =
                targetAudioFiles
                |> Array.find (fun audio -> String.endsWith $"{dlcKey}.wem" audio.Path)

            let previewAudio =
                targetAudioFiles
                |> Array.find (fun audio -> String.endsWith $"{dlcKey}_preview.wem" audio.Path)

            // Extract audio files
            do! targetAudioFilesById
                |> Array.map (fun (id, targetFile) ->
                    async {
                        match psarcContents |> List.tryFind (String.contains id) with
                        | Some psarcPath ->
                            do! psarc.InflateFile(psarcPath, targetFile.Path)
                        | None ->
                            ()
                    })
                |> Async.Sequential
                |> Async.Ignore

            let arrangements =
                sngs
                |> Array.Parallel.map (fun (file, sng) ->
                    // Change the filenames from "dlckey_name" to "arr_name"
                    let targetFile =
                        let f = Path.GetFileName(file)
                        toTargetPath <| Path.ChangeExtension("arr" + f.Substring(f.IndexOf '_'), "xml")

                    let attributes =
                        fileAttributes
                        |> Array.find (fun (mFile, _) ->
                            Path.GetFileNameWithoutExtension(mFile) = Path.GetFileNameWithoutExtension(file))
                        |> snd

                    let importVocals' = importVocals subDir targetFile attributes sng

                    match file with
                    | JVocalsFile ->
                        importVocals' true
                    | VocalsFile ->
                        importVocals' false
                    | InstrumentalFile ->
                        importInstrumental targetAudioFiles dlcKey targetFile attributes sng)
                |> Array.toList
                |> List.add (Showlights { XML = toTargetPath "arr_showlights.xml" }, ImportedData.ShowLights)
                |> List.sortBy (fst >> Arrangement.sorter)


            let tones =
                fileAttributes
                |> Array.choose (fun (_, attr) -> Option.ofObj attr.Tones)
                |> Array.concat
                // Filter out null values and tones without amps
                |> Array.filter (fun x -> notNull (box x) && notNull x.GearList.Amp)
                |> Array.distinctBy (fun x -> x.Key)
                |> Array.toList
                |> List.map toneFromDto

            let metaData =
                fileAttributes
                |> Array.tryPick (fun (file, attr) -> if file.Contains("vocals") then None else Some attr)
                // No instrumental arrangements in PSARC, should not happen
                |> Option.defaultWith (fun () -> fileAttributes[0] |> snd)

            let! version, author, toolkitVersion =
                tryGetFileContents "toolkit.version" psarc
                |> Async.map (function
                    | None ->
                        "1", None, None
                    | Some text ->
                        let version = text |> parseToolkitPackageMetadata "Version" id "1"
                        let author = text |> parseToolkitPackageMetadata "Author" Some None
                        let toolkitVersion = text |> parseToolkitMetadata "Toolkit version" Some None |> prefixWithToolkit
                        version, author, toolkitVersion)

            let! appId =
                tryGetFileContents "appid.appid" psarc
                |> Async.map (Option.bind AppId.tryCustom)

            let project =
                { Version = version
                  DLCKey = StringValidator.dlcKey metaData.DLCKey
                  ArtistName =
                    { Value = metaData.ArtistName
                      SortValue = metaData.ArtistNameSort }
                  JapaneseArtistName = Option.ofString metaData.JapaneseArtistName
                  JapaneseTitle = Option.ofString metaData.JapaneseSongName
                  Title =
                    { Value = metaData.SongName
                      SortValue = metaData.SongNameSort }
                  AlbumName =
                    { Value = metaData.AlbumName
                      SortValue = metaData.AlbumNameSort }
                  Year = metaData.SongYear |> Option.ofNullable |> Option.defaultValue 0
                  AlbumArtFile = toTargetPath "cover.dds"
                  AudioFile = mainAudio
                  AudioPreviewFile = previewAudio
                  AudioPreviewStartTime = None
                  PitchShift = None
                  IgnoredIssues = Set.empty
                  Arrangements = arrangements |> List.map fst
                  Tones = tones
                  Author = author }

            let projectFile =
                sprintf "%s_%s" project.ArtistName.SortValue project.Title.SortValue
                |> StringValidator.fileName
                |> sprintf "%s.rs2dlc"
                |> toTargetPath

            do! DLCProject.save projectFile project
            return 0

        with ex ->
            printfn $"Problem processXBlock {xblock}"
            return 1
        }

let test filename outputDir =
    Directory.CreateDirectory(outputDir) |> ignore
    let platform = Platform.PC
    use psarc = PSARC.ReadFile(filename)
    let psarcContents = psarc.Manifest
    let xblocks = filterFilesWithExtension "xblock" psarcContents
    printList psarcContents
    let ret = Async.RunSynchronously (processXblock outputDir xblocks[3] psarcContents psarc platform)
    0

[<EntryPoint>]
let main argv =
    // Expecting two arguments: input file path and output directory
    if argv.Length < 2 then
        printfn "Usage: <input file path> <output directory>"
        1 // Exit with error
    else
        let inputFilePath = argv.[0]
        let outputDir = argv.[1]
        test inputFilePath outputDir
        0

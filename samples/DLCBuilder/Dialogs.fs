﻿module DLCBuilder.Dialogs

open System
open System.Collections.Generic
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Threading
open Elmish
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

let private window =
    lazy (Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow

let private createFilters name (extensions: string seq) =
    let filter = FileDialogFilter(Extensions = List(extensions), Name = name)
    List(seq { filter })

let private createFileFilters filter =
    let extensions =
        match filter with
        | FileFilter.Audio ->
            [ "wav"; "ogg"; "wem" ]
        | FileFilter.XML ->
            [ "xml" ]
        | FileFilter.Image ->
            [ "png"; "jpg"; "dds" ]
        | FileFilter.DDS ->
            [ "dds" ]
        | FileFilter.Profile ->
            [ ]
        | FileFilter.Project ->
            [ "rs2dlc" ]
        | FileFilter.PSARC ->
            [ "psarc"]
        | FileFilter.ToolkitTemplate ->
            [ "dlc.xml" ]
        | FileFilter.ToneImport ->
            [ "tone2014.xml"; "tone2014.json"; "psarc" ]
        | FileFilter.ToneExport ->
            [ "tone2014.xml"; "tone2014.json" ]
        | FileFilter.WwiseConsoleApplication ->
            [ if OperatingSystem.IsWindows() then "exe" else "sh" ]

    let name =
        match filter with
        | FileFilter.WwiseConsoleApplication ->
            let ext = if OperatingSystem.IsWindows() then "exe" else "sh"
            $"WwiseConsole.{ext}"
        | other ->
            sprintf "%AFiles" other
            |> translate

    createFilters name extensions

/// Shows an open folder dialog.
let private openFolderDialog title directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog = OpenFolderDialog(Title = title, Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    match result with
    | null | "" -> return Ignore
    | path -> return msg path }

/// Shows a save file dialog.
let private saveFileDialog title filter initialFileName directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog =
                SaveFileDialog(
                    Title = title,
                    Filters = createFileFilters filter,
                    InitialFileName = Option.toObj initialFileName,
                    Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    match result with
    | null | "" -> return Ignore
    | path -> return msg path }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = t, Filters = createFileFilters f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let private openFileDialog title filter directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filter directory false
            dialog.ShowAsync window.Value)
    match result with
    | [| file |] -> return msg file
    | _ -> return Ignore }

/// Shows an open file dialog that allows selecting multiple files.
let private openMultiFileDialog title filters directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filters directory true
            dialog.ShowAsync window.Value)
    match result with
    | null | [||] -> return Ignore
    | files -> return msg files }
   
let private translateTitle dialogType =
    let locString =
        match dialogType with
        | Dialog.PsarcImportTargetFolder _ -> "PsarcImportTargetFolderDialogTitle"
        | Dialog.AudioFile _ -> "AudioFileDialogTitle"
        | Dialog.ExportTone _ -> "ExportToneDialogTitle"
        | other -> $"{other}DialogTitle"

    translate locString

/// Shows the given dialog type.
let showDialog dialogType state =
    let title = translateTitle dialogType

    // No initial directory
    let ofd filter msg = openFileDialog title filter None msg

    let dialog = 
        match dialogType with
        | Dialog.OpenProject ->
            ofd FileFilter.Project OpenProject

        | Dialog.ToolkitImport ->
            if state.RunningTasks.Contains PsarcImport then
                async { return Ignore }
            else
                ofd FileFilter.ToolkitTemplate ImportToolkitTemplate

        | Dialog.PsarcImport ->
            if state.RunningTasks.Contains PsarcImport then
                async { return Ignore }
            else
                ofd FileFilter.PSARC (Dialog.PsarcImportTargetFolder >> ShowDialog)

        | Dialog.PsarcImportTargetFolder psarcPath ->
            openFolderDialog title None (fun folder -> ImportPsarc(psarcPath, folder))

        | Dialog.PsarcUnpack ->
            ofd FileFilter.PSARC (UnpackPSARC >> ToolsMsg)

        | Dialog.RemoveDD ->
            openMultiFileDialog title FileFilter.XML None (RemoveDD >> ToolsMsg)

        | Dialog.TestFolder ->
            openFolderDialog title None (SetTestFolderPath >> EditConfig)

        | Dialog.ProjectFolder ->
            openFolderDialog title None (SetProjectsFolderPath >> EditConfig)

        | Dialog.ProfileFile ->
            ofd FileFilter.Profile (SetProfilePath >> EditConfig)

        | Dialog.AddArrangements ->
            openMultiFileDialog title FileFilter.XML None AddArrangements

        | Dialog.ToneImport ->
            ofd FileFilter.ToneImport ImportTonesFromFile

        | Dialog.WwiseConsole ->
            ofd FileFilter.WwiseConsoleApplication (SetWwiseConsolePath >> EditConfig)
            
        | Dialog.CoverArt ->
            ofd FileFilter.Image SetCoverArt

        | Dialog.AudioFile isCustom ->
            let msg =
                match isCustom with
                | true -> Some >> SetCustomAudioPath >> EditInstrumental
                | false -> SetAudioFile
            ofd FileFilter.Audio msg

        | Dialog.CustomFont ->
            ofd FileFilter.DDS (Some >> SetCustomFont >> EditVocals)

        | Dialog.ExportTone tone ->
            let initialFileName = Some $"{tone.Name}.tone2014.xml"
            saveFileDialog title FileFilter.ToneExport initialFileName None (fun path -> ExportTone(tone, path))

        | Dialog.SaveProjectAs ->
            let initialFileName =
                state.OpenProjectFile
                |> Option.map Path.GetFileName
                |> Option.orElseWith (fun () ->
                    sprintf "%s_%s" state.Project.ArtistName.SortValue state.Project.Title.SortValue
                    |> StringValidator.fileName
                    |> sprintf "%s.rs2dlc"
                    |> Some)

            let initialDir =
                state.OpenProjectFile
                |> Option.map Path.GetDirectoryName
                |> Option.orElse (Option.ofString state.Config.ProjectsFolderPath)

            saveFileDialog title FileFilter.Project initialFileName initialDir SaveProject

    state, Cmd.OfAsync.result dialog

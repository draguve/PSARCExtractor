﻿module DLCBuilder.SelectImportTones

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open Avalonia.FuncUI.Types

let view state dispatch (tones: Tone array) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            ListBox.create [
                ListBox.name "tonesListBox"
                ListBox.dataItems tones
                // Multiple selection mode is broken in Avalonia 0.9
                // https://github.com/AvaloniaUI/Avalonia/issues/3497
                ListBox.selectionMode SelectionMode.Single
                ListBox.maxHeight 300.
                ListBox.onSelectedItemChanged (ImportTonesChanged >> dispatch)
            ]
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.spacing 8.
                StackPanel.children [
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content "Import"
                        Button.onClick (fun _ -> ImportSelectedTones |> dispatch)
                        Button.isDefault true
                    ]
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content "Cancel"
                        Button.onClick (fun _ -> CloseOverlay |> dispatch)
                    ]
                ]
            ]
        ]
    ] :> IView
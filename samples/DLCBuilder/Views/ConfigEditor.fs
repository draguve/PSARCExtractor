﻿module DLCBuilder.ConfigEditor

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common

let view state dispatch =
    Grid.create [
        Grid.columnDefinitions "*,*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                Grid.columnSpan 2
                TextBlock.fontSize 16.
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Configuration"
            ]
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Release Platforms"
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "PC"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.onChecked (fun _ -> (fun c -> { c with ReleasePlatforms = PC::c.ReleasePlatforms }) |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> (fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove PC }) |> EditConfig |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.onChecked (fun _ -> (fun c -> { c with ReleasePlatforms = Mac::c.ReleasePlatforms }) |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> (fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove Mac }) |> EditConfig |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Charter Name"
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 2
                TextBox.margin (0., 4.)
                TextBox.text state.Config.CharterName
                TextBox.onTextChanged (fun name -> (fun c -> { c with CharterName = name }) |> EditConfig |> dispatch)
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Profile Path"
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 3
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.ProfilePath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with ProfilePath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectProfilePath |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Test Folder"
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.TestFolderPath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with TestFolderPath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectTestFolderPath |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Projects Folder"
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.ProjectsFolderPath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with ProjectsFolderPath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectProjectsFolderPath |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.margin (0., 0., 4., 0.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Show Advanced Features"
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 6
                CheckBox.isChecked state.Config.ShowAdvanced
                CheckBox.onChecked (fun _ -> (fun c -> { c with ShowAdvanced = true }) |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun c -> { c with ShowAdvanced = false }) |> EditConfig |> dispatch)
            ]

            Button.create [
                Grid.columnSpan 2
                Grid.row 7
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content "Close"
                Button.onClick (fun _ -> SaveConfiguration |> dispatch)
            ]
        ]
    ] :> IView
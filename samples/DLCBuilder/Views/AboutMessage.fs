﻿module DLCBuilder.Views.AboutMessage

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Controls.Shapes
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media

let view dispatch =
    DockPanel.create [
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            vStack [
                TextBlock.create [
                    TextBlock.text "Rocksmith 2014 DLC Builder"
                    TextBlock.fontSize 20.
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin 4.
                ]

                Rectangle.create [
                    Rectangle.height 2.
                    Rectangle.fill Brushes.Gray
                ]

                hStack [
                    locText "programVersion" [
                        TextBlock.margin 4.
                    ]

                    TextBlock.create [
                        TextBlock.text (Utils.createVersionString())
                        TextBlock.margin 4.
                    ]
                ]

                locText "aboutDisclaimer" [
                    TextBlock.margin (4., 8.)
                ]

                locText "gitHubPage" [
                    TextBlock.classes [ "link" ]
                    TextBlock.cursor Cursors.hand
                    TextBlock.margin 4.
                    TextBlock.onTapped (fun ev ->
                        ev.Handled <- true
                        Utils.openLink "https://github.com/iminashi/Rocksmith2014.NET"
                    )
                ]
            ]
        ]
    ] |> generalize

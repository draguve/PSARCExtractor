// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Input.Platform
open Avalonia.Styling
open System
open System.Reactive.Linq

type FixedTextBox() =
    inherit TextBox()
    let mutable textChangedSub: IDisposable = null
    let mutable validationSub: IDisposable = null
    let mutable changeCallback: string -> unit = ignore
    let mutable validationCallback: string -> bool = fun _ -> true

    interface IStyleable with member _.StyleKey = typeof<TextBox>

    member val NoNotify = false with get, set

    member val ValidationErrorMessage = "" with get, set

    member val AutoFocus = false with get, set

    member this.ValidationCallback
        with get(): string -> bool = validationCallback
        and set(v) =
            if notNull validationSub then validationSub.Dispose()
            validationCallback <- v
            validationSub <-
                this.GetObservable(TextBox.TextProperty)
                    .Where(fun _ -> this.ValidationErrorMessage <> "")
                    .Subscribe(fun text ->
                        let errors =
                            if validationCallback text then
                                null
                            else
                                seq { box this.ValidationErrorMessage }

                        this.SetValue(DataValidationErrors.ErrorsProperty, errors)
                        |> ignore)

    member this.OnTextChangedCallback
        with get(): string -> unit = changeCallback
        and set(v) =
            if notNull textChangedSub then textChangedSub.Dispose()
            changeCallback <- v
            textChangedSub <-
                this.GetObservable(TextBox.TextProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    // Workaround for the inability to validate text that is pasted into the textbox
    // https://github.com/AvaloniaUI/Avalonia/issues/2611
    override this.OnKeyDown(e) =
        let keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>()
        let matchGesture (gestures: ResizeArray<KeyGesture>) = gestures.Exists(fun g -> g.Matches e)

        if matchGesture keymap.Paste then
            async {
                let! text =
                    (AvaloniaLocator.Current.GetService(typeof<IClipboard>) :?> IClipboard).GetTextAsync()
                    |> Async.AwaitTask
                if notNull text then
                    this.RaiseEvent(TextInputEventArgs(RoutedEvent = InputElement.TextInputEvent, Text = text, Source = this))
            } |> Async.StartImmediate
            e.Handled <- true
        else
            base.OnKeyDown(e)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull textChangedSub then textChangedSub.Dispose()
        if notNull validationSub then validationSub.Dispose()

        base.OnDetachedFromLogicalTree(e)

    override this.OnAttachedToVisualTree(e) =
        if this.AutoFocus then
            this.Focus()
            match this.Text with
            | null ->
                ()
            | text ->
                this.CaretIndex <- text.Length

        base.OnAttachedToVisualTree(e)

    static member onTextChanged<'t when 't :> FixedTextBox> fn =
        let getter: 't -> (string -> unit) = fun c -> c.OnTextChangedCallback
        let setter: 't * (string -> unit) -> unit = fun (c, f) -> c.OnTextChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<string -> unit>
            ("OnTextChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member validation<'t when 't :> FixedTextBox> fn =
        let getter: 't -> (string -> bool) = fun c -> c.ValidationCallback
        let setter: 't * (string -> bool) -> unit = fun (c, f) -> c.ValidationCallback <- f

        AttrBuilder<'t>.CreateProperty<string -> bool>
            ("Validation", fn, ValueSome getter, ValueSome setter, ValueNone)

    static member validationErrorMessage<'t when 't :> FixedTextBox> message =
        let getter: 't -> string = fun c -> c.ValidationErrorMessage
        let setter: 't * string -> unit = fun (c, v) -> c.ValidationErrorMessage <- v

        AttrBuilder<'t>.CreateProperty<string>
            ("ValidationErrorMessage", message, ValueSome getter, ValueSome setter, ValueNone)

    static member text<'t when 't :> FixedTextBox>(text: string) =
        let getter: 't -> string = fun c -> c.Text
        let setter: 't * string -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Text <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<string>
            ("Text", text, ValueSome getter, ValueSome setter, ValueNone)

    static member autoFocus<'t when 't :> FixedTextBox>(value: bool) =
        let getter: 't -> bool = fun c -> c.AutoFocus
        let setter: 't * bool -> unit = fun (c, v) -> c.AutoFocus <- v

        AttrBuilder<'t>.CreateProperty<bool>
            ("AutoFocus", value, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module FixedTextBox =
    let create (attrs: IAttr<FixedTextBox> list) : IView<FixedTextBox> =
        ViewBuilder.Create<FixedTextBox>(attrs)

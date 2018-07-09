﻿// ReSharper disable InconsistentNaming
#pragma warning disable 659

namespace Tea
{
    using System;
    using ImTools;
    using static ImToolsExt;

    public struct MessageRef<M>
    {
        public Ref<Ref<Action<M>>> Ref;
        public MessageRef(Ref<Ref<Action<M>>> r) => Ref = r;
    }

    public static class Message
    {
        public static void Empty<M>(M _) { }
        
        public static MessageRef<M> Ref<M>(Action<M> send) => 
            new MessageRef<M>(ImTools.Ref.Of(ImTools.Ref.Of(send)));
        
        public static MessageRef<M> EmptyRef<M>() => Ref<M>(Empty);

        public static void Set<M>(this MessageRef<M> x, Action<M> send) => x.Ref.Value.Swap(send);

        public static void Send<M>(this MessageRef<M> x, M m) => x.Ref.Value.Value(m);

        public static Action Updater<M>(this MessageRef<M> a, MessageRef<M> b) => () =>
        {
            var ar = a.Ref.Value;
            ar.Swap(b.Ref.Value.Value);
            b.Ref.Swap(ar);
        };
    }

    //public abstract class Style
    //{
    //    public class Of<TValue> : Style
    //    {
    //        public readonly TValue Value;
    //        public Of(TValue value) { Value = value; }
    //        public override bool Equals(object obj)
    //        {
    //            var other = obj as Of<TValue>;
    //            return other != null && Equals(other.Value, Value);
    //        }
    //    }

    //    public class Width : Of<int> { public Width(int value) : base(value) { } }
    //    public class Height : Of<int> { public Height(int value) : base(value) { } }

    //    public class IsEnabled : Of<bool>
    //    {
    //        public static readonly IsEnabled Enabled = new IsEnabled(true);
    //        public static readonly IsEnabled Disabled = new IsEnabled(false);
    //        private IsEnabled(bool value) : base(value) { }
    //    }
    //    public class Tooltip : Of<string> { public Tooltip(string value) : base(value) { } }
    //}

    //public static class Styles
    //{
    //    public static ImList<Style> style(params Style[] styles) => styles.AsList();

    //    public static Style width(int n) => new Style.Width(n);
    //    public static Style height(int n) => new Style.Height(n);
    //    public static Style isEnabled(bool enabled) => enabled ? Style.IsEnabled.Enabled : Style.IsEnabled.Disabled;
    //    public static Style tooltip(string label) => new Style.Tooltip(label);

    //    public static ImList<(Style, Style)> PrependDiffs(this ImList<Style> source, ImList<Style> other)
    //    {
    //        if (source.IsEmpty && other.IsEmpty)
    //            return ImList<(Style, Style)>.Empty;

    //        if (other.IsEmpty)
    //            return source.Map(s => (s, default(Style)));

    //        if (source.IsEmpty)
    //            return other.Map(s => (default(Style), s));

    //        var resultDiff = source.Fold(ImList<(Style, Style)>.Empty, (sourceStyle, sourceDiff) =>
    //        {
    //            var s2 = default(Style);
    //            other = other.Fold(ImList<Style>.Empty, (otherStyle, otherDiff) =>
    //            {
    //                if (otherStyle.GetType() != sourceStyle.GetType())
    //                    return otherDiff.Prepend(otherStyle);

    //                s2 = otherStyle;
    //                return otherDiff;
    //            });

    //            if (s2 == null)
    //                return sourceDiff.Prepend((sourceStyle, default(Style)));

    //            if (s2 != null && sourceStyle != s2 && !s2.Equals(sourceStyle))
    //                return sourceDiff.Prepend((sourceStyle, s2));

    //            return sourceDiff;
    //        });

    //        if (other.IsEmpty)
    //            return resultDiff;

    //        return other.Fold(resultDiff, (style, diff) => diff.Prepend((default(Style), style)));
    //    }
    //}

    public enum Layout { Horizontal, Vertical }

    /// UI elements
    public sealed class UI : Union<UI, Text, Input, Button, Check, Panel> { }
    public sealed class Text   : Rec<Text, string> { }
    public sealed class Input  : Rec<Input, (string Content, MessageRef<string> Changed)> { }
    public sealed class Button : Rec<Button, (string Label, MessageRef<Unit> Clicked)> { }
    public sealed class Check  : Rec<Check, (string Label, bool IsChecked, MessageRef<bool> Changed)> { }
    public sealed class Panel  : Rec<Panel, (Layout Layout, ImList<UI.I> Elements)> { }

    /// UI component update and event redirection.
    public sealed class Patch : Union<Patch, Insert, Update, Replace, Remove, Event> { }
    public sealed class Insert  : Rec<Insert, (ImList<int> Path, UI.I UI)> { }
    public sealed class Update  : Rec<Update, (ImList<int> Path, UI.I UI)> { }
    public sealed class Replace : Rec<Replace, (ImList<int> Path, UI.I UI)> { }
    public sealed class Remove  : Rec<Remove, ImList<int>> { }
    public sealed class Event   : Rec<Event, Action> { }

    /// UI with message M.
    public class UI<M>
    {
        public readonly UI.I Element;
        public Action<M> Send;
        public UI(UI.I element, Action<M> send) => (Element, Send) = (element, send);
    }

    /// Base interface for component with Update, View but without Commands, Subscriptions.
    public interface IComponent<T> where T : IComponent<T>
    {
        T Update(IMessage<T> message);
        UI<IMessage<T>> View();
    }

    /// Marker interface for boilerplate removal.
    // ReSharper disable once UnusedTypeParameter
    public interface IMessage<T> { }

    public struct ChildChanged<TChild, TParent> : IMessage<TParent>
    {
        public readonly int Index;
        public readonly IMessage<TChild> Message;
        public ChildChanged(int index, IMessage<TChild> message) => (Index, Message) = (index, message);
    }

    public static class Component
    {
        public static UI<IMessage<TParent>> In<TChild, TParent>(this TChild child, int childIndex)
            where TChild : IComponent<TChild>
            where TParent : IComponent<TParent> =>
            child.View().Map(m => m.Lift<TChild, TParent>(childIndex));

        public static UI<IMessage<TParent>> In<TChild, TParent>(this TChild child, TParent _onlyForInference, int childIndex = 0)
            where TChild : IComponent<TChild> =>
            child.View().Map(m => m.Lift<TChild, TParent>(childIndex));

        public static IMessage<TParent> Lift<TChild, TParent>(this IMessage<TChild> childMessage, int childIndex) =>
            new ChildChanged<TChild, TParent>(childIndex, childMessage);
    }

    public interface INativeUI
    {
        void ApplyPatches(ImList<Patch.I> patches);
    }

    public static class UIElements
    {
        public static UI<M> text<M>(string text) =>
            new UI<M>(UI.Of(Text.Of(text)), Message.Empty);

        public static UI<M> text<M>(object textObj) => text<M>("" + textObj);

        public static UI<M> input<M>(string text, Func<string, M> onChange)
        {
            var m = Message.EmptyRef<string>();
            return new UI<M>(UI.Of(Input.Of((text, m))), Message.Empty)
                .Do(x => m.Set(s => x.Send(onChange(s))));
        }

        public static UI<M> button<M>(string label, Func<M> onClick)
        {
            var m = Message.EmptyRef<Unit>();
            return new UI<M>(UI.Of(Button.Of((label, m))), Message.Empty)
                .Do(x => m.Set(_ => x.Send(onClick())));
        }

        public static UI<M> button<M>(string label, M onClickMessage) =>
            button(label, () => onClickMessage);

        public static UI<M> check<M>(string label, bool isChecked, Func<bool, M> onCheck)
        {
            var m = Message.EmptyRef<bool>();
            return new UI<M>(UI.Of(Check.Of((label, isChecked, m))), Message.Empty)
                .Do(x => m.Set(b => x.Send(onCheck(b))));
        }

        public static UI<M> panel<M>(Layout layout, ImList<UI<M>> elements)
        {
            var ui = new UI<M>(UI.Of(Panel.Of((layout, elements.Map(x => x.Element)))), Message.Empty);
            void Send(M m) => ui.Send(m);
            elements.Apply(x => x.Send = Send);
            return ui;
        }

        public static UI<M> row<M>(ImList<UI<M>> uis) => panel(Layout.Horizontal, uis);

        public static UI<M> row<M>(params UI<M>[] kids) => row(list(kids));

        public static UI<M> column<M>(ImList<UI<M>> kids) => panel(Layout.Vertical, kids);

        public static UI<M> column<M>(params UI<M>[] kids) => column(list(kids));
    }

    public static class UIApplication
    {
        /// Returns a new UI component mapping the message using the given function.
        public static UI<B> Map<A, B>(this UI<A> source, Func<A, B> map)
        {
            var target = new UI<B>(source.Element, Message.Empty);
            void Send(A a) => target.Send(map(a));
            source.Send = Send;
            return target;
        }

        /// Returns a list of UI updates from two UI components.
        /// To ensure correct insert and removal sequence where the insert/remove index are existing.
        public static ImList<Patch.I> Diff<M1, M2>(this UI<M1> a, UI<M2> b) =>
            Diff(ImList<Patch.I>.Empty, a.Element, b.Element, path: ImList<int>.Empty, pos: 0);

        private static ImList<Patch.I> Diff(this ImList<Patch.I> patches,
            UI.I a, UI.I b, ImList<int> path, int pos)
        {
            if (ReferenceEquals(a, b))
                return patches;

            switch (a)
            {
                case I<Text> textA when b is I<Text> textB:
                {
                    var (ta, tb) = (textA.V.V, textB.V.V);
                    return ta == tb ? patches : patches.Prepend(Patch.Of(Update.Of((path, b))));
                }
                case I<Button> buttonA when b is I<Button> buttonB:
                {
                    var ((labelA, clickedA), (labelB, clickedB)) = (buttonA.V.V, buttonB.V.V);
                    if (labelA != labelB)
                        patches = patches.Prepend(Patch.Of(Update.Of((path, b))));
                    return patches.Prepend(Patch.Of(Event.Of(clickedA.Updater(clickedB))));
                }
                case I<Input> inputA when b is I<Input> inputB:
                {
                    var ((textA, changedA), (textB, changedB)) = (inputA.V.V, inputB.V.V);
                    if (textA != textB)
                        patches = patches.Prepend(Patch.Of(Update.Of((path, b))));
                    return patches.Prepend(Patch.Of(Event.Of(changedA.Updater(changedB))));
                }
                case I<Check> checkA when b is I<Check> checkB:
                {
                    var ((labelA, isCheckedA, changedA), (labelB, isCheckedB, changedB)) = (checkA.V.V, checkB.V.V);
                    return Patch.Of(Event.Of(changedA.Updater(changedB)))
                        .Cons(isCheckedA == isCheckedB && labelA == labelB 
                            ? patches : Patch.Of(Update.Of((path, b))).Cons(patches));
                }
                case I<Panel> panelA when b is I<Panel> panelB:
                {
                    var ((layoutA, elemsA), (layoutB, elemsB)) = (panelA.V.V, panelB.V.V);
                    return layoutA == layoutB
                        ? patches.Diff(elemsA, elemsB, path, pos)
                        : patches.Prepend(Patch.Of(Replace.Of((path, b))));
                }
                default:
                    return patches.Prepend(Patch.Of(Replace.Of((path, b))));
            }
        }

        private static ImList<Patch.I> Diff(this ImList<Patch.I> patches, 
            ImList<UI.I> a, ImList<UI.I> b, ImList<int> path, int pos)
        {
            if (a.IsEmpty && b.IsEmpty)
                return patches;

            if (a.IsEmpty)
                return b.Fold(patches, (ui, i, tail) => Patch.Of(Insert.Of(((pos + i).Cons(path), ui))).Cons(tail));

            if (b.IsEmpty)
                return a.Fold(patches, (_, i, tail) => Patch.Of(Remove.Of((pos + i).Cons(path))).Cons(tail));

            return  patches
                .Diff(a.Head, b.Head, pos.Cons(path), 0)
                .Diff(a.Tail, b.Tail, path, pos + 1);
        }

        /// <summary>Runs Model-View-Update loop, e.g. Init->View->Update->View->Update->View... </summary>
        public static void Run<T>(INativeUI nativeUI, IComponent<T> application) where T : IComponent<T>
        {
            // Render and insert initial UI from the model
            var initialUI = application.View();
            initialUI.Send = m => UpdateViewLoop(application, initialUI, m);
            nativeUI.ApplyPatches(Patch.Of(Insert.Of((ImList<int>.Empty, initialUI.Element))).Cons<Patch.I>());

            void UpdateViewLoop(IComponent<T> app, UI<IMessage<T>> ui, IMessage<T> msg)
            {
                var newModel = app.Update(msg);
                var newUI = newModel.View();
                newUI.Send = m => UpdateViewLoop(newModel, newUI, m);
                var patches = ui.Diff(newUI);
                patches.Apply(x => (x as I<Event>)?.V.V());
                nativeUI.ApplyPatches(patches);
            }
        }
    }
}

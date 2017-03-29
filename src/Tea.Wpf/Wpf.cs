﻿using System;
using System.Windows;
using System.Windows.Controls;
using ImTools;

namespace Tea.Wpf
{
    public static class Wpf
    {
        public static INativeUI CreateUI(ContentControl root)
        {
            return new WpfUI(Update, root);
        }

        private class WpfUI : INativeUI
        {
            private readonly Func<UIUpdate, ContentControl, unit> _apply;
            private readonly ContentControl _root;

            public WpfUI(Func<UIUpdate, ContentControl, unit> apply, ContentControl root)
            {
                _apply = apply;
                _root = root;
            }

            public void Send(ImList<UIUpdate> uiUpdates)
            {
                uiUpdates.To(unit._, (update, _) => _apply(update, _root));
            }
        }

        private static UIElement CreateUI(UI ui)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));

            if (ui is UI.Text)
                return new Label { Content = ui.Value };

            var input = ui as UI.Input;
            if (input != null)
            {
                var elem = new TextBox { Text = input.Value };
                var evnt = input.Event.Value;
                elem.TextChanged += (sender, _) => evnt.Value(elem.Text);
                return elem;
            }

            var button = ui as UI.Button;
            if (button != null)
            {
                var elem = new Button { Content = button.Value };
                var evnt = button.Event.Value;
                elem.Click += (sender, _) => evnt.Value(unit._);
                return elem;
            }

            var div = ui as UI.Div;
            if (div != null)
            {
                var elems = div.Parts.Map(CreateUI);
                var orientation = div.Layout == Layout.Vertical ? Orientation.Vertical : Orientation.Horizontal;
                var panel = new StackPanel { Orientation = orientation };
                elems.To(0, (p, _) => panel.Children.Add(p));
                return panel;
            }

            throw new NotSupportedException("The type of UI is not supported: " + ui.GetType());
        }

        private static unit UpdateUI(UI ui, UIElement elem)
        {
            if (ui is UI.Text)
                ((Label)elem).Content = ui.Value;

            else if (ui is UI.Input)
                ((TextBox)elem).Text = ui.Value;

            else if (ui is UI.Button)
                ((Button)elem).Content = ui.Value;

            return unit._;
        }

        private static Panel LocatePanel(ImList<int> path, ContentControl root)
        {
            if (path.IsEmpty)
                return root.Content as Panel;
            var panel = LocatePanel(path.Tail, root);
            return (Panel)panel.Children[path.Head];
        }

        private static unit Update(UIUpdate update, ContentControl root)
        {
            var insertUI = update as UIUpdate.Insert;
            if (insertUI != null)
            {
                var path = insertUI.Path;
                if (path.IsEmpty)
                    root.Content = CreateUI(insertUI.UI);
                else
                    LocatePanel(path.Tail, root).Children.Insert(path.Head, CreateUI(insertUI.UI));
                return unit._;
            }

            var updateUI = update as UIUpdate.Update;
            if (updateUI != null)
            {
                var path = updateUI.Path;
                var elem = path.IsEmpty
                    ? (UIElement)root.Content
                    : LocatePanel(path.Tail, root).Children[path.Head];
                return UpdateUI(updateUI.UI, elem);
            }

            var replaceUI = update as UIUpdate.Replace;
            if (replaceUI != null)
            {
                var path = replaceUI.Path;
                if (path.IsEmpty)
                    root.Content = CreateUI(replaceUI.UI);
                else
                {
                    var children = LocatePanel(path.Tail, root).Children;
                    children.RemoveAt(path.Head);
                    children.Insert(path.Head, CreateUI(replaceUI.UI));
                }
                return unit._;
            }

            var removeUI = update as UIUpdate.Remove;
            if (removeUI != null)
            {
                var path = removeUI.Path;
                if (!path.IsEmpty)
                    LocatePanel(path.Tail, root).Children.RemoveAt(path.Head);
            }

            // Skip event
            //| EventUI _-> ()
            return unit._;
        }
    }
}

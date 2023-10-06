using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.UI.Library {
    internal class LibraryView : View {

        public Menu MacroEntries { get; private set; }

        public ViewContainer MacroConfig { get; private set; }

        public LibraryView() {
            
        }

        protected override void Build(Container buildPanel) {

            var panel = new FlowPanel {
                Parent = buildPanel,
                Width  = 300,
                Height = buildPanel.ContentRegion.Height - 50,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll = true,
                Title = "Chat Macros"
            };

            MacroConfig = new ViewContainer {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width - 300,
                Height = buildPanel.ContentRegion.Height,
                Left   = panel.Right
            };

            MacroEntries = new Menu {
                Parent = panel,
                Width = panel.ContentRegion.Width,
                Height = panel.ContentRegion.Height
            };

            var createNewBttn = new StandardButton {
                Parent = buildPanel,
                Width  = panel.Width,
                Top    = panel.Bottom,
                Height = 50,
                Text   = Resources.Create_Macro
            };

            var macros = ChatMacros.Instance.Data.GetActiveMacros();

            foreach (var macro in macros) {
                AddMacroEntry(MacroEntries, macro);
            }

            createNewBttn.Click += (_,_) => {
                var newMacro = new ChatMacro {
                    Title = Resources.New_Macro,
                    Lines = new List<ChatLine>(),
                    VoiceCommands = new List<string>()
                };

                if (!ChatMacros.Instance.Data.Upsert(newMacro)) {
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    return;
                }

                ChatMacros.Instance.Speech.UpdateGrammar();

                AddMacroEntry(MacroEntries, newMacro);
            };

            base.Build(buildPanel);
        }

        private void AddMacroEntry(Menu parent, ChatMacro macro) {
            var menuEntry = new MenuItem<ChatMacro>(macro) {
                Parent           = parent,
                Width            = parent.Width,
                Height           = 50,
                Text             = AssetUtil.Truncate(macro.Title, 260, GameService.Content.DefaultFont16),
                BasicTooltipText = macro.Title
            };

            menuEntry.Click += (_, _) => {
                MacroConfig.Show(new MacroView(macro));
            };

            void OnMacroTitleChanged(object _, ValueEventArgs<string> e) {
                menuEntry.Text = AssetUtil.Truncate(e.Value, 260, GameService.Content.DefaultFont16);
                menuEntry.BasicTooltipText = e.Value;
            }

            macro.TitleChanged += OnMacroTitleChanged;

            menuEntry.Disposed += (_, _) => {
                macro.TitleChanged -= OnMacroTitleChanged;
            };
        }

        private class MenuItem<T> : MenuItem {
            public readonly T Item;
            public MenuItem(T item) {
                Item = item;
            }
        }

        private class MacroView : View {

            private readonly ChatMacro _macro;

            public MacroView(ChatMacro macro) {
                _macro = macro;
            }

            protected override void Build(Container buildPanel) {

                var titleField = new TextBox {
                    Parent           = buildPanel,
                    PlaceholderText  = Resources.Enter_a_title___,
                    Text             = _macro.Title,
                    Width            = buildPanel.ContentRegion.Width - Panel.RIGHT_PADDING * 2,
                    Height           = 35,
                    Left             = Panel.RIGHT_PADDING,
                    BasicTooltipText = Resources.Enter_a_title___,
                    MaxLength        = 100
                };

                titleField.InputFocusChanged += (_, e) => {
                    if (e.Value) {
                        return;
                    }
                    _macro.Title = titleField.Text;
                    ChatMacros.Instance.Data.Upsert(_macro);
                };

                var linesPanel = new FlowPanel {
                    Parent              = buildPanel,
                    Width               = buildPanel.ContentRegion.Width,
                    Height              = buildPanel.ContentRegion.Height - 50 - titleField.Bottom - Panel.BOTTOM_PADDING,
                    Top                 = titleField.Bottom               + Panel.BOTTOM_PADDING,
                    FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                    ShowBorder          = true,
                    ControlPadding      = new Vector2(0,                  4),
                    OuterControlPadding = new Vector2(Panel.LEFT_PADDING, Panel.TOP_PADDING),
                    CanScroll           = true
                };

                foreach (var line in _macro.Lines) {
                    AddLine(linesPanel, line);
                }
                
                var addLineBttn = new StandardButton {
                    Parent = buildPanel,
                    Width  = buildPanel.ContentRegion.Width,
                    Height = 50,
                    Text   = Resources.Add_Line,
                    Top = linesPanel.Bottom
                };

                addLineBttn.Click += (_, _) => {
                    CreateLine(linesPanel);
                };
                base.Build(buildPanel);
            }

            private void AddLine(Container parent, ChatLine line) {
                var lineDisplay = new ViewContainer {
                    Parent = parent,
                    Width  = parent.ContentRegion.Width - 18,
                    Height = 50
                };

                var lineView = new LineView(line);
                lineDisplay.Show(lineView);

                lineView.DragStart += (_, _) => {
                    lineDisplay.Parent   = GameService.Graphics.SpriteScreen;
                    lineDisplay.Location = GameService.Graphics.SpriteScreen.RelativeMousePosition;
                };

                lineView.DragEnd += (_, _) => {
                    var dropIndex = lineDisplay.RelativeMousePosition.Y;
                    parent.Children.Insert(dropIndex, lineDisplay);

                    _macro.Lines.Remove(line);
                    _macro.Lines.Insert(dropIndex, line);
                    ChatMacros.Instance.Data.Upsert(_macro);
                };
            }

            private void CreateLine(Container parent) {
                var line = new ChatLine {
                    Channel = _macro.Lines.IsNullOrEmpty() ? ChatChannel.Current : _macro.Lines.Last().Channel
                };

                ChatMacros.Instance.Data.Upsert(line);

                _macro.Lines.Add(line);

                ChatMacros.Instance.Data.Upsert(_macro);

                AddLine(parent, line);
            }

            private class LineView : View {

                public event EventHandler<EventArgs> DragEnd;
                public event EventHandler<EventArgs> DragStart;
                public event EventHandler<EventArgs> RemoveLineClick;

                public readonly ChatLine Line;

                public LineView(ChatLine line) {
                    Line = line;
                }

                protected override void Build(Container buildPanel) {

                    var targetChannelDd = new KeyValueDropdown<ChatChannel> {
                        Parent           = buildPanel,
                        PlaceholderText  = Resources.Select_a_target_channel___,
                        SelectedItem     = Line.Channel,
                        BasicTooltipText = Resources.Select_a_target_channel___,
                        AutoSizeWidth    = true
                    };

                    foreach (var channel in Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>()) {
                        targetChannelDd.AddItem(channel, channel.ToDisplayName(), channel.GetHeadingColor());
                    }

                    var messageInput = new TextBox {
                        Parent = buildPanel,
                        PlaceholderText = Resources.Enter_a_message___,
                        Text = Line.Message,
                        Width = buildPanel.ContentRegion.Width - targetChannelDd.Width - Panel.RIGHT_PADDING * 2 - 32,
                        Left = targetChannelDd.Right + Panel.RIGHT_PADDING,
                        ForeColor = Line.Channel.GetMessageColor(),
                        BasicTooltipText = Resources.Enter_a_message___
                    };

                    targetChannelDd.Resized += (_, _) => {
                        messageInput.Width = buildPanel.ContentRegion.Width - targetChannelDd.Width - Panel.RIGHT_PADDING * 2 - 32;
                        messageInput.Left  = targetChannelDd.Right + Panel.RIGHT_PADDING;
                    };

                    var dragReorder = new Image(ChatMacros.Instance.Resources.DragReorderIcon) {
                        Parent = buildPanel,
                        Width = 32,
                        Height = 32,
                        Left = messageInput.Right + Panel.RIGHT_PADDING,
                    };

                    dragReorder.LeftMouseButtonPressed += (_, _) => {
                        DragStart?.Invoke(this, EventArgs.Empty);
                    };

                    
                    dragReorder.LeftMouseButtonReleased += (_, _) => {
                        DragEnd?.Invoke(this, EventArgs.Empty);
                    };

                    messageInput.TextChanged += (_, _) => {
                        Line.Message = messageInput.Text;
                        Save();
                    };

                    targetChannelDd.ValueChanged += (_, _) => {
                        Line.Channel = targetChannelDd.SelectedItem;
                        messageInput.ForeColor = Line.Channel.GetMessageColor();
                        Save();
                    };
                    base.Build(buildPanel);
                }

                private void Save() {
                    if (!ChatMacros.Instance.Data.Upsert(Line)) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    }
                }
            }
        }
    }
}

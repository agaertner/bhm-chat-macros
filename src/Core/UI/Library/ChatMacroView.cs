using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.UI.Library {
    internal class LibraryView : View {

        private const int  MAX_MENU_ENTRY_TITLE_WIDTH = 195;

        public Menu          MacroEntries { get; private set; }
        public ViewContainer MacroConfig  { get; private set; }

        private AsyncTexture2D _cogWheelIcon;
        private AsyncTexture2D _cogWheelIconHover;
        private AsyncTexture2D _cogWheelIconClick;

        private LibraryConfig _config;
        public LibraryView(LibraryConfig config) {
            _config            = config;
            _cogWheelIcon      = GameService.Content.DatAssetCache.GetTextureFromAssetId(155052);
            _cogWheelIconHover = GameService.Content.DatAssetCache.GetTextureFromAssetId(157110);
            _cogWheelIconClick = GameService.Content.DatAssetCache.GetTextureFromAssetId(157109);
        }

        protected override void Build(Container buildPanel) {

            var panel = new FlowPanel {
                Parent              = buildPanel,
                Width               = 300,
                Height              = buildPanel.ContentRegion.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                Title               = "Chat Macros",
                OuterControlPadding = new Vector2(2, 5),
                ControlPadding      = new Vector2(2, 5),
                ShowBorder          = true
            };

            var filterWrap = new FlowPanel {
                Parent              = panel,
                Width               = panel.ContentRegion.Width,
                Height              = 35,
                FlowDirection       = ControlFlowDirection.SingleLeftToRight,
                OuterControlPadding = new Vector2(0, 0),
                ControlPadding      = new Vector2(Panel.RIGHT_PADDING, 0)
            };

            var searchBar = new TextBox {
                Parent          = filterWrap,
                Width           = filterWrap.ContentRegion.Width - 40,
                Height          = filterWrap.ContentRegion.Height,
                PlaceholderText = Resources.Search___
            };

            var filterCog = new Image {
                Parent  = filterWrap,
                Width   = searchBar.Height,
                Height  = searchBar.Height,
                Texture = _cogWheelIcon
            };

            filterCog.MouseEntered += (_, _) => {
                filterCog.Texture = _cogWheelIconHover;
            };

            filterCog.MouseLeft += (_, _) => {
                filterCog.Texture = _cogWheelIcon;
            };

            filterCog.LeftMouseButtonPressed += (_, _) => {
                filterCog.Texture = _cogWheelIconClick;
            };

            filterCog.LeftMouseButtonReleased += (_, _) => {
                filterCog.Texture = _cogWheelIconHover;
            };

            var menu = new ContextMenuStrip {
                Parent      = buildPanel,
                ClipsBounds = false
            };

            var showActivesOnly = new ContextMenuStripItem(Resources.Show_Actives_Only) {
                Parent   = menu,
                CanCheck = true,
                Checked  = _config.ShowActivesOnly
            };

            filterCog.Click += (_, _) => {
                GameService.Content.PlaySoundEffectByName("button-click");
                menu.Show(GameService.Input.Mouse.Position);
            };

            var macrosMenuWrap = new FlowPanel {
                Parent        = panel,
                Width         = panel.ContentRegion.Width,
                Height        = panel.ContentRegion.Height - 85 - (int)panel.ControlPadding.Y * 2 - (int)panel.OuterControlPadding.Y,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll     = true
            };

            MacroConfig = new ViewContainer {
                Parent     = buildPanel,
                Width      = buildPanel.ContentRegion.Width - 300,
                Height     = buildPanel.ContentRegion.Height,
                Left       = panel.Right,
                ShowBorder = true
            };

            MacroEntries = new Menu {
                Parent    = macrosMenuWrap,
                Width     = macrosMenuWrap.ContentRegion.Width,
                Height    = macrosMenuWrap.ContentRegion.Height,
                CanSelect = true
            };

            var createNewBttn = new StandardButtonCustomFont(ChatMacros.Instance.Resources.RubikRegular26) {
                Parent = panel,
                Width  = panel.ContentRegion.Width - 12,
                Height = 50,
                Text   = Resources.Create_Macro
            };

            var macros = ChatMacros.Instance.Data.GetAllMacros();

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
                    GameService.Content.PlaySoundEffectByName("error");
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    return;
                }

                GameService.Content.PlaySoundEffectByName("button-click");

                ChatMacros.Instance.Speech.UpdateGrammar();

                AddMacroEntry(MacroEntries, newMacro);
            };

            searchBar.TextChanged += (_, _) => {
                UpdateFilter(searchBar.Text, showActivesOnly.Checked);
            };

            showActivesOnly.CheckedChanged += (_, e) => {
                _config.ShowActivesOnly = e.Checked;
                UpdateFilter(searchBar.Text, showActivesOnly.Checked);
                GameService.Content.PlaySoundEffectByName("color-change");
            };

            UpdateFilter(searchBar.Text, showActivesOnly.Checked);

            base.Build(buildPanel);
        }

        private void UpdateFilter(string searchKey, bool showActives) {
            var entries = MacroEntries.Children.Cast<MenuItem<ChatMacro>>().ToList();

            var filtered = FastenshteinUtil.FindMatchesBy(searchKey, entries, entry => entry.Item.Title).ToList();

            foreach (var entry in entries) {
                entry.Visible = filtered.IsNullOrEmpty() || filtered.Contains(entry);
                if (showActives) {
                    entry.Visible = ChatMacros.Instance.Macro.ActiveMacros.Any(macro => macro.Id.Equals(entry.Item.Id));
                }
            }

            MacroEntries.Invalidate();
        }

        private void AddMacroEntry(Menu parent, ChatMacro macro) {
            var menuEntry = new MenuItem<ChatMacro>(macro, m => m.Title) {
                Parent           = parent,
                Width            = parent.ContentRegion.Width,
                Height           = 50,
                Text             = string.IsNullOrEmpty(macro.Title) ? 
                                       Resources.Enter_a_title___ : 
                                       AssetUtil.Truncate(macro.Title, MAX_MENU_ENTRY_TITLE_WIDTH, GameService.Content.DefaultFont16),
                BasicTooltipText = macro.Title
            };

            menuEntry.DeleteClick += (_, _) => {
                if (ChatMacros.Instance.Data.Delete(macro)) {
                    menuEntry.Dispose();
                    ChatMacros.Instance.Speech.UpdateGrammar();
                }
            };

            void CreateMacroView() {
                var m = ChatMacros.Instance.Data.GetChatMacro(macro.Id);
                if (m == null) {
                    return;
                }
                menuEntry.AssignItem(m, x => x.Title);

                var view = new MacroView(m);
                view.TitleChanged += (_, e) => {
                    menuEntry.Text = string.IsNullOrEmpty(m.Title) ?
                                         Resources.Enter_a_title___ :
                                         AssetUtil.Truncate(e.Value, MAX_MENU_ENTRY_TITLE_WIDTH, GameService.Content.DefaultFont16);
                    menuEntry.BasicTooltipText = e.Value;
                };

                MacroConfig.Show(view);
            }

            menuEntry.Click += (_, _) => {
                ChatMacros.Instance.Resources.PlayMenuItemClick();
                CreateMacroView();
            };
        }

        private class MenuItem<T> : MenuItem {

            public event EventHandler<EventArgs> DeleteClick; 

            public T Item;

            private Func<T, string> _basicTooltipText;
            private bool            _mouseOverDelete;
            private Rectangle       _deleteBounds;

            private AsyncTexture2D _currentDeleteTexture;
            private AsyncTexture2D _deleteHoverTexture;
            private AsyncTexture2D _deleteTexture;
            private AsyncTexture2D _deletePressedTexture;

            public MenuItem(T item, Func<T, string> basicTooltipText) {
                Item                  = item;
                _basicTooltipText     = basicTooltipText;
                _deleteTexture        = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175782);
                _deleteHoverTexture   = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175784);
                _deletePressedTexture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175783);
                _currentDeleteTexture = _deleteTexture;
            }

            public void AssignItem(T item, Func<T, string> basicTooltipText) {
                Item              = item;
                _basicTooltipText = basicTooltipText;
            }

            protected override void OnMouseMoved(MouseEventArgs e) {
                if (_deleteBounds.Contains(RelativeMousePosition)) {
                    _currentDeleteTexture = _deleteHoverTexture;
                    _mouseOverDelete      = true;
                    BasicTooltipText      = Resources.Delete;
                } else {
                    _currentDeleteTexture = _deleteTexture;
                    _mouseOverDelete      = false;
                    BasicTooltipText      = _basicTooltipText(Item);
                }
                base.OnMouseMoved(e);
            }

            protected override void OnClick(MouseEventArgs e) {
                if (!_mouseOverDelete) {
                    base.OnClick(e);
                }
            }

            protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
                if (_mouseOverDelete) {
                    _currentDeleteTexture = _deletePressedTexture;
                    GameService.Content.PlaySoundEffectByName("button-click");
                }
                base.OnLeftMouseButtonPressed(e);
            }

            protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
                if (_mouseOverDelete) {
                    _currentDeleteTexture = _deleteHoverTexture;
                    DeleteClick?.Invoke(this, EventArgs.Empty);
                }
                base.OnLeftMouseButtonReleased(e);
            }

            public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds) {
                var height = _menuItemHeight - Panel.TOP_PADDING - Panel.TOP_PADDING / 2;
                _deleteBounds = new Rectangle(this.Width - height - Panel.RIGHT_PADDING - 15, (_menuItemHeight - height) / 2, height, height);
                spriteBatch.DrawOnCtrl(this, _currentDeleteTexture, _deleteBounds);

                if (this.Parent is Menu menu && menu.SelectedMenuItem == this) {
                    height += 4;
                    spriteBatch.DrawOnCtrl(this, ChatMacros.Instance.Resources.EditIcon, new Rectangle(_deleteBounds.Left - height - Panel.RIGHT_PADDING, _deleteBounds.Y, height, height));
                }
            }
        }

        private class MacroView : View {

            public event EventHandler<ValueEventArgs<string>> TitleChanged;

            private readonly ChatMacro _macro;

            public MacroView(ChatMacro macro) {
                _macro = macro;
            }

            protected override void Build(Container buildPanel) {

                var titleField = new TextBox {
                    Parent           = buildPanel,
                    PlaceholderText  = Resources.Enter_a_title___,
                    Text             = _macro.Title,
                    Width            = buildPanel.ContentRegion.Width,
                    Height           = 35,
                    BasicTooltipText = Resources.Enter_a_title___,
                    MaxLength        = 100,
                    Font             = GameService.Content.DefaultFont18,
                    ForeColor        = ChatMacros.Instance.Resources.BrightGold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                titleField.InputFocusChanged += (_, e) => {
                    if (e.Value) {
                        return;
                    }
                    var oldTitle = new string(_macro.Title.ToCharArray());
                    _macro.Title = titleField.Text.Trim();

                    if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        titleField.Text = oldTitle;
                        _macro.Title    = oldTitle;
                        return;
                    }

                    TitleChanged?.Invoke(this, new ValueEventArgs<string>(_macro.Title));
                };

                var macroConfig = new ViewContainer {
                    Parent = buildPanel,
                    Width  = buildPanel.ContentRegion.Width,
                    Height = 200,
                    Top    = titleField.Bottom + Panel.TOP_PADDING
                };
                macroConfig.Show(new BaseMacroSettings(_macro, () => ChatMacros.Instance.Data.Upsert(_macro)));

                var linesBttnWrap = new FlowPanel {
                    Parent        = buildPanel,
                    Width         = buildPanel.ContentRegion.Width,
                    Height        = buildPanel.ContentRegion.Height - titleField.Bottom - macroConfig.Height - Panel.TOP_PADDING,
                    FlowDirection = ControlFlowDirection.SingleTopToBottom,
                    Top           = macroConfig.Bottom,
                    Title         = Resources.Message_Sequence
                };

                #region linesBttnWrap children
                var linesPanel = new FlowPanel {
                    Parent              = linesBttnWrap,
                    Width               = linesBttnWrap.ContentRegion.Width,
                    Height              = linesBttnWrap.ContentRegion.Height - 50,
                    FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                    ShowBorder          = true,
                    ControlPadding      = new Vector2(0,                  4),
                    OuterControlPadding = new Vector2(Panel.LEFT_PADDING, Panel.TOP_PADDING),
                    CanScroll           = true
                };

                foreach (var line in _macro.Lines) {
                    AddLine(linesPanel, line);
                }
                
                var addLineBttn = new StandardButtonCustomFont(ChatMacros.Instance.Resources.RubikRegular26) {
                    Parent = linesBttnWrap,
                    Width  = linesBttnWrap.ContentRegion.Width,
                    Height = 50,
                    Text   = Resources.Add_Line
                };

                addLineBttn.Click += (_, _) => {
                    CreateLine(linesPanel);
                };
                #endregion linesBttnWrap children

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

                lineView.RemoveClick += (_, _) => {
                    var oldLines = _macro.Lines.ToList();

                    if (_macro.Lines.RemoveAll(l => l.Id.Equals(line.Id)) < 1 || !ChatMacros.Instance.Data.Upsert(_macro)) {
                        _macro.Lines = oldLines;
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        return;
                    }
                    ;
                    if (ChatMacros.Instance.Data.Delete(line)) {
                        lineDisplay.Dispose();
                        ChatMacros.Instance.Speech.UpdateGrammar();
                    }
                };

                int lineIndex = _macro.Lines.IndexOf(line);
                void OnMouseMoved(object o, MouseEventArgs mouseEventArgs) {
                    if (lineView.IsDragging) {
                        var tempLines = _macro.Lines.ToList();
                        var dropIndex = MathHelper.Clamp(parent.RelativeMousePosition.Y / lineDisplay.Height, 0, tempLines.Count - 1);

                        if (lineIndex != dropIndex) {
                            ChatMacros.Instance.Resources.PlayMenuItemClick();
                            lineIndex = dropIndex;
                        }

                        tempLines.Remove(line);
                        tempLines.Insert(dropIndex, line);
                        parent.ClearChildren();
                        foreach (var reLine in tempLines) {
                            AddLine(parent, reLine);
                        }
                    }
                }

                GameService.Input.Mouse.MouseMoved += OnMouseMoved;

                lineDisplay.Disposed += (_, _) => {
                    GameService.Input.Mouse.MouseMoved -= OnMouseMoved;
                };

                lineView.DragEnd += (_, _) => {
                    var dropIndex = MathHelper.Clamp(parent.RelativeMousePosition.Y / lineDisplay.Height, 0, _macro.Lines.Count - 1);

                    var oldOrder = _macro.Lines.ToList();

                    if (_macro.Lines.RemoveAll(l => l.Id.Equals(line.Id)) < 1) {
                        _macro.Lines = oldOrder;
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        return;
                    }

                    _macro.Lines.Insert(dropIndex, line);

                    if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                        _macro.Lines = oldOrder;
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        return;
                    }

                    ChatMacros.Instance.Macro.UpdateMacros();

                    GameService.Content.PlaySoundEffectByName("color-change");

                    parent.ClearChildren();
                    foreach (var reLine in _macro.Lines) {
                        AddLine(parent, reLine);
                    }
                };
            }

            private void CreateLine(Container parent) {
                var line = new ChatLine {
                    Channel = _macro.Lines.IsNullOrEmpty() ? ChatChannel.Current : _macro.Lines.Last().Channel
                };

                if (!ChatMacros.Instance.Data.Upsert(line)) {
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    GameService.Content.PlaySoundEffectByName("error");
                    return;
                }

                GameService.Content.PlaySoundEffectByName("button-click");

                _macro.Lines.Add(line);

                if (ChatMacros.Instance.Data.Upsert(_macro)) {
                    ChatMacros.Instance.Speech.UpdateGrammar();
                }

                AddLine(parent, line);
            }
            private class LineView : View {

                public event EventHandler<EventArgs> RemoveClick; 
                public event EventHandler<EventArgs> DragEnd;

                private readonly ChatLine _line;

                public bool IsDragging { get; private set; }

                public LineView(ChatLine line) {
                    _line = line;
                }

                protected override void Build(Container buildPanel) {

                    var targetChannelDd = new KeyValueDropdown<ChatChannel> {
                        Parent           = buildPanel,
                        PlaceholderText  = Resources.Select_a_target_channel___,
                        SelectedItem     = _line.Channel,
                        BasicTooltipText = Resources.Select_a_target_channel___,
                        AutoSizeWidth    = true
                    };

                    foreach (var channel in Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>()) {
                        if (channel != ChatChannel.Whisper) {
                            targetChannelDd.AddItem(channel, channel.ToDisplayName(), channel.GetHeadingColor());
                        }
                    }

                    var messageInput = new TextBox {
                        Parent = buildPanel,
                        PlaceholderText = Resources.Enter_a_message___,
                        Text = _line.Message,
                        Width = buildPanel.ContentRegion.Width - targetChannelDd.Width - Panel.RIGHT_PADDING * 4 - 50,
                        Left = targetChannelDd.Right + Panel.RIGHT_PADDING,
                        ForeColor = _line.Channel.GetMessageColor(),
                        BasicTooltipText = string.IsNullOrWhiteSpace(_line.Message) ? Resources.Enter_a_message___ : _line.Message,
                        Font = ChatMacros.Instance.Resources.Menomonia24
                    };

                    var remove = new Image {
                        Parent  = buildPanel,
                        Width   = 25,
                        Height  = 25,
                        Left    = messageInput.Right + Panel.RIGHT_PADDING,
                        Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175782),
                        BasicTooltipText = Resources.Remove
                    };

                    remove.MouseEntered += (_, _) => {
                        remove.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175784);
                    };

                    remove.MouseLeft += (_, _) => {
                        remove.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175782);
                    };

                    remove.LeftMouseButtonPressed += (_, _) => {
                        remove.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175783);
                    };

                    remove.LeftMouseButtonReleased += (_, _) => {
                        GameService.Content.PlaySoundEffectByName("button-click");
                        RemoveClick?.Invoke(this, EventArgs.Empty);
                    };

                    var dragReorder = new Image(ChatMacros.Instance.Resources.DragReorderIcon) {
                        Parent = buildPanel,
                        Width = 25,
                        Height = 25,
                        Left = remove.Right + Panel.RIGHT_PADDING,
                    };

                    dragReorder.LeftMouseButtonPressed += (_, _) => {
                        IsDragging = true;
                        GameService.Content.PlaySoundEffectByName("button-click");
                    };

                    void OnLeftMouseButtonReleased(object o, MouseEventArgs mouseEventArgs) {
                        if (IsDragging) {
                            IsDragging = false;
                            this.DragEnd?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    GameService.Input.Mouse.LeftMouseButtonReleased += OnLeftMouseButtonReleased;

                    dragReorder.Disposed += (_, _) => {
                        GameService.Input.Mouse.LeftMouseButtonReleased -= OnLeftMouseButtonReleased;
                    };

                    messageInput.TextChanged += (_, _) => {
                        _line.Message                  = messageInput.Text.TrimEnd();
                        messageInput.BasicTooltipText = string.IsNullOrWhiteSpace(_line.Message) ? Resources.Enter_a_message___ : _line.Message;
                        Save();
                    };

                    targetChannelDd.Resized += (_, _) => {
                        messageInput.Width = buildPanel.ContentRegion.Width - targetChannelDd.Width - Panel.RIGHT_PADDING * 4 - 50;
                        messageInput.Left  = targetChannelDd.Right          + Panel.RIGHT_PADDING;
                    };

                    targetChannelDd.ValueChanged += (_, _) => {
                        _line.Channel = targetChannelDd.SelectedItem;
                        messageInput.ForeColor = _line.Channel.GetMessageColor();
                        Save();
                    };
                    base.Build(buildPanel);
                }

                private void Save() {
                    if (!ChatMacros.Instance.Data.Upsert(_line)) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        return;
                    }

                    ChatMacros.Instance.Macro.UpdateMacros();
                }
            }
        }
    }
}

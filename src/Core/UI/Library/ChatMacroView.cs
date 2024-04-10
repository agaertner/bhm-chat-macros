using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Resources;
using Blish_HUD.Extended;
using LiteDB;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.TextureAtlases;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;
using ContextMenuStrip = Blish_HUD.Controls.ContextMenuStrip;
using Control = Blish_HUD.Controls.Control;
using File = System.IO.File;
using HorizontalAlignment = Blish_HUD.Controls.HorizontalAlignment;
using Menu = Blish_HUD.Controls.Menu;
using MenuItem = Blish_HUD.Controls.MenuItem;
using MouseEventArgs = Blish_HUD.Input.MouseEventArgs;
using Panel = Blish_HUD.Controls.Panel;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using TextBox = Blish_HUD.Controls.TextBox;
using View = Blish_HUD.Graphics.UI.View;

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
                Height              = buildPanel.ContentRegion.Height - 50,
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
                Height        = panel.ContentRegion.Height - 35 - (int)panel.ControlPadding.Y * 2 - (int)panel.OuterControlPadding.Y,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                CanScroll     = true
            };

            MacroConfig = new ViewContainer {
                Parent     = buildPanel,
                Width      = buildPanel.ContentRegion.Width - 300,
                Height     = buildPanel.ContentRegion.Height,
                Left       = panel.Right
            };

            MacroEntries = new Menu {
                Parent    = macrosMenuWrap,
                Width     = macrosMenuWrap.ContentRegion.Width,
                Height    = macrosMenuWrap.ContentRegion.Height,
                CanSelect = true
            };

            var createNewBttn = new StandardButtonCustomFont(ChatMacros.Instance.Resources.RubikRegular26) {
                Parent = buildPanel,
                Width  = panel.Width - 20,
                Height = 50,
                Top = panel.Bottom,
                Left = 10,
                Text = Resources.Create_Macro
            };

            var macros = ChatMacros.Instance.Data.GetAllMacros();

            foreach (var macro in macros) {
                AddMacroEntry(MacroEntries, macro);
            }

            createNewBttn.Click += (_,_) => {
                var newMacro = new ChatMacro {
                    Id = new ObjectId(),
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

            void OnMacroOnActiveMacrosChange(object o, ValueEventArgs<IReadOnlyList<BaseMacro>> valueEventArgs) {
                UpdateFilter(searchBar.Text, showActivesOnly.Checked);
            }

            ChatMacros.Instance.Macro.ActiveMacrosChange += OnMacroOnActiveMacrosChange;

            MacroEntries.Disposed += (_, _) => {
                ChatMacros.Instance.Macro.ActiveMacrosChange -= OnMacroOnActiveMacrosChange;
            };

            base.Build(buildPanel);

            UpdateFilter(searchBar.Text, showActivesOnly.Checked);
        }

        private void UpdateFilter(string searchKey, bool showActives) {
            //TODO: Sorting works but Menu doesn't update properly even if children are overwritten or Parent is reset.
            var entries = SortMacroMenuEntries(MacroEntries.Children.Cast<MenuItem<ChatMacro>>()).ToList();

            var filtered = FastenshteinUtil.FindMatchesBy(searchKey, entries, entry => entry.Item.Title).ToList();

            foreach (var entry in entries) {
                entry.IsActive = ChatMacros.Instance.Macro
                                           .ActiveMacros.Any(macro => macro.Id.Equals(entry.Item.Id));

                var match = string.IsNullOrEmpty(searchKey) || filtered.IsNullOrEmpty() || filtered.Any(x => x.Item.Id.Equals(entry.Item.Id));

                if (showActives) {
                    entry.Visible = entry.IsActive && match;
                } else {
                    entry.Visible = match;
                }

                entry.Enabled = entry.Visible;
            }
            MacroEntries.Invalidate();
        }

        private IEnumerable<MenuItem<ChatMacro>> SortMacroMenuEntries(IEnumerable<MenuItem<ChatMacro>> toSort) {
            return toSort.OrderBy(x => ChatMacros.Instance.LibraryConfig.Value.IndexChannelHistory(x.Item.Lines.FirstOrDefault()?.Channel ?? ChatChannel.Current))
                         .ThenBy(x => x.Item.Lines?.FirstOrDefault()?.Channel ?? ChatChannel.Current)
                         .ThenBy(x => x.Item.Title.ToLowerInvariant());
        }

        private void AddMacroEntry(Menu parent, ChatMacro macro) {
            var menuEntry = new MenuItem<ChatMacro>(macro, m => m.Title, m => m.GetDisplayColor()) {
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
                    MacroConfig.Clear();
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
                CreateMacroView();
            };
        }

        private class MenuItem<T> : MenuItem {

            public event EventHandler<EventArgs> DeleteClick; 

            public T Item;

            private bool _isActive;
            public bool IsActive {
                get => _isActive;
                set => SetProperty(ref _isActive, value, true);
            }

            private Func<T, string> _basicTooltipText;
            private Func<T, Color>  _color;
            private bool            _mouseOverDelete;
            private Rectangle       _deleteBounds;

            private bool      _mouseOverActive;
            private Rectangle _activeBounds;

            private AsyncTexture2D _currentDeleteTexture;
            private AsyncTexture2D _deleteHoverTexture;
            private AsyncTexture2D _deleteTexture;
            private AsyncTexture2D _deletePressedTexture;

            private readonly AsyncTexture2D _textureArrow = AsyncTexture2D.FromAssetId(156057);

            private int LeftSidePadding {
                get {
                    int leftSidePadding = 10;
                    if (!this._children.IsEmpty)
                        leftSidePadding += 16;
                    return leftSidePadding;
                }
            }

            private bool _mouseOverIconBox;

            private Rectangle FirstItemBoxRegion => new Rectangle(0, this.MenuItemHeight / 2 - 16, 32, 32);

            public MenuItem(T item, Func<T, string> basicTooltipText, Func<T, Color> color) {
                Item                  = item;
                _basicTooltipText     = basicTooltipText;
                _color                = color;
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
                } else if (_activeBounds.Contains(RelativeMousePosition)) {
                    BasicTooltipText = Resources.This_macro_is_currently_active_and_can_be_triggered_;
                    _mouseOverActive = true;
                } else {
                    _currentDeleteTexture = _deleteTexture;
                    _mouseOverDelete      = false;
                    _mouseOverActive      = false;
                    BasicTooltipText      = _basicTooltipText(Item);
                }

                this._mouseOverIconBox = _canCheck && _overSection && this.FirstItemBoxRegion.OffsetBy(this.LeftSidePadding, 0).Contains(this.RelativeMousePosition);
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

            private void DrawDropdownArrow(SpriteBatch spriteBatch) {
                Vector2   origin               = new Vector2(8f, 8f);
                Rectangle destinationRectangle = new Rectangle(13, this.MenuItemHeight / 2, 16, 16);
                spriteBatch.DrawOnCtrl((Control)this, (Texture2D)_textureArrow, destinationRectangle, new Rectangle?(), Color.White, this.ArrowRotation, origin);
            }

            public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) 
            {
                int leftSidePadding = this.LeftSidePadding;
                if (!_children.IsEmpty) {
                    this.DrawDropdownArrow(spriteBatch);
                }
                TextureRegion2D texture = null;
                if (this.CanCheck) {
                    string state     = this.Checked ? "-checked" : "-unchecked";
                    string extension = "";
                    extension = _mouseOverIconBox ? "-active" : extension;
                    extension = !this.Enabled ? "-disabled" : extension;
                    texture   = Checkable.TextureRegionsCheckbox.First<TextureRegion2D>(cb => cb.Name == "checkbox/cb" + state + extension);
                } else if (this.Icon != null && _children.IsEmpty)
                    texture = new TextureRegion2D(this.Icon);
                if (texture != null)
                    spriteBatch.DrawOnCtrl(this, texture, this.FirstItemBoxRegion.OffsetBy(leftSidePadding, 0));
                if (_canCheck)
                    leftSidePadding += 42;
                else if (!_children.IsEmpty)
                    leftSidePadding += 10;
                else if (_icon != null)
                    leftSidePadding += 42;
                spriteBatch.DrawStringOnCtrl((Control)this, _text, Control.Content.DefaultFont16, new Rectangle(leftSidePadding, 0, this.Width - (leftSidePadding - 10), this.MenuItemHeight), _color(Item), true, true);
            }

            public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds) {
                var height = _menuItemHeight - Panel.TOP_PADDING - Panel.TOP_PADDING / 2;
                _deleteBounds = new Rectangle(this.Width - height - Panel.RIGHT_PADDING - 15, (_menuItemHeight - height) / 2, height, height);
                spriteBatch.DrawOnCtrl(this, _currentDeleteTexture, _deleteBounds);

                if (_isActive) {
                    height        += 4;
                    _activeBounds =  new Rectangle(_deleteBounds.Left - height - Panel.RIGHT_PADDING, _deleteBounds.Y, height, height);
                    spriteBatch.DrawOnCtrl(this, ChatMacros.Instance.Resources.EditIcon, _activeBounds);
                }
            }
        }

        private class MacroView : View {

            public event EventHandler<ValueEventArgs<string>> TitleChanged;

            private readonly ChatMacro _macro;

            private ViewContainer _editField;
            private Image         _linkFileState;

            public MacroView(ChatMacro macro) {
                _macro = macro;
            }


            private void UpdateLinkFileState(bool showError = false) {
                var linkFileExists = FileUtil.Exists(_macro.LinkFile, out string _, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray());
                if (!linkFileExists) {
                    _macro.LinkFile = string.Empty;
                    _linkFileState.Texture          = ChatMacros.Instance.Resources.LinkBrokenIcon;
                    _linkFileState.BasicTooltipText = $"{Resources.Inactive_File_Sync}: {Resources.Enter_a_file_path___} ({Resources.Optional})";
                    if (showError) {
                        ScreenNotification.ShowNotification(Resources.File_not_found_or_access_denied_, ScreenNotification.NotificationType.Warning);
                    }
                    return;
                }
                _linkFileState.Texture          = ChatMacros.Instance.Resources.LinkIcon;
                _linkFileState.BasicTooltipText = $"{Resources.Active_File_Sync}: {Path.GetFileName(_macro.LinkFile)}";
            }

            private void OnLinkFileUpdate(object sender, ValueEventArgs<BaseMacro> e) {
                if (e.Value is not ChatMacro chatMacro) {
                    return;
                }

                _macro.LinkFile = chatMacro.LinkFile;
                UpdateLinkFileState();

                if (_editField == null || !_macro.Id.Equals(e.Value.Id)) {
                    return;
                }

                _macro.Lines = chatMacro.Lines;

                _editField.Show(ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit ?
                                   new RawLinesEditView(_macro) :
                                   new FancyLinesEditView(_macro));
            }

            protected override void Unload() {
                ChatMacros.Instance.Macro.Observer.MacroUpdate -= OnLinkFileUpdate;
                base.Unload();
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
                    ChatMacros.Instance.Macro.UpdateMacros();
                };

                var macroConfig = new ViewContainer {
                    Parent = buildPanel,
                    Width  = buildPanel.ContentRegion.Width,
                    Height = 200,
                    Top    = titleField.Bottom + Panel.TOP_PADDING
                };
                macroConfig.Show(new BaseMacroSettings(_macro, () => ChatMacros.Instance.Data.Upsert(_macro)));

                _editField = new ViewContainer {
                    Title  = Resources.Message_Sequence,
                    Parent = buildPanel,
                    Width  = buildPanel.ContentRegion.Width,
                    Height = buildPanel.ContentRegion.Height - titleField.Bottom - macroConfig.Height - Panel.TOP_PADDING,
                    Top    = macroConfig.Bottom,
                    BasicTooltipText = "List of Placeholders\n\n"
                                     + "Placeholders are executable commands inside your messages that get replaced with their result when the message is sent.\n"
                                     + "Placeholders must be surrounded by paranthesis and their parameters are separated by whitespace.\n\n"
                                     + string.Join("\n", ChatMacros.Instance.Resources.Placeholders)
                };

                _editField.Show(ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit ? 
                                    new RawLinesEditView(_macro) : 
                                    new FancyLinesEditView(_macro));

                var openExternalBttn = new Image {
                    Parent           = buildPanel,
                    Width            = 22,
                    Height           = 22,
                    Left             = _editField.Right - 22 - Panel.RIGHT_PADDING,
                    Top              = _editField.Top + 8,
                    Texture          = ChatMacros.Instance.Resources.OpenExternalIcon,
                    BasicTooltipText = "Open and Edit in default text editor."
                };

                openExternalBttn.Click += (_, _) => {
                    var linkFileExists = FileUtil.Exists(_macro.LinkFile, out string _, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray());
                    if (linkFileExists) {
                        FileUtil.OpenExternally(_macro.LinkFile);
                        return;
                    }

                    var syncedPath = Path.Combine(ChatMacros.Instance.ModuleDirectory, "synced");
                    Directory.CreateDirectory(syncedPath);
                    var linkFilePath = Path.Combine(syncedPath, $"{_macro.Id}.txt");

                    try {
                        using var generatedLinkFile = File.CreateText(linkFilePath);
                        generatedLinkFile.WriteAsync(string.Join("\n", _macro.Lines.Select(x => x.Serialize())));
                    } catch (Exception) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        return;
                    }
                    _macro.LinkFile = linkFilePath;
                    UpdateLinkFileState(true);
                    ChatMacros.Instance.Data.LinkFileChanged(_macro);

                    FileUtil.OpenExternally(_macro.LinkFile);
                };

                _linkFileState = new Image {
                    Parent = buildPanel,
                    Width  = 22,
                    Height = 22,
                    Left   = openExternalBttn.Left - 22 - Panel.RIGHT_PADDING,
                    Top    = _editField.Top + 8
                };

                _linkFileState.LeftMouseButtonReleased += async (_, _) => {
                    using var fileSelect = new AsyncFileDialog<OpenFileDialog>(Resources.File_to_monitor_and_synchronize_lines_with_, "txt files (*.txt)|*.txt", _macro.LinkFile);
                    var       result     = await fileSelect.Show();
                    if (result == DialogResult.OK) {
                        var oldLinkFile = new string(_macro.LinkFile?.ToCharArray());

                        _macro.LinkFile = fileSelect.Dialog.FileName;

                        if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                            _macro.LinkFile = oldLinkFile;
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
                            return;
                        }

                        GameService.Content.PlaySoundEffectByName("button-click");

                        UpdateLinkFileState(true);
                        ChatMacros.Instance.Data.LinkFileChanged(_macro);
                    }
                };

                _linkFileState.RightMouseButtonReleased += (_, _) => {
                    GameService.Content.PlaySoundEffectByName("button-click");
                    _macro.LinkFile = string.Empty;
                    UpdateLinkFileState();
                    ChatMacros.Instance.Data.LinkFileChanged(_macro);
                };

                UpdateLinkFileState();
                ChatMacros.Instance.Macro.Observer.MacroUpdate += OnLinkFileUpdate;

                var editMode = new Image {
                    Parent = buildPanel,
                    Width  = 32,
                    Height = 32,
                    Left   = _linkFileState.Left - 32 - Panel.RIGHT_PADDING,
                    Top    = _editField.Top      + 4,
                    Texture = ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit ? ChatMacros.Instance.Resources.SwitchModeOnIcon : ChatMacros.Instance.Resources.SwitchModeOffIcon,
                    BasicTooltipText = "Change Edit Mode"
                };

                editMode.Click += (_, _) => {
                    GameService.Content.PlaySoundEffectByName("button-click");

                    ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit = !ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit;

                    if (ChatMacros.Instance.LibraryConfig.Value.AdvancedEdit) {
                        _editField.Show(new RawLinesEditView(_macro));
                        editMode.Texture = ChatMacros.Instance.Resources.SwitchModeOnIcon;
                    } else {
                        _editField.Show(new FancyLinesEditView(_macro));
                        editMode.Texture = ChatMacros.Instance.Resources.SwitchModeOffIcon;
                    }
                };

                base.Build(buildPanel);
            }

            private class RawLinesEditView : View {

                protected ChatMacro _macro;

                public RawLinesEditView(ChatMacro macro) {
                    _macro = macro;
                }

                protected override void Build(Container buildPanel) {
                    var editLinesBox = new MultilineTextBox {
                        Parent = buildPanel,
                        Width  = buildPanel.ContentRegion.Width,
                        Height = buildPanel.ContentRegion.Height,
                        Font   = ChatMacros.Instance.Resources.SourceCodePro24
                    };

                    editLinesBox.Text = string.Join("\n", _macro.Lines.Select(l => l.Serialize()));
                    editLinesBox.InputFocusChanged += (_, e) => {
                        if (e.Value) {
                            return; // Being focused. Do nothing.
                        }

                        var oldLines = _macro.Lines.ToList();

                        // Delete all lines to recreate them from parsing all lines of the multiline textbox.
                        if (!ChatMacros.Instance.Data.DeleteMany(_macro.Lines)) {
                            _macro.Lines = oldLines;
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
                            return;
                        }

                        _macro.Lines.Clear();

                        // Read all lines to re-add them during save.
                        var lines = editLinesBox.Text.ReadLines().Select(ChatLine.Parse).ToList();

                        if (!SaveLines(lines.ToArray())) {
                            _macro.Lines = oldLines;
                        }
                    };

                    base.Build(buildPanel);
                }

                protected bool SaveLines(params ChatLine[] lines) {

                    if (lines.IsNullOrEmpty() || !ChatMacros.Instance.Data.Insert(lines)) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        GameService.Content.PlaySoundEffectByName("error");
                        return false;
                    }

                    _macro.Lines.AddRange(lines);

                    if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        GameService.Content.PlaySoundEffectByName("error");
                        return false;
                    }

                    ChatMacros.Instance.Macro.UpdateMacros();
                    return true;
                }
            }

            private class FancyLinesEditView : RawLinesEditView {

                public FancyLinesEditView(ChatMacro macro) : base(macro) {
                    /* NOOP */
                }

                protected override void Build(Container buildPanel) {
                    var linesPanel = new FlowPanel {
                        Parent              = buildPanel,
                        Width               = buildPanel.ContentRegion.Width,
                        Height              = buildPanel.ContentRegion.Height - Panel.TOP_PADDING - 50,
                        FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                        Top                 = 0,
                        ShowBorder          = true,
                        ControlPadding      = new Vector2(0,                  2),
                        OuterControlPadding = new Vector2(Panel.LEFT_PADDING, 4),
                        CanScroll           = true
                    };

                    foreach (var line in _macro.Lines) {
                        AddLine(linesPanel, line);
                    }

                    var addLineBttn = new StandardButtonCustomFont(ChatMacros.Instance.Resources.RubikRegular26) {
                        Parent = buildPanel,
                        Width  = linesPanel.Width  - 20,
                        Top    = linesPanel.Bottom + Panel.TOP_PADDING,
                        Left   = 10,
                        Height = 50,
                        Text   = Resources.Add_Line
                    };

                    addLineBttn.Click += (_, _) => {
                        var newLine = ChatLine.Parse(null);
                        newLine.Channel   = _macro.Lines.IsNullOrEmpty() ? ChatChannel.Current : _macro.Lines.Last().Channel;
                        newLine.WhisperTo = _macro.Lines.IsNullOrEmpty() ? string.Empty : _macro.Lines.Last().WhisperTo;

                        var oldLines = _macro.Lines.ToList();
                        if (SaveLines(newLine)) {
                            GameService.Content.PlaySoundEffectByName("button-click");
                            AddLine(linesPanel, newLine);
                        } else {
                            _macro.Lines = oldLines;
                        }
                    };
                }

                private void AddLine(Container parent, ChatLine line) {
                    var lineDisplay = new ViewContainer {
                        Parent = parent,
                        Width = parent.ContentRegion.Width - 18,
                        Height = 32
                    };

                    var lineView = new LineView(line);
                    lineDisplay.Show(lineView);

                    lineView.RemoveClick += (_, _) => {
                        var oldLines = _macro.Lines.ToList();

                        if (_macro.Lines.RemoveAll(x => x.Id.Equals(line.Id)) < 1) {
                            return;
                        }

                        if (!ChatMacros.Instance.Data.Delete(line)) {
                            _macro.Lines = oldLines;
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
                            return;
                        }

                        if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
                        }

                        ChatMacros.Instance.Speech.UpdateGrammar();
                        lineDisplay.Dispose();
                    };

                    int lineIndex = _macro.Lines.IndexOf(line);
                    void OnMouseMoved(object o, MouseEventArgs mouseEventArgs) {
                        if (lineView.IsDragging) {
                            var tempLines = _macro.Lines.ToList();
                            var dropIndex = MathHelper.Clamp((parent.RelativeMousePosition.Y - Panel.HEADER_HEIGHT) / lineDisplay.Height, 0, _macro.Lines.Count - 1);

                            if (lineIndex != dropIndex) {
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
                        var dropIndex = MathHelper.Clamp((parent.RelativeMousePosition.Y - Panel.HEADER_HEIGHT) / lineDisplay.Height, 0, _macro.Lines.Count - 1);

                        var oldOrder = _macro.Lines.ToList();

                        if (_macro.Lines.RemoveAll(l => l.Id.Equals(line.Id)) < 1) {
                            _macro.Lines = oldOrder;
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
                            return;
                        }

                        _macro.Lines.Insert(dropIndex, line);

                        if (!ChatMacros.Instance.Data.Upsert(_macro)) {
                            _macro.Lines = oldOrder;
                            ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                            GameService.Content.PlaySoundEffectByName("error");
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

                private class LineView : View {

                    public event EventHandler<EventArgs> RemoveClick;
                    public event EventHandler<EventArgs> DragEnd;

                    private readonly ChatLine _line;

                    public bool IsDragging { get; private set; }

                    private AsyncTexture2D _squadBroadcastActive;
                    private AsyncTexture2D _squadBroadcastInactive;
                    private AsyncTexture2D _squadBroadcastActiveHover;
                    private AsyncTexture2D _squadBroadcastInactiveHover;

                    public LineView(ChatLine line) {
                        _line = line;
                        _squadBroadcastActive = GameService.Content.DatAssetCache.GetTextureFromAssetId(1304068);
                        _squadBroadcastActiveHover = GameService.Content.DatAssetCache.GetTextureFromAssetId(1304069);
                        _squadBroadcastInactive = GameService.Content.DatAssetCache.GetTextureFromAssetId(1234950);
                        _squadBroadcastInactiveHover = GameService.Content.DatAssetCache.GetTextureFromAssetId(1304070);
                    }

                    protected override void Build(Container buildPanel) {

                        var targetChannelDd = new KeyValueDropdown<ChatChannel> {
                            Parent = buildPanel,
                            PlaceholderText = Resources.Select_a_target_channel___,
                            SelectedItem = _line.Channel,
                            BasicTooltipText = Resources.Select_a_target_channel___,
                            AutoSizeWidth = true
                        };

                        foreach (var channel in Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>()) {
                            targetChannelDd.AddItem(channel, channel.ToDisplayName(), channel.GetHeadingColor());
                        }

                        TextBox messageInput = null;
                        TextBox whisperTo = null;
                        Image squadBroadcast = null;

                        void CreateWhisperToField() {
                            whisperTo = new TextBox {
                                Parent = buildPanel,
                                PlaceholderText = Resources.Recipient___,
                                Width = 100,
                                ForeColor = _line.Channel.GetMessageColor(),
                                Left = targetChannelDd.Right + Panel.RIGHT_PADDING,
                                Font = ChatMacros.Instance.Resources.SourceCodePro24,
                                Text = _line.WhisperTo,
                            };

                            whisperTo.TextChanged += (_, _) => {
                                _line.WhisperTo = whisperTo.Text.Trim();
                                whisperTo.BasicTooltipText = _line.WhisperTo;
                                Save();
                            };

                            messageInput.Width = buildPanel.ContentRegion.Width - whisperTo.Right - Panel.RIGHT_PADDING * 3 - 50;
                            messageInput.Left = whisperTo.Right + Panel.RIGHT_PADDING;
                            messageInput.Invalidate();
                        }

                        void CreateSquadBroadcastTick() {

                            bool hovering = false;
                            squadBroadcast = new Image {
                                Parent = buildPanel,
                                Left = targetChannelDd.Right,
                                Height = 32,
                                Width = 32,
                                Top = -2,
                                BasicTooltipText = Resources.Broadcast_to_Squad,
                                Texture = _line.SquadBroadcast ? _squadBroadcastActive : _squadBroadcastInactive
                            };

                            squadBroadcast.Click += (_, _) => {
                                _line.SquadBroadcast = !_line.SquadBroadcast;

                                if (hovering) {
                                    squadBroadcast.Texture = _line.SquadBroadcast ? _squadBroadcastActiveHover : _squadBroadcastInactiveHover;
                                } else {
                                    squadBroadcast.Texture = _line.SquadBroadcast ? _squadBroadcastActive : _squadBroadcastInactive;
                                }

                                GameService.Content.PlaySoundEffectByName("button-click");

                                Save();
                            };

                            squadBroadcast.MouseEntered += (_, _) => {
                                hovering = true;
                                squadBroadcast.Texture = _line.SquadBroadcast ? _squadBroadcastActiveHover : _squadBroadcastInactiveHover;
                            };

                            squadBroadcast.MouseLeft += (_, _) => {
                                hovering = false;
                                squadBroadcast.Texture = _line.SquadBroadcast ? _squadBroadcastActive : _squadBroadcastInactive;
                            };

                            messageInput.Width = buildPanel.ContentRegion.Width - squadBroadcast.Right - Panel.RIGHT_PADDING * 2 - 50 - Panel.RIGHT_PADDING / 2;
                            messageInput.Left = squadBroadcast.Right + Panel.RIGHT_PADDING / 2;
                            messageInput.Invalidate();
                        }

                        messageInput = new TextBox {
                            Parent = buildPanel,
                            PlaceholderText = Resources.Enter_a_message___,
                            Text = _line.Message,
                            Width = buildPanel.ContentRegion.Width - targetChannelDd.Right - Panel.RIGHT_PADDING * 3 - 50,
                            Left = targetChannelDd.Right + Panel.RIGHT_PADDING,
                            ForeColor = _line.Channel.GetMessageColor(),
                            BasicTooltipText = string.IsNullOrWhiteSpace(_line.Message) ? Resources.Enter_a_message___ : _line.ToChatMessage(),
                            Font = ChatMacros.Instance.Resources.SourceCodePro24
                        };

                        var overlengthWarn = new Image(GameService.Content.DatAssetCache.GetTextureFromAssetId(1444522)) {
                            Parent = buildPanel,
                            Left = messageInput.Right - 32,
                            Width = 32,
                            Height = 32,
                            Top = -2,
                            BasicTooltipText = string.Format(Resources.Message_exceeds_limit_of__0__characters_, ChatUtil.MAX_MESSAGE_LENGTH),
                            Visible = _line.ToChatMessage().Length > ChatUtil.MAX_MESSAGE_LENGTH,
                            ZIndex = messageInput.ZIndex + 1
                        };

                        if (targetChannelDd.SelectedItem == ChatChannel.Whisper) {
                            CreateWhisperToField();
                        } else if (targetChannelDd.SelectedItem == ChatChannel.Squad) {
                            CreateSquadBroadcastTick();
                        }

                        var remove = new Image {
                            Parent = buildPanel,
                            Width = 25,
                            Height = 25,
                            Left = messageInput.Right + Panel.RIGHT_PADDING,
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
                            BasicTooltipText = Resources.Drag_to_Reorder
                        };

                        dragReorder.LeftMouseButtonPressed += (_, _) => {
                            IsDragging = true;
                            GameService.Content.PlaySoundEffectByName("button-click");
                        };

                        void OnLeftMouseButtonReleased(object o, MouseEventArgs mouseEventArgs) {
                            if (IsDragging) {
                                this.DragEnd?.Invoke(this, EventArgs.Empty);
                            }
                            IsDragging = false;
                        }

                        GameService.Input.Mouse.LeftMouseButtonReleased += OnLeftMouseButtonReleased;

                        dragReorder.Disposed += (_, _) => {
                            GameService.Input.Mouse.LeftMouseButtonReleased -= OnLeftMouseButtonReleased;
                        };

                        messageInput.TextChanged += (_, _) => {
                            var cmd = $"{_line.Channel.ToShortChatCommand()} ".TrimStart();
                            var msg = $"{cmd}{messageInput.Text}";
                            overlengthWarn.Visible = msg.Length > ChatUtil.MAX_MESSAGE_LENGTH;
                            messageInput.BasicTooltipText = string.IsNullOrEmpty(messageInput.Text) ? Resources.Enter_a_message___ : msg;
                        };

                        messageInput.InputFocusChanged += (_, e) => {
                            if (e.Value) {
                                return;
                            }
                            _line.Message = messageInput.Text.TrimEnd();
                            messageInput.BasicTooltipText = string.IsNullOrEmpty(_line.Message) ? Resources.Enter_a_message___ : _line.ToChatMessage();
                            Save();
                        };

                        targetChannelDd.Resized += (_, _) => {
                            messageInput.Width = buildPanel.ContentRegion.Width - targetChannelDd.Width - Panel.RIGHT_PADDING * 4 - 50;
                            messageInput.Left = targetChannelDd.Right + Panel.RIGHT_PADDING;
                        };

                        targetChannelDd.ValueChanged += (_, e) => {
                            _line.Channel = e.NewValue;
                            messageInput.ForeColor = _line.Channel.GetMessageColor();
                            messageInput.BasicTooltipText = string.IsNullOrWhiteSpace(_line.Message) ? Resources.Enter_a_message___ : _line.ToChatMessage();
                            overlengthWarn.Visible = _line.ToChatMessage().Length > ChatUtil.MAX_MESSAGE_LENGTH;
                            Save();

                            whisperTo?.Dispose();
                            squadBroadcast?.Dispose();
                            messageInput.Width = buildPanel.ContentRegion.Width - targetChannelDd.Right - Panel.RIGHT_PADDING * 4 - 50;
                            messageInput.Left = targetChannelDd.Right + Panel.RIGHT_PADDING;

                            if (e.NewValue == ChatChannel.Whisper) {
                                CreateWhisperToField();
                            } else if (e.NewValue == ChatChannel.Squad) {
                                CreateSquadBroadcastTick();
                            }
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
}

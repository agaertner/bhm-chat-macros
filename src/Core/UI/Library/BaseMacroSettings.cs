using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Properties;
using System;
using System.Linq;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.ChatMacros.Core.UI.Library {
    internal class BaseMacroSettings : View {

        private BaseMacro  _macro;
        private Func<bool> _upsert;
        public BaseMacroSettings(BaseMacro macro, Func<bool> upsert) {
            _macro       = macro;
            _upsert      = upsert;
        }

        protected override void Build(Container buildPanel) {
            var contentRegion = new Rectangle(Panel.LEFT_PADDING, Panel.TOP_PADDING, buildPanel.ContentRegion.Width - Panel.RIGHT_PADDING * 2, buildPanel.ContentRegion.Height);

            var activeMapsWrap = new FlowPanel {
                Parent        = buildPanel,
                Width         = contentRegion.Width / 2 - Panel.RIGHT_PADDING / 2,
                Height        = contentRegion.Height - Panel.BOTTOM_PADDING - 16,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Title         = Resources.Active_Areas
            };

            var activeMapsPanel = new FlowPanel {
                Parent              = activeMapsWrap,
                Width               = activeMapsWrap.ContentRegion.Width,
                Height              = activeMapsWrap.ContentRegion.Height - 30 - Panel.BOTTOM_PADDING,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                ControlPadding      = new Vector2(0,                  4),
                OuterControlPadding = new Vector2(Panel.LEFT_PADDING, Panel.TOP_PADDING),
                CanScroll           = true,
                ShowBorder          = true
            };

            foreach (var id in _macro.MapIds) {
                var map = ChatMacros.Instance.Macro.AllMaps.FirstOrDefault(map => map.Id == id);
                if (map != null) {
                    AddActiveMap(activeMapsPanel, map);
                }
            }

            var addActiveMap = new TextBox {
                Parent           = activeMapsWrap,
                Width            = activeMapsWrap.ContentRegion.Width - 2,
                Height           = 30,
                BasicTooltipText = $"{Resources.Add_Map___}\n{string.Format(Resources.Current_Map_ID___0_, GameService.Gw2Mumble.CurrentMap.Id)}",
                PlaceholderText  = Resources.Add_Map___
            };

            addActiveMap.InputFocusChanged += (_, e) => {
                if (e.Value) {
                    return;
                }

                if (string.IsNullOrWhiteSpace(addActiveMap.Text)) {
                    addActiveMap.Text = string.Empty;
                    return;
                }

                Map bestMatch;

                if (int.TryParse(addActiveMap.Text.Trim(), out var mapId)) {
                    bestMatch = ChatMacros.Instance.Macro.AllMaps?.FirstOrDefault(x => x.Id == mapId);

                    if (bestMatch == null) {
                        ScreenNotification.ShowNotification(string.Format(Resources._0__does_not_exist_, string.Format(Resources.Map_ID__0_, mapId)), ScreenNotification.NotificationType.Warning);
                        addActiveMap.Text = string.Empty;
                        return;
                    }
                } else {
                    bestMatch = FastenshteinUtil.FindClosestMatchBy(addActiveMap.Text.Trim(), ChatMacros.Instance.Macro.AllMaps, map => map.Name);

                    if (bestMatch == null) {
                        ScreenNotification.ShowNotification(string.Format(Resources._0__not_found__Check_your_spelling_, $"\"{addActiveMap.Text}\""), ScreenNotification.NotificationType.Warning);
                        addActiveMap.Text = string.Empty;
                        return;
                    }
                }

                addActiveMap.Text = string.Empty;

                var oldMaps = _macro.MapIds.ToList();

                if (oldMaps.Contains(bestMatch.Id)) {
                    ScreenNotification.ShowNotification(string.Format(Resources._0__already_added_, $"\"{bestMatch.Name}\""));
                    return;
                }

                _macro.MapIds.Add(bestMatch.Id);

                if (!_upsert()) {
                    _macro.MapIds = oldMaps;
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    GameService.Content.PlaySoundEffectByName("error");
                    return;
                }

                AddActiveMap(activeMapsPanel, bestMatch);
                ChatMacros.Instance.Macro.UpdateMacros();
            };

            var activeGameModes = new FlowPanel {
                Parent              = buildPanel,
                Width               = activeMapsWrap.Width,
                Height              = 16,
                Top                 = activeMapsWrap.Bottom,
                FlowDirection       = ControlFlowDirection.SingleLeftToRight,
                ControlPadding      = new Vector2(Panel.LEFT_PADDING * 5, 0),
                OuterControlPadding = new Vector2(Panel.LEFT_PADDING, 0)
            };

            foreach (var mode in Enum.GetValues(typeof(GameMode)).Cast<GameMode>().Skip(1)) {
                var cb = new Checkbox {
                    Parent           = activeGameModes,
                    Width            = 50,
                    Height           = activeGameModes.ContentRegion.Height,
                    Checked          = _macro.HasGameMode(mode),
                    Text             = mode.ToShortDisplayString(),
                    BasicTooltipText = mode.ToDisplayString()
                };

                cb.CheckedChanged += (_, e) => {

                    if ((_macro.GameModes & ~mode) == GameMode.None) {
                        // At least one mode must be selected
                        cb.GetPrivateField("_checked").SetValue(cb, !e.Checked); // Skip invoking CheckedChanged
                        GameService.Content.PlaySoundEffectByName("error");
                        return;
                    }

                    var oldModes = _macro.GameModes;
                    if (e.Checked) {
                        _macro.GameModes |= mode;
                    } else {
                        _macro.GameModes &= ~mode;
                    }

                    if (!_upsert()) {
                        _macro.GameModes = oldModes;
                        cb.GetPrivateField("_checked").SetValue(cb, !e.Checked); // Skip invoking CheckedChanged
                        ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                        GameService.Content.PlaySoundEffectByName("error");
                        return;
                    }

                    ChatMacros.Instance.Macro.UpdateMacros();
                    GameService.Content.PlaySoundEffectByName("color-change");
                };
            }

            var voiceCommandsWrap = new FlowPanel {
                Parent        = buildPanel,
                Left          = activeMapsWrap.Right + Panel.RIGHT_PADDING * 2,
                Width         = contentRegion.Width                        / 2 - Panel.RIGHT_PADDING / 2,
                Height        = contentRegion.Height                           - Panel.BOTTOM_PADDING - 16,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Title         = Resources.Trigger_Options
            };

            #region voiceCommandsWrap children
            var commandsPanel = new FlowPanel {
                Parent              = voiceCommandsWrap,
                Width               = voiceCommandsWrap.ContentRegion.Width,
                Height              = voiceCommandsWrap.ContentRegion.Height - 30 - Panel.BOTTOM_PADDING,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                ControlPadding      = new Vector2(0,                  4),
                OuterControlPadding = new Vector2(Panel.LEFT_PADDING, Panel.TOP_PADDING),
                CanScroll           = true,
                ShowBorder          = true
            };

            var addVoiceCmd = new TextBox {
                Parent           = voiceCommandsWrap,
                Width            = voiceCommandsWrap.ContentRegion.Width,
                Height           = 30,
                BasicTooltipText = Resources.Add_Voice_Command___,
                PlaceholderText  = Resources.Add_Voice_Command___
            };

            foreach (string cmd in _macro.VoiceCommands) {
                AddVoiceCommand(commandsPanel, cmd);
            }

            addVoiceCmd.InputFocusChanged += (_, e) => {
                if (e.Value) {
                    return;
                }

                if (string.IsNullOrWhiteSpace(addVoiceCmd.Text)) {
                    addVoiceCmd.Text   = string.Empty;
                    return;
                }

                var cmds = addVoiceCmd.Text.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries)
                                      .Except(_macro.VoiceCommands, StringComparer.InvariantCultureIgnoreCase).ToArray();

                var oldCmds = _macro.VoiceCommands.ToList();
                _macro.VoiceCommands.AddRange(cmds);
                addVoiceCmd.Text = string.Empty;

                if (!_upsert()) {
                    _macro.VoiceCommands = oldCmds;
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    GameService.Content.PlaySoundEffectByName("error");
                    return;
                }
                
                foreach (string cmd in cmds) {
                    AddVoiceCommand(commandsPanel, cmd);
                }

                ChatMacros.Instance.Speech.UpdateGrammar();
            };
            #endregion voiceCommandsWrap children

            var keyBinding = new KeybindingAssigner(_macro.KeyBinding) {
                Parent         = buildPanel,
                KeyBindingName = Resources.Non_Voice_Trigger,
                Width          = voiceCommandsWrap.Width - Panel.LEFT_PADDING * 2,
                Top            = voiceCommandsWrap.Bottom,
                Left           = voiceCommandsWrap.Left + Panel.LEFT_PADDING
            };
            keyBinding.NameWidth = keyBinding.Width / 2;
            keyBinding.BindingChanged += (_, _) => {
                _upsert();
                ChatMacros.Instance.Macro.UpdateMacros();
            };

            base.Build(buildPanel);
        }

        private void AddVoiceCommand(Container parent, string command) {
            var entry = new ViewContainer {
                Parent = parent,
                Width = parent.ContentRegion.Width,
                Height = 32
            };

            var view = new ItemEntry<string>(command, x => x, x => x);
            view.Remove += (_, _) => {
                _macro.VoiceCommands.Remove(command);

                if (!_upsert()) {
                    _macro.VoiceCommands.Add(command);
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    GameService.Content.PlaySoundEffectByName("error");
                    return;
                }

                ChatMacros.Instance.Speech.UpdateGrammar();
            };
            entry.Show(view);
        }

        private void AddActiveMap(Container parent, Map map) {
            var entry = new ViewContainer {
                Parent = parent,
                Width  = parent.ContentRegion.Width,
                Height = 32,
                BasicTooltipText = string.Format(Resources.Map_ID__0_, map.Id)
            };

            var view = new ItemEntry<Map>(map, x => x.Name, x => $"{x.Name} ({x.Id})");
            view.Remove += (_, _) => {
                _macro.MapIds.Remove(map.Id);
                
                if (!_upsert()) {
                    _macro.MapIds.Add(map.Id);
                    ScreenNotification.ShowNotification(Resources.Something_went_wrong__Please_try_again_, ScreenNotification.NotificationType.Error);
                    GameService.Content.PlaySoundEffectByName("error");
                    return;
                }

                ChatMacros.Instance.Macro.UpdateMacros();
            };
            entry.Show(view);
        }

        private class ItemEntry<T> : View {

            public event EventHandler<EventArgs> Remove;
            
            private readonly T               _item;
            private          Func<T, string> _displayString;
            private          Func<T, string> _basicTooltipText;
            public ItemEntry(T item, Func<T, string> displayString, Func<T, string> basicTooltipText) {
                _item = item;
                _displayString = displayString;
                _basicTooltipText = basicTooltipText;
            }
            protected override void Build(Container buildPanel) {
                var title = new Label {
                    Parent            = buildPanel,
                    BasicTooltipText  = _basicTooltipText(_item),
                    Text              = AssetUtil.Truncate(_displayString(_item), buildPanel.ContentRegion.Width - 60, GameService.Content.DefaultFont14),
                    Font              = GameService.Content.DefaultFont14,
                    Width             = buildPanel.ContentRegion.Width - 47,
                    Height            = 24,
                    VerticalAlignment = VerticalAlignment.Middle
                };

                var delete = new Image {
                    Parent = buildPanel,
                    Width = 24,
                    Height = 24,
                    Left = title.Right + 3,
                    Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175782),
                    BasicTooltipText = Resources.Remove
                };

                delete.MouseEntered += (_, _) => {
                    delete.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175784);
                };

                delete.MouseLeft += (_, _) => {
                    delete.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175782);
                };

                delete.LeftMouseButtonPressed += (_, _) => {
                    GameService.Content.PlaySoundEffectByName("button-click");
                    delete.Texture = GameService.Content.DatAssetCache.GetTextureFromAssetId(2175783);
                };

                delete.LeftMouseButtonReleased += (_, _) => {
                    Remove?.Invoke(this, EventArgs.Empty);
                    buildPanel.Dispose();
                };

                base.Build(buildPanel);
            }
        }
    }
}

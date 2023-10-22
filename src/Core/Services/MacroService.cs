using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Extended;
using Flurl.Http;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Core.Services.Macro;
using Nekres.ChatMacros.Core.UI;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nekres.ChatMacros.Core.Services {
    internal class MacroService : IDisposable {

        public event EventHandler<ValueEventArgs<IReadOnlyList<BaseMacro>>> ActiveMacrosChange;

        private const char  COMMAND_START = '{';
        private const char  COMMAND_END   = '}';
        private const char  PARAM_CHAR    = ' ';
        private       Regex _commandRegex = new (@$"\{COMMAND_START}(?<command>[^\{COMMAND_END}]+)\{COMMAND_END}", RegexOptions.Compiled);
        private       Regex _paramRegex   = new($"{PARAM_CHAR}(?<param>[^{PARAM_CHAR}]+)", RegexOptions.Compiled);

        private IReadOnlyList<ContinentFloorRegionMap>       _regionMaps;
        private IReadOnlyList<ContinentFloorRegionMapSector> _mapSectors;

        public IReadOnlyList<Map>            AllMaps         { get; private set; }
        public Map                           CurrentMap      { get; private set; }
        public ContinentFloorRegionMapSector CurrentSector   { get; private set; }
        public string                        CurrentMapName  { get; private set; }
        public ContinentFloorRegionMapPoi    ClosestWaypoint { get; private set; }
        public ContinentFloorRegionMapPoi    ClosestPoi      { get; private set; }
        public IReadOnlyList<BaseMacro>      ActiveMacros    { get; private set; }

        private readonly ReaderWriterLockSlim _rwLock       = new();
        private          ManualResetEvent     _lockReleased = new(false);
        private          bool                 _lockAcquired = false;

        private          ContextMenuStrip     _quickAccessWindow;

        public readonly FileMacroObserver Observer;

        public MacroService() {
            ActiveMacros =  new List<BaseMacro>();

            OnMapChanged(this, new ValueEventArgs<int>(GameService.Gw2Mumble.CurrentMap.Id));
            UpdateMacros();

            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            GameService.Overlay.UserLocaleChanged       += OnUserLocaleChanged;

            Observer = new FileMacroObserver();
        }

        private async void OnUserLocaleChanged(object sender, ValueEventArgs<CultureInfo> e) {
            AllMaps = await ChatMacros.Instance.Gw2Api.GetMaps();
        }

        private void OnOpenQuickAccessActivated(object sender, EventArgs e) {
            if (!Gw2Util.IsInGame()) {
                return;
            }

            _quickAccessWindow ??= new ContextMenuStrip {
                Parent          = GameService.Graphics.SpriteScreen,
                Visible         = false,
                WidthSizingMode = SizingMode.AutoSize
            };

            AddMacrosToQuickAccess(ActiveMacros.ToList());

            GameService.Content.PlaySoundEffectByName("numeric-spinner");
            if (_quickAccessWindow.Visible) {
                _quickAccessWindow.Hide();
                return;
            }
            _quickAccessWindow.Left = GameService.Graphics.SpriteScreen.RelativeMousePosition.X - _quickAccessWindow.Width  / 2;
            _quickAccessWindow.Top  = GameService.Graphics.SpriteScreen.RelativeMousePosition.Y - (int)Math.Round(0.25f * _quickAccessWindow.Height);
            _quickAccessWindow.Show();
        }

        private void AddMacrosToQuickAccess(IReadOnlyList<BaseMacro> macros) {
            if (macros.IsNullOrEmpty()) {
                return;
            }

            foreach (var ctrl in _quickAccessWindow.Children.ToList()) {
                ctrl?.Dispose();
            }
            _quickAccessWindow.ClearChildren();
            _quickAccessWindow.Width = 1; // Reset width; otherwise WidthSizingMode Auto will never shrink the width when appropriate.

            foreach (var macro in SortMacros(ActiveMacros.ToList())) {
                var menuItem = new ContextMenuStripItem<BaseMacro>(macro) {
                    Parent = _quickAccessWindow,
                    Text   = AssetUtil.Truncate(macro.Title, 300, GameService.Content.DefaultFont14),
                    BasicTooltipText = macro.Title,
                    FontColor = macro.GetDisplayColor()
                };
                menuItem.Click += async (_, _) => {
                    GameService.Content.PlaySoundEffectByName("button-click");
                    _quickAccessWindow.Hide();
                    await Trigger(macro);
                };
                _quickAccessWindow.AddChild(menuItem);
            }
        }

        public IEnumerable<BaseMacro> SortMacros(List<BaseMacro> toSort) {
            return toSort.OrderBy(x => {
                              if (x is ChatMacro macro && !macro.Lines.IsNullOrEmpty()) {
                                  return ChatMacros.Instance.LibraryConfig.Value.IndexChannelHistory(macro.Lines[0].Channel);
                              }
                              return int.MaxValue;
                          })
                         .ThenBy(x => x is ChatMacro macro ? macro.Lines?.FirstOrDefault()?.Channel : ChatChannel.Current)
                         .ThenBy(x => x.Title.ToLowerInvariant());
        }

        public void UpdateMacros() {
            ToggleMacros(false);
            _quickAccessWindow?.Hide();
            ActiveMacros = ChatMacros.Instance.Data.GetActiveMacros();
            ActiveMacrosChange?.Invoke(this, new ValueEventArgs<IReadOnlyList<BaseMacro>>(ActiveMacros.ToList()));
            ToggleMacros(true);
        }

        public void ToggleMacros(bool enabled) {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            if (enabled) {
                ChatMacros.Instance.ControlsConfig.Value.OpenQuickAccess.Activated += OnOpenQuickAccessActivated;
            } else {
                ChatMacros.Instance.ControlsConfig.Value.OpenQuickAccess.Activated -= OnOpenQuickAccessActivated;
            }
            
            try {
                foreach (var macro in ActiveMacros.ToList()) {
                    macro.Toggle(enabled);

                    if (enabled) {
                        macro.Triggered += OnMacroTriggered;
                    } else {
                        macro.Triggered -= OnMacroTriggered;
                    }
                }
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }

        private async void OnMacroTriggered(object sender, EventArgs e) {
            await Trigger((BaseMacro)sender);

        }

        public async Task Trigger(BaseMacro macro) {
            if (macro == null) {
                return;
            }

            if (!Gw2Util.IsInGame()) {
                return;
            }

            if (macro is ChatMacro chatMacro) {
                await Fire(chatMacro);
            }
        }

        public async Task Fire(ChatMacro macro) {
            ToggleMacros(false);

            var firstLine = macro.Lines.FirstOrDefault();

            if (firstLine != null) {
                ChatMacros.Instance.LibraryConfig.Value.UpdateChannelHistory(firstLine.Channel);
            }
            
            bool isSquadbroadcastCleared = false;
            bool isChatCleared           = false;
            foreach (var line in macro.Lines.ToList()) {
                await Task.Delay(1);

                var message = await ReplaceCommands(line.ToChatMessage());

                if (string.IsNullOrEmpty(message)) {
                    break;
                }

                // Is squad broadcast and user can broadcast (is commander).
                if (line.Channel == ChatChannel.Squad && line.SquadBroadcast 
                                                      && GameService.Gw2Mumble.PlayerCharacter.IsCommander) {

                    // Check if squadbroadcast key is assigned
                    if (ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage == null || 
                        ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage.GetBindingDisplayText().Equals(string.Empty)) {

                        ScreenNotification.ShowNotification(string.Format(Resources._0__is_not_assigned_a_key_, Resources.Squad_Broadcast_Message), ScreenNotification.NotificationType.Warning);
                        break;
                    }

                    if (!isSquadbroadcastCleared) {
                        isSquadbroadcastCleared = await ChatUtil.Clear(ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage);
                    }

                    await ChatUtil.Send(message, ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage, ChatMacros.Logger);
                    continue;
                }

                if (ChatMacros.Instance.ControlsConfig.Value.ChatMessage == null 
                 || ChatMacros.Instance.ControlsConfig.Value.ChatMessage.GetBindingDisplayText().Equals(string.Empty)) {
                    ScreenNotification.ShowNotification(string.Format(Resources._0__is_not_assigned_a_key_, Resources.Chat_Message), ScreenNotification.NotificationType.Warning);
                    break;
                }

                if (!isChatCleared) {
                    isChatCleared = await ChatUtil.Clear(ChatMacros.Instance.ControlsConfig.Value.ChatMessage);
                }

                // Is whisper and has recipient specified.
                if (line.Channel == ChatChannel.Whisper) {
                    if (string.IsNullOrWhiteSpace(line.WhisperTo)) {
                        ScreenNotification.ShowNotification(Resources.Unable_to_whisper__No_recipient_specified_, ScreenNotification.NotificationType.Warning);
                        break;
                    }

                    await ChatUtil.SendWhisper(line.WhisperTo, message, ChatMacros.Instance.ControlsConfig.Value.ChatMessage, ChatMacros.Logger);
                    continue;
                }

                // Send message to chat.
                await ChatUtil.Send(message, ChatMacros.Instance.ControlsConfig.Value.ChatMessage, ChatMacros.Logger);
            }
            await Task.Delay(200);
            ToggleMacros(true);
        }

        public void Update(GameTime gameTime) {
            GetClosestPoints();
        }

        private async void OnMapChanged(object sender, ValueEventArgs<int> e) {
            UpdateMacros();

            if (!ChatMacros.Instance.Gw2Api.IsApiAvailable()) {
                return;
            }

            if (AllMaps.IsNullOrEmpty()) {
                AllMaps = await ChatMacros.Instance.Gw2Api.GetMaps();
            }

            var currentMap = AllMaps.FirstOrDefault(map => map.Id == e.Value);
            if (currentMap == null) {
                return;
            }
            CurrentMap  = currentMap;

            if (CurrentMap != null) {
                _regionMaps = await ChatMacros.Instance.Gw2Api.GetRegionMap(CurrentMap);
                _mapSectors = await ChatMacros.Instance.Gw2Api.GetMapSectors(CurrentMap);
            }
        }

        private void GetClosestPoints() {
            if (CurrentMap == null) {
                return;
            }

            var pois = _regionMaps?.Where(x => x != null).SelectMany(x => x.PointsOfInterest.Values.Distinct()).ToList();
            if (!pois.IsNullOrEmpty()) {
                var continentPosition = GameService.Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.MUMBLE, CurrentMap.MapRect, CurrentMap.ContinentRect);

                double closestPoiDistance      = double.MaxValue;
                double closestWaypointDistance = double.MaxValue;

                ContinentFloorRegionMapPoi closestPoi      = null;
                ContinentFloorRegionMapPoi closestWaypoint = null;
                // ReSharper disable once PossibleNullReferenceException
                foreach (var poi in pois) {
                    double distanceX = Math.Abs(continentPosition.X     - poi.Coord.X);
                    double distanceZ = Math.Abs(continentPosition.Z     - poi.Coord.Y);
                    double distance  = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceZ, 2));

                    switch (poi.Type.Value) {
                        case PoiType.Waypoint when distance < closestWaypointDistance:
                            closestWaypointDistance = distance;
                            closestWaypoint         = poi;
                            break;
                        case PoiType.Landmark when distance < closestPoiDistance:
                            closestPoiDistance = distance;
                            closestPoi         = poi;
                            break;
                    }
                }
                ClosestWaypoint = closestWaypoint;
                ClosestPoi      = closestPoi;
            } else {
                ClosestWaypoint = null;
                ClosestPoi      = null;
            }
            
            // Some maps consist of just a single sector and hide their actual name in it.
            CurrentMapName = _mapSectors is {Count: 1} ? _mapSectors[0].Name : CurrentMap.Name;
            
            var playerLocation = GameService.Gw2Mumble.RawClient.AvatarPosition.ToContinentCoords(CoordsUnit.MUMBLE, CurrentMap.MapRect, CurrentMap.ContinentRect).SwapYz().ToPlane();
            CurrentSector = _mapSectors?.FirstOrDefault(sector => playerLocation.Inside(sector.Bounds));
        }

        public async Task<string> ReplaceCommands(string text) {
            var matches = _commandRegex.Matches(text);
            var result = text;
            foreach (Match match in matches) {
                var command = match.Groups["command"].Value;

                var replacement = await Resolve(command);
                if (string.IsNullOrWhiteSpace(replacement)) {
                    return string.Empty;
                }

                result = result.Replace($"{COMMAND_START}{command}{COMMAND_END}", replacement);
            }
            return result;
        }

        private async Task<string> Resolve(string fullCommand) {
            var matches = _paramRegex.Matches(fullCommand);

            var args = new List<string>();
            foreach (Match match in matches) {
                var arg = match.Groups["param"].Value;
                args.Add(arg);
            }

            var paramsStart = fullCommand.IndexOf(PARAM_CHAR);

            var command = paramsStart < 0 ? fullCommand : fullCommand.Substring(0, paramsStart);
            
            return await Exec(command, args);
        }

        private async Task<string> Exec(string command, IReadOnlyList<string> args) {
            return command switch {
                "blish"  => GetVersion(),
                "time"   => DateTime.Now.ToString("HH:mm",          CultureInfo.CurrentUICulture),
                "today"  => DateTime.Now.ToString("dddd, d.M.yyyy", CultureInfo.CurrentUICulture),
                "wp"     => ClosestWaypoint?.ChatLink ?? string.Empty,
                "poi"    => ClosestPoi?.ChatLink      ?? string.Empty,
                "map"    => CurrentMapName            ?? string.Empty,
                "sector" => CurrentSector?.Name       ?? string.Empty,
                "random" => GetRandom(args).ToString(),
                "json"   => await GetJson(args),
                "txt"    => ReadTextFile(args),
                _        => string.Empty
            };
        }

        private string ReadTextFile(IReadOnlyList<string> args) {
            if (args.Count == 0) {
                return string.Empty;
            }

            var relativePath = args[0] ?? string.Empty;

            if (!FileUtil.TryReadAllLines(relativePath, out var lines, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray())) {
                return string.Empty;
            }

            int line = RandomUtil.GetRandom(0, lines.Count - 1);

            if (args.Count == 2 && int.TryParse(args[1], out int lineArg)) {
                line = lineArg <= lines.Count && lineArg > 0 ? lineArg - 1 : line;
            }
            return lines[line];

        }

        private string GetVersion() {
            var version = typeof(BlishHud).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var release = string.IsNullOrEmpty(version) ? string.Empty : $"Blish HUD v{version.Split('+').First()}";
            return release;
        }

        private int GetRandom(IReadOnlyList<string> args) {
            int max = int.MaxValue;
            int min = 0;
            if (args.Count > 0) {
                if (args.Count == 1) {
                    int.TryParse(args[0], out max);
                } else if (args.Count == 2) {
                    int.TryParse(args[0], out min);
                    int.TryParse(args[1], out max);
                }
            }
            return RandomUtil.GetRandom(min, max);
        }

        private async Task<string> GetJson(IReadOnlyList<string> args) {
            if (args.Count < 2) {
                return string.Empty;
            }

            var url = args[1];

            if (!url.IsWebLink()) {
                return string.Empty;
            }

            var response = await HttpUtil.TryAsync(() => url.GetStringAsync());

            var path = args[0];

            return JsonPropertyUtil.GetPropertyFromJson(response, path);
        }

        public bool TryImportFromFile(string filePath, out IReadOnlyList<ChatLine> messages) {
            var msgs = new List<ChatLine>();
            messages = msgs;

            if (!FileUtil.TryReadAllLines(filePath, out var lines, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray())) {
                return false;
            }

            msgs.AddRange(lines.Select(ChatLine.Parse));
            return true;
        }

        public void Dispose() {
            GameService.Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            GameService.Overlay.UserLocaleChanged       -= OnUserLocaleChanged;
            ToggleMacros(false);

            Observer.Dispose();

            // Wait for the lock to be released
            if (_lockAcquired) {
                _lockReleased.WaitOne(500);
            }

            _lockReleased.Dispose();

            _quickAccessWindow?.Dispose();

            // Dispose the lock
            try {
                _rwLock.Dispose();
            } catch (Exception ex) {
                ChatMacros.Logger.Debug(ex, ex.Message);
            }
        }
    }
}

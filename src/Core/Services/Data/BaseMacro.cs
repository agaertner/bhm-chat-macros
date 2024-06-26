﻿using Blish_HUD.Input;
using LiteDB;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.Services.Data {

    [Flags]
    public enum GameMode {
        None  = 0,
        PvE   = 1 << 0,
        WvW   = 1 << 1,
        PvP   = 1 << 2,
    }

    internal static class GameModeExtensions {
        public static string ToShortDisplayString(this GameMode mode) {
            return mode switch {
                GameMode.PvE => Resources.PvE,
                GameMode.WvW => Resources.WvW,
                GameMode.PvP => Resources.PvP,
                _            => string.Empty
            };
        }
        public static string ToDisplayString(this GameMode mode) {
            return mode switch {
                GameMode.PvE => Resources.Player_vs__Environment,
                GameMode.WvW => Resources.World_vs__World,
                GameMode.PvP => Resources.Player_vs__Player,
                _            => string.Empty
            };
        }
    }

    internal abstract class BaseMacro {
        public event EventHandler<EventArgs> Triggered;

        [BsonId(true)]
        public ObjectId Id { get; set; }

        private string _title;
        [BsonField("title")]
        public string Title {
            get => _title ?? string.Empty;
            set => _title = value ?? string.Empty;
        }

        [BsonField("voice_commands")]
        public List<string> VoiceCommands { get; set; }

        [BsonField("key_binding")]
        public KeyBinding KeyBinding { get; set; }

        [BsonField("game_mode")]
        public GameMode GameModes { get; set; }

        [BsonField("map_ids")]
        public List<int> MapIds { get; set; }

        private string _linkFile;
        [BsonField("link_file")]
        public string LinkFile {
            get => _linkFile ?? string.Empty;
            set => _linkFile = value ?? string.Empty;
        }

        protected BaseMacro() {
            Title         = string.Empty;
            KeyBinding    = new KeyBinding { Enabled = false };
            MapIds        = new List<int>();
            VoiceCommands = new List<string>();
            GameModes     = GameMode.PvE | GameMode.WvW | GameMode.PvP;
        }

        public void Toggle(bool enable) {
            KeyBinding.Enabled = enable;

            if (enable) {
                KeyBinding.Activated += OnKeyBindingActivated;
            } else {
                KeyBinding.Activated -= OnKeyBindingActivated;
            }
        }

        private void OnKeyBindingActivated(object sender, EventArgs e) {
            Triggered?.Invoke(this, EventArgs.Empty);
        }

        public bool HasGameMode(GameMode mode) {
            return (this.GameModes & mode) == mode;
        }

        public bool HasMapId(int id) {
            return (!this.MapIds?.Any() ?? true) || this.MapIds.Contains(id); // Enable on all maps if no map ids are specified.
        }

        public virtual Color GetDisplayColor() {
            return Color.White;
        }

        public static string[] GetCommands<T>(IEnumerable<T> macros) where T : BaseMacro {
            return macros?.Where(x => x.VoiceCommands != null)
                          .SelectMany(x => x.VoiceCommands).ToArray() ?? Array.Empty<string>();
        }
    }
}

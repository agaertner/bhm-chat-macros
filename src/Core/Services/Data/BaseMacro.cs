using LiteDB;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;

namespace Nekres.ChatMacros.Core.Services.Data {

    [Flags]
    public enum GameMode {
        None  = 0,
        PvE   = 1 << 0,
        WvW   = 1 << 1,
        PvP   = 1 << 2,
    }

    internal abstract class BaseMacro {

        public event EventHandler<ValueEventArgs<string>> TitleChanged;
        
        [BsonId(true)]
        public ObjectId Id { get; set; }

        private string _title;
        [BsonField("title")]
        public string Title { 
            get => _title;
            set {
                _title = value;
                TitleChanged?.Invoke(this, new ValueEventArgs<string>(value));
            }
        }

        [BsonField("voice_commands")]
        public List<string> VoiceCommands { get; set; }

        [BsonField("modifier_key")]
        public ModifierKeys ModifierKey { get; set; }

        [BsonField("primary_key")]
        public Keys PrimaryKey { get; set; }

        [BsonField("game_mode")]
        public GameMode GameModes { get; set; }

        [BsonField("map_ids")]
        public List<int> MapIds { get; set; }

        public bool HasGameMode(GameMode mode) {
            return (this.GameModes & mode) == mode;
        }

        public bool HasMapId(int id) {
            return (!this.MapIds?.Any() ?? true) || this.MapIds.Contains(id); // Enable on all maps if no map ids are specified.
        }

        public static string[] GetCommands<T>(List<T> macros) where T : BaseMacro {
            return macros?.Where(x => x.VoiceCommands != null)
                          .SelectMany(x => x.VoiceCommands).ToArray() ?? Array.Empty<string>();
        }
    }
}

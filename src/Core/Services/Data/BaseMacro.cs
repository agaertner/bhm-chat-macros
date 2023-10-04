using LiteDB;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;

namespace Nekres.ChatMacros.Core.Services.Data {
    public enum GameMode {
        All,
        PvE,
        WvW,
        PvP
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

        [BsonField("modifierKey")]
        public ModifierKeys ModifierKey { get; set; }

        [BsonField("primaryKey")]
        public Keys PrimaryKey { get; set; }

        [BsonField("gameMode")]
        public GameMode GameMode { get; set; }

        [BsonField("mapIds")]
        public List<int> MapIds { get; set; }

        public static string[] GetCommands<T>(List<T> macros) where T : BaseMacro {
            return macros?.Where(x => x.VoiceCommands != null)
                          .SelectMany(x => x.VoiceCommands).ToArray() ?? Array.Empty<string>();
        }
    }
}

using LiteDB;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Nekres.ChatMacros.Core.Services.Data {
    public enum GameMode {
        All,
        PvE,
        WvW,
        PvP
    }

    internal abstract class BaseMacro
    {
        [BsonId(true)]
        public ObjectId Id { get; set; }

        [BsonField("title")]
        public string Title { get; set; }

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
    }
}

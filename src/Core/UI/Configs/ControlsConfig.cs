using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace Nekres.ChatMacros.Core.UI.Configs {
    internal class ControlsConfig : ConfigBase {

        public static ControlsConfig Default = new() {
            ChatMessage           = new KeyBinding(Keys.Enter) { Enabled = false, IgnoreWhenInTextField = true },
            SquadBroadcastMessage = new KeyBinding(ModifierKeys.Shift, Keys.Enter) { Enabled = false, IgnoreWhenInTextField = true },
            OpenQuickAccess       = new KeyBinding()
        };

        private KeyBinding _openQuickAccess;
        [JsonProperty("open_quick_access")]
        public KeyBinding OpenQuickAccess {
            get => _openQuickAccess;
            set => _openQuickAccess = ResetDelegates(_openQuickAccess, value);
        }

        private KeyBinding _chatMessage;
        [JsonProperty("chat_message")]
        public KeyBinding ChatMessage {
            get => _chatMessage;
            set => _chatMessage = ResetDelegates(_chatMessage, value);
        }

        private KeyBinding _squadBroadcastMessage;
        [JsonProperty("squad_broadcast_message")]
        public KeyBinding SquadBroadcastMessage {
            get => _squadBroadcastMessage;
            set => _squadBroadcastMessage = ResetDelegates(_squadBroadcastMessage, value);
        }

        protected override void BindingChanged() {
            SaveConfig(ChatMacros.Instance.ControlsConfig);
        }
    }
}

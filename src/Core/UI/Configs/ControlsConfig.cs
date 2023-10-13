using Blish_HUD.Input;
using Newtonsoft.Json;

namespace Nekres.ChatMacros.Core.UI.Configs {
    internal class ControlsConfig : ConfigBase {

        public static ControlsConfig Default = new() {
            OpenQuickAccess = new KeyBinding()
        };

        private KeyBinding _openQuickAccess;
        [JsonProperty("open_quick_access")]
        public KeyBinding OpenQuickAccess {
            get => _openQuickAccess;
            set => _openQuickAccess = ResetDelegates(_openQuickAccess, value);
        }

        protected override void BindingChanged() {
            SaveConfig(ChatMacros.Instance.ControlsConfig);
        }
    }
}

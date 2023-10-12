using Newtonsoft.Json;

namespace Nekres.ChatMacros.Core.UI.Configs {
    internal class LibraryConfig : ConfigBase {
        public static LibraryConfig Default = new();

        private bool _showActivesOnly;
        [JsonProperty("show_actives_only")]
        public bool ShowActivesOnly {
            get => _showActivesOnly;
            set {
                _showActivesOnly = value;
                this.SaveConfig(ChatMacros.Instance.LibraryConfig);
            }
        }
    }
}

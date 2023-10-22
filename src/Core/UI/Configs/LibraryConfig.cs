using Nekres.ChatMacros.Core.Services.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.UI.Configs {
    internal class LibraryConfig : ConfigBase {
        public static LibraryConfig Default = new() {
            ChannelHistory = new List<ChatChannel>()
        };

        private bool _showActivesOnly;
        [JsonProperty("show_actives_only")]
        public bool ShowActivesOnly {
            get => _showActivesOnly;
            set {
                if (SetProperty(ref _showActivesOnly, value)) {
                    this.SaveConfig(ChatMacros.Instance.LibraryConfig);
                }
            }
        }

        private List<ChatChannel> _channelHistory = new();
        [JsonProperty("channel_history")]
        public List<ChatChannel> ChannelHistory {
            get => _channelHistory;
            set {
                if (SetProperty(ref _channelHistory, value)) {
                    this.SaveConfig(ChatMacros.Instance.LibraryConfig);
                }
            }
        }

        public void UpdateChannelHistory(ChatChannel usedChannel) {
            _channelHistory ??= new List<ChatChannel>();
            ChannelHistory.Remove(usedChannel);
            ChannelHistory = ChannelHistory.Prepend(usedChannel).ToList();
        }

        public int IndexChannelHistory(ChatChannel channel) {
            int indexInHistory = ChannelHistory.IndexOf(channel);
            return indexInHistory != -1 ? indexInHistory : int.MaxValue;
        }
    }
}

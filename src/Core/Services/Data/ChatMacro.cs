using LiteDB;
using Nekres.ChatMacros.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.Services.Data {

    public enum Channel {
        Current,
        Say,
        Map,
        Party,
        Squad,
        Team,
        Reply,
        Guild
    }

    public static class ChannelExtensions {
        public static string ToChatCommand(this Channel channel) {
            return channel switch {
                Channel.Current => string.Empty,
                Channel.Say     => Resources._say,
                Channel.Map     => Resources._map,
                Channel.Party   => Resources._party,
                Channel.Squad   => Resources._squad,
                Channel.Team    => Resources._team,
                Channel.Reply   => Resources._reply,
                Channel.Guild   => Resources._guild,
                _               => string.Empty
            };
        }
    }

    internal class ChatMacro : BaseMacro {
        [BsonField("lines")]
        public List<(Channel, string)> Lines { get; set; }

        [BsonIgnore]
        public IEnumerable<string> Messages => Lines?.Select(LineToString) ?? Enumerable.Empty<string>();

        private string LineToString((Channel, string) line) {
            // Trim channel prefix if it is not specified while allowing spaces in the message.
            return $"{$"{line.Item1.ToChatCommand()} ".TrimStart()}{line.Item2}";
        }
    }
}

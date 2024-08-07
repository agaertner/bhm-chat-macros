using Gw2Sharp.WebApi;
using LiteDB;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nekres.ChatMacros.Core.Services.Data {

    public enum ChatChannel {
        Current,
        Emote,
        Say,
        Map,
        Party,
        Squad,
        Team,
        Reply,
        Whisper,
        Guild,
        Guild1,
        Guild2,
        Guild3,
        Guild4,
        Guild5,
        Guild6
    }

    public static class ChannelExtensions {
        public static string ToShortChatCommand(this ChatChannel channel) {
            return channel switch {
                ChatChannel.Current => string.Empty,
                ChatChannel.Emote   => Resources._e,
                ChatChannel.Say     => Resources._s,
                ChatChannel.Map     => Resources._m,
                ChatChannel.Party   => Resources._p,
                ChatChannel.Squad   => Resources._d,
                ChatChannel.Team    => Resources._t,
                ChatChannel.Reply   => Resources._r,
                ChatChannel.Whisper => Resources._w,
                ChatChannel.Guild   => Resources._g,
                ChatChannel.Guild1  => string.Format(Resources._g_0_, 1),
                ChatChannel.Guild2  => string.Format(Resources._g_0_, 2),
                ChatChannel.Guild3  => string.Format(Resources._g_0_, 3),
                ChatChannel.Guild4  => string.Format(Resources._g_0_, 4),
                ChatChannel.Guild5  => string.Format(Resources._g_0_, 5),
                ChatChannel.Guild6  => string.Format(Resources._g_0_, 6),
                _                   => string.Empty
            };
        }

        public static string ToChatCommand(this ChatChannel channel) {
            return channel switch {
                ChatChannel.Current => string.Empty,
                ChatChannel.Emote   => Resources._emote,
                ChatChannel.Say     => Resources._say,
                ChatChannel.Map     => Resources._map,
                ChatChannel.Party   => Resources._party,
                ChatChannel.Squad   => Resources._squad,
                ChatChannel.Team    => Resources._team,
                ChatChannel.Reply   => Resources._reply,
                ChatChannel.Whisper => Resources._whisper,
                ChatChannel.Guild   => Resources._guild,
                ChatChannel.Guild1  => string.Format(Resources._guild_0_, 1),
                ChatChannel.Guild2  => string.Format(Resources._guild_0_, 2),
                ChatChannel.Guild3  => string.Format(Resources._guild_0_, 3),
                ChatChannel.Guild4  => string.Format(Resources._guild_0_, 4),
                ChatChannel.Guild5  => string.Format(Resources._guild_0_, 5),
                ChatChannel.Guild6  => string.Format(Resources._guild_0_, 6),
                _                   => string.Empty
            };
        }

        public static string ToDisplayName(this ChatChannel channel, bool brackets = true) {
            var name = channel switch {
                ChatChannel.Current => Resources.Whichever,
                ChatChannel.Emote   => Resources.Emote,
                ChatChannel.Say     => Resources.Say,
                ChatChannel.Map     => Resources.Map,
                ChatChannel.Party   => Resources.Party,
                ChatChannel.Squad   => Resources.Squad,
                ChatChannel.Team    => Resources.Team,
                ChatChannel.Reply   => Resources.Reply,
                ChatChannel.Whisper => Resources.Whisper,
                ChatChannel.Guild   => Resources.Guild,
                ChatChannel.Guild1  => string.Format(Resources.G_0_, 1),
                ChatChannel.Guild2  => string.Format(Resources.G_0_, 2),
                ChatChannel.Guild3  => string.Format(Resources.G_0_, 3),
                ChatChannel.Guild4  => string.Format(Resources.G_0_, 4),
                ChatChannel.Guild5  => string.Format(Resources.G_0_, 5),
                ChatChannel.Guild6  => string.Format(Resources.G_0_, 6),
                _                   => string.Empty
            };
            return string.IsNullOrEmpty(name) || !brackets ? name : $"[{name}]";
        }

        public static Color GetHeadingColor(this ChatChannel channel) {
            return channel switch {
                ChatChannel.Current => Color.White,
                ChatChannel.Emote   => new Color(136, 136, 136),
                ChatChannel.Say     => new Color(118, 217, 140),
                ChatChannel.Map     => new Color(170, 79,  68),
                ChatChannel.Party   => new Color(63,  155, 229),
                ChatChannel.Squad   => new Color(173, 218, 91),
                ChatChannel.Team    => new Color(221, 48,  49),
                ChatChannel.Reply   => new Color(177, 78,  169),
                ChatChannel.Whisper => new Color(177, 78,  169),
                ChatChannel.Guild   => new Color(204, 150, 42),
                ChatChannel.Guild1  => new Color(141, 129, 86),
                ChatChannel.Guild2  => new Color(141, 129, 86),
                ChatChannel.Guild3  => new Color(141, 129, 86),
                ChatChannel.Guild4  => new Color(141, 129, 86),
                ChatChannel.Guild5  => new Color(141, 129, 86),
                ChatChannel.Guild6  => new Color(141, 129, 86),
                _                   => Color.White
            } * 1.25f;
        }

        public static Color GetMessageColor(this ChatChannel channel) {
            return channel switch {
                ChatChannel.Current => Color.White,
                ChatChannel.Emote   => new Color(136, 136, 136),
                ChatChannel.Say     => new Color(220, 224, 233),
                ChatChannel.Map     => new Color(200, 168, 164),
                ChatChannel.Party   => new Color(147, 192, 223),
                ChatChannel.Squad   => new Color(191, 240, 230),
                ChatChannel.Team    => new Color(212, 212, 212),
                ChatChannel.Reply   => new Color(222, 135, 208),
                ChatChannel.Whisper => new Color(222, 135, 208),
                ChatChannel.Guild   => new Color(208, 192, 138),
                ChatChannel.Guild1  => new Color(227, 217, 164),
                ChatChannel.Guild2  => new Color(227, 217, 164),
                ChatChannel.Guild3  => new Color(227, 217, 164),
                ChatChannel.Guild4  => new Color(227, 217, 164),
                ChatChannel.Guild5  => new Color(227, 217, 164),
                ChatChannel.Guild6  => new Color(227, 217, 164),
                _                   => Color.White
            };
        }
    }

    internal class ChatMacro : BaseMacro {

        [BsonRef(DataService.TBL_CHATLINES), BsonField("lines")]
        public List<ChatLine> Lines { get; set; }

        public ChatMacro() {
            Lines = new List<ChatLine>();
        }

        public override Color GetDisplayColor() {
            return this.Lines.IsNullOrEmpty() ? base.GetDisplayColor() : this.Lines[0].Channel.GetHeadingColor();
        }

        public List<string> ToChatMessage() {
            return Lines.Select(line => line.ToChatMessage()).ToList();
        }

        public string Serialize() {
            return string.Join("\r\n", this.Lines?.Select(chatLine => chatLine.Serialize()) ?? Enumerable.Empty<string>()) + "\r\n";
        }
    }

    internal class ChatLine {
        [BsonId(true)]
        public ObjectId Id { get; set; }

        [BsonField("channel")]
        public ChatChannel Channel { get; set; }

        private string _whisperTo;
        [BsonField("whisper_to")]
        public string WhisperTo {
            get => _whisperTo ?? string.Empty;
            set => _whisperTo = value ?? string.Empty;
        }

        [BsonField("squad_broadcast")]
        public bool SquadBroadcast { get; set; }

        private string _message;
        [BsonField("message")]
        public string Message {
            get => _message ?? string.Empty; 
            set => _message = value ?? string.Empty;
        }

        public string ToChatMessage() {
            var cmd = $"{Channel.ToShortChatCommand()} ".TrimStart();
            return $"{cmd}{Message}";
        }

        private static Regex _whisperRecipientPattern = new (@"^\[(?<name>.*)\]", RegexOptions.Compiled);
        private static Regex _squadBroadcastPattern   = new(@"^!", RegexOptions.Compiled);

        public static ChatLine Parse(string input) {

            var line = new ChatLine {
                Id = new ObjectId()
            };

            if (input.IsNullOrEmpty()) {
                return line;
            }

            var channel = ParseChannel(ref input);
            line.Channel = channel;

            if (channel == ChatChannel.Current) {
                line.Message = input.TrimEnd();
                return line;
            }

            string message = input.TrimStart(1);

            if (channel == ChatChannel.Whisper) {
                var recipientMatch = _whisperRecipientPattern.Match(message);
                if (recipientMatch.Success) {
                    line.WhisperTo = recipientMatch.Groups["name"].Value;
                    message        = message.Remove(0, recipientMatch.Index + recipientMatch.Length);
                }

            } else if (channel == ChatChannel.Squad) {
                var broadcastMatch = _squadBroadcastPattern.Match(message);
                if (broadcastMatch.Success) {
                    line.SquadBroadcast = true;
                    message             = message.Remove(0, broadcastMatch.Index + broadcastMatch.Length);
                }
            }

            line.Message = message.TrimEnd().TrimStart(1);
            return line;
        }

        private static ChatChannel ParseChannel(ref string input) {
            if (string.IsNullOrEmpty(input)) {
                return ChatChannel.Current;
            }

            var currentCulture = Resources.Culture;
            var channel        = ChatChannel.Current;

            foreach (var culture in Enum.GetValues(typeof(Locale)).Cast<Locale>().Select(l => l.GetCulture())) {
                Resources.Culture = culture;

                foreach (var chatChannel in Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>().Skip(1)) {
                    var shortcmd = chatChannel.ToShortChatCommand();
                    var cmd      = chatChannel.ToChatCommand();

                    var command = input.Split(' ')[0];

                    if (chatChannel == ChatChannel.Whisper) {
                        command = command.Split('[')[0];
                    }

                    if (chatChannel == ChatChannel.Squad) {
                        command = command.Split('!')[0];
                    }

                    if (command.Equals(shortcmd, StringComparison.InvariantCultureIgnoreCase)) {
                        channel           = chatChannel;
                        input             = input.Replace(shortcmd, string.Empty, 1);
                        Resources.Culture = currentCulture;
                        return channel;
                    }

                    if (command.Equals(cmd, StringComparison.InvariantCultureIgnoreCase)) {
                        channel           = chatChannel;
                        input             = input.Replace(cmd, string.Empty, 1);
                        Resources.Culture = currentCulture;
                        return channel;
                    }
                }
            }
            Resources.Culture = currentCulture;
            return channel;
        }

        public string Serialize() {
            var param = string.Empty;
            if (Channel == ChatChannel.Whisper && !WhisperTo.IsNullOrEmpty()) {
                param = $"[{WhisperTo}]";
            } else if (Channel == ChatChannel.Squad && SquadBroadcast) {
                param = "!";
            }

            var channel = Channel.ToShortChatCommand();
            
            if (channel.IsNullOrEmpty()) {
                return Message;
            }

            if (param.IsNullOrEmpty()) {
                return $"{channel} {Message}";
            }

            return $"{channel} {param} {Message}";
        }
    }
}

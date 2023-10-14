using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Extended;
using Gw2Sharp.WebApi;
using LiteDB;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        Guild5
    }

    public static class ChannelExtensions {
        public static string ToChatCommand(this ChatChannel channel) {
            var ch = channel switch {
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
                _                   => string.Empty
            };
            return $"{ch} ".TrimStart();
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
                _                   => Color.White
            };
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

        public List<string> ToChatMessage() {
            return Lines.Select(line => line.ToChatMessage()).ToList();
        }

        public override async Task Fire() {
            ChatMacros.Instance.Macro.ToggleMacros(false);
            foreach (var line in Lines) {
                Thread.Sleep(2);

                var message = await ChatMacros.Instance.Macro.ReplaceCommands(line.ToChatMessage());

                if (string.IsNullOrWhiteSpace(message)) {
                    break;
                }

                if (line.Channel == ChatChannel.Squad && line.SquadBroadcast && 
                    GameService.Gw2Mumble.PlayerCharacter.IsCommander) {
                    if (ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage == null || ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage.GetBindingDisplayText().Equals(string.Empty)) {
                        ScreenNotification.ShowNotification(string.Format(Resources._0__is_not_assigned_a_key_, Resources.Squad_Broadcast_Message), ScreenNotification.NotificationType.Warning);
                        break;
                    }
                    ChatUtil.Send(message, ChatMacros.Instance.ControlsConfig.Value.SquadBroadcastMessage);
                    continue;
                }

                if (ChatMacros.Instance.ControlsConfig.Value.ChatMessage == null || ChatMacros.Instance.ControlsConfig.Value.ChatMessage.GetBindingDisplayText().Equals(string.Empty)) {
                    ScreenNotification.ShowNotification(string.Format(Resources._0__is_not_assigned_a_key_, Resources.Chat_Message), ScreenNotification.NotificationType.Warning);
                    break;
                }

                if (line.Channel == ChatChannel.Whisper) {
                    if (string.IsNullOrWhiteSpace(line.WhisperTo)) {
                        ScreenNotification.ShowNotification(Resources.Unable_to_whisper__No_recipient_specified_, ScreenNotification.NotificationType.Warning);
                        break;
                    }

                    ChatUtil.SendWhisper(line.WhisperTo, message, ChatMacros.Instance.ControlsConfig.Value.ChatMessage);
                    continue;
                }

                ChatUtil.Send(message, ChatMacros.Instance.ControlsConfig.Value.ChatMessage);
            }
            await Task.Delay(200);
            ChatMacros.Instance.Macro.ToggleMacros(true);
        }
    }

    internal class ChatLine {
        [BsonId(true)]
        public ObjectId Id { get; set; }

        [BsonField("channel")]
        public ChatChannel Channel { get; set; }

        [BsonField("whisper_to")]
        public string WhisperTo { get; set; }

        [BsonField("squad_broadcast")]
        public bool SquadBroadcast { get; set; } 

        [BsonField("message")]
        public string Message { get; set; }

        public string ToChatMessage() {
            return $"{Channel.ToChatCommand()}{Message}";
        }

        private static Regex _whisperRecipientPattern = new (@"<@(?<name>.*)>", RegexOptions.Compiled);
        private const string _squadBroadcastPattern   = "<!>";
        public static ChatLine Parse(string input) {

            var line = new ChatLine {
                Id = new ObjectId()
            };

            if (input.IsNullOrEmpty()) {
                return line;
            }

            var separator  = input.IndexOf(' ');
            var channelStr = separator < 0 ? string.Empty : input.Substring(0, separator).Trim();
            var message = channelStr.Length >= input.Length ? string.Empty : input.Substring(channelStr.Length);
            message = message.TrimStart(1);

            var channel = ParseChannel(channelStr);

            line.Channel = channel;
            
            if (channel == ChatChannel.Whisper) {
                var recipientMatch = _whisperRecipientPattern.Match(message);

                if (recipientMatch.Success) {
                    line.WhisperTo = recipientMatch.Groups["name"].Value;
                    message = _whisperRecipientPattern.Replace(message, string.Empty, 1);
                    message = message.TrimStart(1);
                }
            }

            if (channel == ChatChannel.Squad) {
                line.SquadBroadcast = message.TrimStart().StartsWith(_squadBroadcastPattern);

                if (line.SquadBroadcast) {
                    message = message.Replace(_squadBroadcastPattern, string.Empty);
                    message = message.TrimStart(1);
                }
            }

            line.Message = message.TrimEnd();
            return line;
        }

        private static ChatChannel ParseChannel(string input) {
            if (string.IsNullOrEmpty(input)) {
                return ChatChannel.Current;
            }

            input = input.Trim().ToLowerInvariant();

            var currentCulture = Resources.Culture;
            var channel        = ChatChannel.Current;
            foreach (var culture in Enum.GetValues(typeof(Locale)).Cast<Locale>().Select(l => l.GetCulture())) {
                Resources.Culture = culture;

                if (input.Equals(Resources._emote)) {
                    channel = ChatChannel.Emote;
                    break;
                }

                if (input.Equals(Resources._say)) {
                    channel = ChatChannel.Say;
                    break;
                }

                if (input.Equals(Resources._map)) {
                    channel = ChatChannel.Map;
                    break;
                }

                if (input.Equals(Resources._party)) {
                    channel = ChatChannel.Party;
                    break;
                }

                if (input.Equals(Resources._squad)) {
                    channel = ChatChannel.Squad;
                    break;
                }

                if (input.Equals(Resources._team)) {
                    channel = ChatChannel.Team;
                    break;
                }

                if (input.Equals(Resources._reply)) {
                    channel = ChatChannel.Reply;
                    break;
                }

                if (input.Equals(Resources._whisper)) {
                    channel = ChatChannel.Whisper;
                    break;
                }

                if (input.Equals(Resources._guild)) {
                    channel = ChatChannel.Guild;
                    break;
                }

                if (input.Equals(string.Format(Resources._guild_0_, 1))) {
                    channel = ChatChannel.Guild1;
                    break;
                }

                if (input.Equals(string.Format(Resources._guild_0_, 2))) {
                    channel = ChatChannel.Guild2;
                    break;
                }

                if (input.Equals(string.Format(Resources._guild_0_, 3))) {
                    channel = ChatChannel.Guild3;
                    break;
                }

                if (input.Equals(string.Format(Resources._guild_0_, 4))) {
                    channel = ChatChannel.Guild4;
                    break;
                }

                if (input.Equals(string.Format(Resources._guild_0_, 5))) {
                    channel = ChatChannel.Guild5;
                    break;
                }
            }
            Resources.Culture = currentCulture;
            return channel;
        }
    }
}

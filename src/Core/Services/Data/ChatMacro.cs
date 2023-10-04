﻿using LiteDB;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;
using System.Collections.Generic;
using System.Linq;

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
            return $"{channel switch {
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
                _               => string.Empty
            }} ".TrimStart();
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

        public List<string> ToChatMessage() {
            return Lines.Select(line => line.ToChatMessage()).ToList();
        }
    }

    internal class ChatLine {
        [BsonId(true)]
        public ObjectId Id { get; set; }

        [BsonField("channel")]
        public ChatChannel Channel { get; set; }

        [BsonField("message")]
        public string Message { get; set; }

        public string ToChatMessage() {
            return $"{Channel.ToChatCommand()}{Message}";
        }
    }
}
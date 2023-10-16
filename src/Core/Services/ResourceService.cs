using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Nekres.ChatMacros.Core.Services {
    internal class ResourceService : IDisposable {

        public Color      BrightGold = new (223, 194, 149, 255);
        public Texture2D  DragReorderIcon { get; private set; }
        public Texture2D  TwitchLogo      { get; private set; }
        public Texture2D  YoutubeLogo     { get; private set; }
        public Texture2D  EditIcon        { get; private set; }
        public Texture2D  LinkIcon        { get; private set; }
        public Texture2D  LinkBrokenIcon  { get; private set; }
        public BitmapFont RubikRegular26  { get; private set; }
        public BitmapFont LatoRegular24   { get; private set; }
        public BitmapFont SourceCodePro24     { get; private set; }

        private IReadOnlyList<SoundEffect> _menuClicks;
        private SoundEffect                _menuItemClickSfx;

        public ResourceService() {
            LoadTextures();
            LoadFonts();
            LoadSounds();
        }

        public void LoadTextures() {
            DragReorderIcon = ChatMacros.Instance.ContentsManager.GetTexture("icons/drag-reorder.png");
            EditIcon        = ChatMacros.Instance.ContentsManager.GetTexture("icons/edit_icon.png");
            LinkIcon        = ChatMacros.Instance.ContentsManager.GetTexture("icons/link.png");
            LinkBrokenIcon  = ChatMacros.Instance.ContentsManager.GetTexture("icons/link-broken.png");
            TwitchLogo      = ChatMacros.Instance.ContentsManager.GetTexture("socials/twitch_logo.png");
            YoutubeLogo     = ChatMacros.Instance.ContentsManager.GetTexture("socials/youtube_logo.png");
        }

        public void PlayMenuItemClick() {
            _menuItemClickSfx.Play(GameService.GameIntegration.Audio.Volume, 0, 0);
        }

        public void PlayMenuClick() {
            _menuClicks[RandomUtil.GetRandom(0, 3)].Play(GameService.GameIntegration.Audio.Volume, 0, 0);
        }

        private void LoadFonts() {
            this.RubikRegular26 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/Rubik-Regular.ttf", 26);
            this.LatoRegular24 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/Lato-Regular.ttf",  24);
            this.SourceCodePro24 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/SourceCodePro-SemiBold.ttf", 24);
        }

        private void LoadSounds() {
            _menuItemClickSfx = ChatMacros.Instance.ContentsManager.GetSound(@"audio\menu-item-click.wav");
            _menuClicks = new List<SoundEffect> {
                ChatMacros.Instance.ContentsManager.GetSound(@"audio\menu-click-1.wav"),
                ChatMacros.Instance.ContentsManager.GetSound(@"audio\menu-click-2.wav"),
                ChatMacros.Instance.ContentsManager.GetSound(@"audio\menu-click-3.wav"),
                ChatMacros.Instance.ContentsManager.GetSound(@"audio\menu-click-4.wav")
            };
        }

        public void Dispose() {
            this.RubikRegular26?.Dispose();
            this.LatoRegular24?.Dispose();
            this.SourceCodePro24?.Dispose();
            this.DragReorderIcon?.Dispose();
            this.EditIcon?.Dispose();
            this.LinkIcon?.Dispose();
            this.LinkBrokenIcon?.Dispose();
            this.TwitchLogo?.Dispose();
            this.YoutubeLogo?.Dispose();
        }

    }
}

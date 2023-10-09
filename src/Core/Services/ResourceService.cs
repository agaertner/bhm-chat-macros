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
        public Texture2D  EditIcon        { get; private set; }
        public BitmapFont Menomonia24     { get; private set; }
        public BitmapFont RubikRegular26  { get; private set; }

        private IReadOnlyList<SoundEffect> _menuClicks;
        private SoundEffect                _menuItemClickSfx;

        public ResourceService() {
            DragReorderIcon = ChatMacros.Instance.ContentsManager.GetTexture("icons/drag-reorder.png");
            EditIcon        = ChatMacros.Instance.ContentsManager.GetTexture("icons/edit_icon.png");
            LoadFonts();
            LoadSounds();
        }

        public void PlayMenuItemClick() {
            _menuItemClickSfx.Play(GameService.GameIntegration.Audio.Volume, 0, 0);
        }

        public void PlayMenuClick() {
            _menuClicks[RandomUtil.GetRandom(0, 3)].Play(GameService.GameIntegration.Audio.Volume, 0, 0);
        }

        private void LoadFonts() {
            this.Menomonia24 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/menomonia.ttf", 24);
            this.RubikRegular26 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/rubik-regular.ttf", 26);
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
            this.Menomonia24?.Dispose();
            this.RubikRegular26?.Dispose();
            this.DragReorderIcon?.Dispose();
            this.EditIcon?.Dispose();
        }

    }
}

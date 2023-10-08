using System;
using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nekres.ChatMacros.Core.Services {
    internal class ResourceService : IDisposable {

        public Color      BrightGold = new (223, 194, 149, 255);
        public Texture2D  DragReorderIcon { get; private set; }
        public BitmapFont Menomonia24     { get; private set; }
        public BitmapFont RubikRegular26  { get; private set; }

        public ResourceService() {
            DragReorderIcon = ChatMacros.Instance.ContentsManager.GetTexture("icons/drag-reorder.png");
            LoadFonts();
        }

        private void LoadFonts() {
            this.Menomonia24 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/menomonia.ttf", 24);
            this.RubikRegular26 = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/rubik-regular.ttf", 26);
        }

        public void Dispose() {
            this.Menomonia24?.Dispose();
            this.RubikRegular26?.Dispose();
            this.DragReorderIcon?.Dispose();
        }

    }
}

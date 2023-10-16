using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Nekres.ChatMacros.Core.UI {
    internal class ContextMenuStripItem<T> : ContextMenuStripItem {

        private readonly AsyncTexture2D _textureBullet = AsyncTexture2D.FromAssetId(155038);
        private readonly Texture2D      _textureArrow  = Content.GetTexture("context-menu-strip-submenu");

        public readonly T Value;

        private Color _fontColor;
        public Color FontColor {
            get => _fontColor;
            set {
                SetProperty(ref _fontColor, value);
                this.Invalidate();
            }
        }

        public ContextMenuStripItem(T value) {
            this.Value = value;
        }

        public ContextMenuStripItem(string text, T value) : base(text) {
            this.Value = value;
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            var color = this.Enabled ? this.MouseOver ? StandardColors.Tinted : StandardColors.Default : StandardColors.DisabledText;
            if (this.CanCheck) {
                string state = this.Checked ? "-checked" : "-unchecked";
                string extension = "";
                extension = this.MouseOver ? "-active" : extension;
                extension = !this.Enabled ? "-disabled" : extension;
                spriteBatch.DrawOnCtrl(this, Checkable.TextureRegionsCheckbox.First(cb => cb.Name == "checkbox/cb" + state + extension), new Rectangle(-1, _size.Y / 2 - 16, 32, 32), StandardColors.Default);
            } else {
                spriteBatch.DrawOnCtrl(this, _textureBullet, new Rectangle(6, _size.Y / 2 - 9, 18, 18), color);
            }

            spriteBatch.DrawStringOnCtrl(this, Text, Content.DefaultFont14, new Rectangle(31, 1, _size.X - 30 - 6, _size.Y), StandardColors.Shadow);
            spriteBatch.DrawStringOnCtrl(this, Text, Content.DefaultFont14, new Rectangle(30, 0, _size.X - 30 - 6, _size.Y), _enabled ? FontColor : StandardColors.DisabledText);
            if (this.Submenu == null) {
                return;
            }

            spriteBatch.DrawOnCtrl(this, _textureArrow, new Rectangle(this._size.X - 6 - _textureArrow.Width, _size.Y / 2 - _textureArrow.Height / 2, _textureArrow.Width, _textureArrow.Height), color);
        }
    }
}

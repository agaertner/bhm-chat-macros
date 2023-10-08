using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nekres.ChatMacros.Core.UI {
    internal class RotatableImage : Image {

        private float _rotation;
        public float Rotation {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            if (Texture == null) {
                return;
            }

            spriteBatch.DrawOnCtrl(this, Texture, bounds, this.SourceRectangle, Tint, Rotation, Vector2.Zero, this.SpriteEffects);
        }
    }
}

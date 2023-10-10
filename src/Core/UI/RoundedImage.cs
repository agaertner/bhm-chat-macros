using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Glide;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Nekres.ChatMacros.Core.UI {
    public class RoundedImage : Control {

        private Effect _curvedBorder;

        private AsyncTexture2D _texture;

        private SpriteBatchParameters _defaultParams;
        private SpriteBatchParameters _curvedBorderParams;

        private float _radius = 0.215f;
        private Tween _tween;

        private Color _color;
        public Color Color {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public RoundedImage(AsyncTexture2D texture) {
            _defaultParams = new();
            _curvedBorder = ChatMacros.Instance.ContentsManager.GetEffect<Effect>(@"effects\curvedborder.mgfx");
            _curvedBorderParams = new() {
                Effect = _curvedBorder
            };
            _texture = texture;

            _curvedBorder.Parameters["Smooth"].SetValue(false); // Disable anti-aliasing
        }

        protected override void DisposeControl() {
            _curvedBorder.Dispose();
            base.DisposeControl();
        }

        protected override void OnMouseEntered(MouseEventArgs e) {
            _tween?.Cancel();
            _tween = Animation.Tweener.Tween(this, new { _radius = 0.315f }, 0.1f);
            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e) {
            _tween?.Cancel();
            _tween = Animation.Tweener.Tween(this, new { _radius = 0.215f }, 0.1f);
            base.OnMouseLeft(e);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            _curvedBorder.Parameters["Radius"].SetValue(_radius);
            _curvedBorder.Parameters["Opacity"].SetValue(this.Opacity);

            spriteBatch.End();
            spriteBatch.Begin(_curvedBorderParams);
            spriteBatch.DrawOnCtrl(this, _texture, new Rectangle(0, 0, this.Width, this.Height), _color);
            spriteBatch.End();
            spriteBatch.Begin(_defaultParams);
        }
    }
}

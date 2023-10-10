using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Glide;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nekres.ChatMacros.Properties;
using System.Diagnostics;

namespace Nekres.ChatMacros.Core.UI {
    internal class KofiButton : View {

        private Color      _backgroundColor = new Color(41, 171, 224);
        private Texture2D  _cupBorder;
        private Texture2D  _background;
        private BitmapFont _font;

        private const int    CUP_SIZE         = 38;
        private const int    BOUNCE_COUNT     = 15;
        private const float  BOUNCE_DURATION  = 2f;
        private const float  SCALE_DURATION   = 0.6f;
        private const float  BOUNCE_ROTATION  = -MathHelper.PiOver4 / 4;
        private       int    _wiggleDirection = 1;
        private       bool   _nonOpp;
        private       bool   _isAnimating;
        private       string _text => RandomUtil.GetRandom(0, 1) > 0 ? Resources.Support_Me_on_Ko_fi : Resources.Buy_Me_a_Coffee_;
        
        public KofiButton() {
            _cupBorder = ChatMacros.Instance.ContentsManager.GetTexture("socials/cup_border.png");
            _background = ChatMacros.Instance.ContentsManager.GetTexture("socials/kofi_background.png");
            _font = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/Quicksand-SemiBold.ttf", 24);
        }

        protected override void Unload() {
            _cupBorder?.Dispose();
            _background?.Dispose();
            _font?.Dispose();
            base.Unload();
        }

        protected override void Build(Container buildPanel) {

            var background = new Image(ContentService.Textures.Pixel) {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width,
                Height = buildPanel.ContentRegion.Height,
                Texture = _background,
                Tint = _backgroundColor
            };

            var cup = new RotatableImage {
                Parent  = buildPanel,
                Width   = CUP_SIZE,
                Height  = CUP_SIZE,
                Left    = Panel.LEFT_PADDING + CUP_SIZE,
                Top     = (buildPanel.ContentRegion.Height - CUP_SIZE) / 2,
                Texture = _cupBorder
            };

            var label = new Label {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width - CUP_SIZE - Panel.LEFT_PADDING * 2,
                Height = buildPanel.ContentRegion.Height,
                Left   = cup.Left,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text   = _text,
                Font   = _font
            };

            Tween bouncer = null;

            void DoWiggle() {
                _isAnimating     = true;
                _nonOpp          = !_nonOpp;
                _wiggleDirection = 1;
                
                bouncer = GameService.Animation.Tweener.Tween(cup, new {
                                          Size = new Point(CUP_SIZE + 5, CUP_SIZE + 5), 
                                          Left = Panel.RIGHT_PADDING + CUP_SIZE - 5, 
                                          Top = (buildPanel.ContentRegion.Height - cup.Height) / 2
                                      }, SCALE_DURATION)
                                     .OnComplete(() => bouncer = GameService.Animation.Tweener.Tween(cup, new { Rotation = BOUNCE_ROTATION * _wiggleDirection }, BOUNCE_DURATION / BOUNCE_COUNT)
                                     .Reflect()
                                     .Repeat(BOUNCE_COUNT)
                                     .Ease(Ease.BounceInOut)
                                     .Rotation(Tween.RotationUnit.Radians)
                                      // ReSharper disable once AssignmentInConditionalExpression
                                     .OnRepeat(() => _wiggleDirection *= (_nonOpp = !_nonOpp) ? -1 : 1).OnComplete(() => bouncer = GameService.Animation.Tweener.Tween(cup, new {
                                          Size = new Point(CUP_SIZE, CUP_SIZE), 
                                          Left = Panel.RIGHT_PADDING + CUP_SIZE, 
                                          Top = (buildPanel.ContentRegion.Height - CUP_SIZE) / 2}, SCALE_DURATION)
                                          .OnComplete(RestartWiggle)));
            }

            void RestartWiggle() {
                Reset();
                DoWiggle();
            }

            void Reset() {
                _isAnimating     = false;
                cup.Rotation     = 0;
                cup.Size         = new Point(CUP_SIZE, CUP_SIZE);
                cup.Left         = Panel.LEFT_PADDING + CUP_SIZE;
                cup.Top          = (buildPanel.ContentRegion.Height - CUP_SIZE) / 2;
                _nonOpp          = !_nonOpp;
                _wiggleDirection = 1;
            }

            buildPanel.MouseEntered += (_, _) => {
                background.Tint = new Color(21, 151, 204);
                if (_isAnimating) {
                    return;
                }
                RestartWiggle();
            };

            buildPanel.MouseLeft += (_, _) => {
                bouncer?.Cancel();
                Reset();
                background.Tint  = new Color(41, 171, 224);
            };

            buildPanel.Click += (_,_) => Process.Start("https://ko-fi.com/nekres");
            buildPanel.BasicTooltipText = "ko-fi.com/nekres";
            base.Build(buildPanel);
        }
    }
}

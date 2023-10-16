using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;
using System;

namespace Nekres.ChatMacros.Core {
    internal static class ContentsManagerExtensions {

        internal static CharacterRange GeneralPunctuation = new('\u2000', '\u206F');
        internal static CharacterRange Arrows = new('\u2190', '\u21FF');
        internal static CharacterRange MathematicalOperators = new('\u2200', '\u22FF');
        internal static CharacterRange BoxDrawing = new('\u2500', '\u2570');
        internal static CharacterRange GeometricShapes = new('\u25A0', '\u25FF');
        internal static CharacterRange MiscellaneousSymbols = new('\u2600', '\u26FF');

        public static readonly CharacterRange[] Gw2CharacterRange = {
            CharacterRange.BasicLatin,
            CharacterRange.Latin1Supplement,
            CharacterRange.LatinExtendedA,
            GeneralPunctuation,
            Arrows,
            MathematicalOperators,
            BoxDrawing,
            GeometricShapes,
            MiscellaneousSymbols
        };

        /// <summary>
        /// Loads a <see cref="SpriteFont"/> from a TrueTypeFont (*.ttf) file.
        /// </summary>
        /// <param name="manager">Module's <see cref="ContentsManager"/>.</param>
        /// <param name="fontPath">The path to the TTF font file.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="textureSize">Size of the <see cref="SpriteFont.Texture"/>.<br/>A greater <c>fontSize</c> results in bigger glyphs which may require more texture space.</param>
        public static SpriteFont GetSpriteFont(this ContentsManager manager, string fontPath, int fontSize, int textureSize = 1392) {
            if (fontSize <= 0) {
                throw new ArgumentException("Font size must be greater than 0.", nameof(fontSize));
            }

            using var fontStream = manager.GetFileStream(fontPath);
            var fontData = new byte[fontStream.Length];
            var fontDataLength = fontStream.Read(fontData, 0, fontData.Length);

            if (fontDataLength > 0) {
                using var ctx        = GameService.Graphics.LendGraphicsDeviceContext();
                var       bakeResult = TtfFontBaker.Bake(fontData, fontSize, textureSize, textureSize, Gw2CharacterRange);
                return bakeResult.CreateSpriteFont(ctx.GraphicsDevice);
            }

            return null;
        }

        /// <summary>
        /// Loads a <see cref="BitmapFont"/> from a TrueTypeFont (*.ttf) file.
        /// </summary>
        /// <param name="manager">Module's <see cref="ContentsManager"/>.</param>
        /// <param name="fontPath">The path to the TTF font file.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="lineHeight">Sets the line height. By default, <see cref="SpriteFont.LineSpacing"/> will be used.</param>
        public static BitmapFont GetBitmapFont(this ContentsManager manager, string fontPath, int fontSize, int lineHeight = 0) {
            return manager.GetSpriteFont(fontPath, fontSize)?.ToBitmapFont(lineHeight);
        }
    }
}

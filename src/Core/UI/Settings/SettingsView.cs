using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Nekres.ChatMacros.Core.UI.Credits;

namespace Nekres.ChatMacros.Core.UI.Settings {
    internal class SettingsView : View {

        protected override void Build(Container buildPanel) {

            var voiceRecognitionSettings = new ViewContainer {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width  / 2,
                Height = buildPanel.ContentRegion.Height / 2
            };
            voiceRecognitionSettings.Show(new VoiceRecognitionSettings(ChatMacros.Instance.InputConfig.Value));

            var controlSettings = new ViewContainer {
                Parent = buildPanel,
                Top = voiceRecognitionSettings.Bottom,
                Width  = buildPanel.ContentRegion.Width  / 2,
                Height = buildPanel.ContentRegion.Height / 2
            };
            controlSettings.Show(new ControlSettings(ChatMacros.Instance.ControlsConfig.Value));

            var donors = new ViewContainer {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width / 2,
                Height = buildPanel.ContentRegion.Height,
                Left   = voiceRecognitionSettings.Right
            };
            donors.Show(new CreditsView());
            base.Build(buildPanel);
        }
    }
}

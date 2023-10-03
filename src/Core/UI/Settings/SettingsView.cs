using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Nekres.ChatMacros.Core.UI.Configs;
using System;
using System.Linq;
using Blish_HUD;
using Microsoft.Xna.Framework;

namespace Nekres.ChatMacros.Core.UI.Settings {
    internal class SettingsView : View {

        private InputConfig _config;

        public SettingsView(InputConfig config) {
            _config = config;
        }

        protected override void Build(Container buildPanel) {

            var flowBody = new FlowPanel {
                Parent              = buildPanel,
                Width               = buildPanel.ContentRegion.Width,
                Height              = buildPanel.ContentRegion.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding      = new Vector2(5, 5),
                CanCollapse         = true,
                Title               = "Voice Recognition"
            };

            var inputDevice = new KeyValueDropdown<Guid> {
                Parent          = flowBody,
                PlaceholderText = "Select an input device…",
                SelectedItem = _config.InputDevice
            };

            foreach (var device in ChatMacros.Instance.Speech.InputDevices) {
                inputDevice.AddItem(device.ProductNameGuid, device.ProductName);
            }

            var voiceLanguage = new KeyValueDropdown<VoiceLanguage> {
                Parent          = flowBody,
                PlaceholderText = "Select a voice language…",
                SelectedItem    = _config.VoiceLanguage
            };

            foreach (var lang in Enum.GetValues(typeof(VoiceLanguage)).Cast<VoiceLanguage>()) {
                voiceLanguage.AddItem(lang, lang.ToString());
            }

            var pttKeybinding = new KeybindingAssigner(_config.PushToTalk) {
                Parent           = flowBody,
                KeyBindingName   = "Push to Talk",
                BasicTooltipText = "Hold to recognize voice commands and release to trigger an action."
            };

            inputDevice.ValueChanged   += OnInputDeviceChanged;
            voiceLanguage.ValueChanged += OnVoiceLanguageChanged;

            base.Build(buildPanel);
        }

        private void OnVoiceLanguageChanged(object sender, ValueChangedEventArgs<VoiceLanguage> e) {
            _config.VoiceLanguage = e.NewValue;
        }

        private void OnInputDeviceChanged(object o, ValueChangedEventArgs<Guid> e) {
            _config.InputDevice = e.NewValue;
        }
    }
}

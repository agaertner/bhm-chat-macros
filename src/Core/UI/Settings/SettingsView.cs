using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Nekres.ChatMacros.Core.UI.Configs;
using System;
using System.Linq;
using Blish_HUD;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;

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
                Title               = Resources.Voice_Recognition
            };

            var inputDevice = new KeyValueDropdown<Guid> {
                Parent          = flowBody,
                PlaceholderText = Resources.Select_an_input_device___,
                SelectedItem = _config.InputDevice,
                BasicTooltipText = Resources.Select_an_input_device___
            };

            foreach (var device in ChatMacros.Instance.Speech.InputDevices) {
                inputDevice.AddItem(device.ProductNameGuid, device.ProductName);
            }

            var voiceLanguage = new KeyValueDropdown<VoiceLanguage> {
                Parent          = flowBody,
                PlaceholderText = Resources.Select_a_command_language___,
                SelectedItem    = _config.VoiceLanguage,
                BasicTooltipText = Resources.Select_a_command_language___
            };

            foreach (var lang in Enum.GetValues(typeof(VoiceLanguage)).Cast<VoiceLanguage>()) {
                voiceLanguage.AddItem(lang, lang.ToString());
            }

            var pttKeybinding = new KeybindingAssigner(_config.PushToTalk) {
                Parent           = flowBody,
                KeyBindingName   = Resources.Push_to_Talk,
                BasicTooltipText = $"{Resources.Hold_to_recognize_voice_commands_}\n{Resources.Release_to_trigger_an_action_}"
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

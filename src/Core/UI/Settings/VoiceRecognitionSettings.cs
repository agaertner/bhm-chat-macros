using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.Services;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Properties;
using System;
using System.Diagnostics;
using System.Linq;
using Blish_HUD.Extended;

namespace Nekres.ChatMacros.Core.UI.Settings {
    internal class VoiceRecognitionSettings : View {

        private InputConfig _config;

        public VoiceRecognitionSettings(InputConfig config) {
            _config = config;
        }

        protected override void Build(Container buildPanel) {

            var voiceRecognitionPanel = new FlowPanel {
                Parent              = buildPanel,
                Width               = buildPanel.ContentRegion.Width,
                Height              = buildPanel.ContentRegion.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding      = new Vector2(5, 5),
                CanCollapse         = true,
                Title               = Resources.Voice_Recognition
            };

            var inputDevice = new TextDropdown<Guid> {
                Parent          = voiceRecognitionPanel,
                PlaceholderText = Resources.Select_an_input_device___,
                SelectedItem = _config.InputDevice,
                BasicTooltipText = Resources.Select_an_input_device___,
                Width = 300,
            };

            foreach (var device in ChatMacros.Instance.Speech.InputDevices) {
                inputDevice.AddItem(device.ProductNameGuid, device.ProductName);
            }

            var voiceLanguage = new TextDropdown<VoiceLanguage> {
                Parent           = voiceRecognitionPanel,
                PlaceholderText  = Resources.Select_your_primary_command_language___,
                SelectedItem     = _config.VoiceLanguage,
                BasicTooltipText = Resources.Select_your_primary_command_language___,
                Width = 300,
            };

            var secondaryVoiceLanguage = new TextDropdown<VoiceLanguage> {
                Parent           = voiceRecognitionPanel,
                PlaceholderText  = Resources.Select_a_secondary_command_language___,
                SelectedItem     = _config.SecondaryVoiceLanguage,
                BasicTooltipText = Resources.Select_a_secondary_command_language___,
                Width = 300,
            };

            foreach (var lang in Enum.GetValues(typeof(VoiceLanguage)).Cast<VoiceLanguage>()) {
                voiceLanguage.AddItem(lang, lang.ToDisplayString());
                secondaryVoiceLanguage.AddItem(lang, lang.ToDisplayString());
            }

            var pttKeybinding = new KeybindingAssigner(_config.PushToTalk) {
                Parent           = voiceRecognitionPanel,
                KeyBindingName   = Resources.Push_to_Talk,
                BasicTooltipText = $"{Resources.Hold_to_recognize_voice_commands_}\n{Resources.Release_to_trigger_an_action_}"
            };

            inputDevice.SelectedItemChanged += OnInputDeviceChanged;
            voiceLanguage.SelectedItemChanged += OnVoiceLanguageChanged;
            secondaryVoiceLanguage.SelectedItemChanged += OnSecondaryVoiceLanguageChanged;
            base.Build(buildPanel);
        }

        private void OnVoiceLanguageChanged(object sender, ValueChangedEventArgs<VoiceLanguage> e) {
            if (IsInstalled(e.NewValue)) {
                _config.VoiceLanguage = e.NewValue;
            }
        }
        private void OnSecondaryVoiceLanguageChanged(object sender, ValueChangedEventArgs<VoiceLanguage> e) {
            if (IsInstalled(e.NewValue)) {
                _config.SecondaryVoiceLanguage = e.NewValue;
            }
        }

        private bool IsInstalled(VoiceLanguage lang) {
            var culture = lang.Culture();
            if (!WindowsSpeech.TestVoiceLanguage(culture)) {
                GameService.Content.PlaySoundEffectByName("error");
                ScreenNotification.ShowNotification(string.Format(Resources.Speech_recognition_for__0__is_not_installed_, $"'{culture.DisplayName}'"), ScreenNotification.NotificationType.Error);
                Process.Start("ms-settings:speech");
                return false;
            }
            return true;
        }

        private void OnInputDeviceChanged(object o, ValueChangedEventArgs<Guid> e) {
            _config.InputDevice = e.NewValue;
        }
    }
}

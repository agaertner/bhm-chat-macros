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
using Nekres.ChatMacros.Core.UI.Credits;

namespace Nekres.ChatMacros.Core.UI.Settings {
    internal class SettingsView : View {

        private InputConfig _config;

        public SettingsView(InputConfig config) {
            _config = config;
        }

        protected override void Build(Container buildPanel) {

            var flowBody = new FlowPanel {
                Parent              = buildPanel,
                Width               = buildPanel.ContentRegion.Width / 2,
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
                Parent           = flowBody,
                PlaceholderText  = Resources.Select_your_primary_command_language___,
                SelectedItem     = _config.VoiceLanguage,
                BasicTooltipText = Resources.Select_your_primary_command_language___
            };

            var secondaryVoiceLanguage = new KeyValueDropdown<VoiceLanguage> {
                Parent           = flowBody,
                PlaceholderText  = Resources.Select_a_secondary_command_language___,
                SelectedItem     = _config.SecondaryVoiceLanguage,
                BasicTooltipText = Resources.Select_a_secondary_command_language___
            };

            foreach (var lang in Enum.GetValues(typeof(VoiceLanguage)).Cast<VoiceLanguage>()) {
                voiceLanguage.AddItem(lang, lang.ToDisplayString());
                secondaryVoiceLanguage.AddItem(lang, lang.ToDisplayString());
            }

            var pttKeybinding = new KeybindingAssigner(_config.PushToTalk) {
                Parent           = flowBody,
                KeyBindingName   = Resources.Push_to_Talk,
                BasicTooltipText = $"{Resources.Hold_to_recognize_voice_commands_}\n{Resources.Release_to_trigger_an_action_}"
            };

            inputDevice.ValueChanged   += OnInputDeviceChanged;
            voiceLanguage.ValueChanged += OnVoiceLanguageChanged;
            secondaryVoiceLanguage.ValueChanged += OnSecondaryVoiceLanguageChanged;

            var donors = new ViewContainer {
                Parent = buildPanel,
                Width  = buildPanel.ContentRegion.Width / 2,
                Height = buildPanel.ContentRegion.Height,
                Left = flowBody.Right
            };
            donors.Show(new CreditsView());

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

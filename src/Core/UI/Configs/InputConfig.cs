using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Globalization;
using Blish_HUD;
using Gw2Sharp.WebApi;

namespace Nekres.ChatMacros.Core.UI.Configs {
    public enum VoiceLanguage {
        English,
        German,
        French,
        Spanish
    }

    public static class VoiceLanguageExtensions {
        public static CultureInfo Culture(this VoiceLanguage lang) {
            return lang switch {
                VoiceLanguage.English => CultureInfo.GetCultureInfo(9),
                VoiceLanguage.German  => CultureInfo.GetCultureInfo(7),
                VoiceLanguage.French  => CultureInfo.GetCultureInfo(12),
                VoiceLanguage.Spanish => CultureInfo.GetCultureInfo(10),
                _                     => throw new NotImplementedException()
            };
        }
    }

    internal class InputConfig : ConfigBase {

        public static InputConfig Default => new() {
            _inputDevice = Guid.Empty,
            _pushToTalk  = new KeyBinding(Keys.Y)
        };

        private Guid _inputDevice;
        [JsonProperty("input_device")]
        public Guid InputDevice {
            get => _inputDevice;
            set {
                _inputDevice = value;
                this.SaveConfig(ChatMacros.Instance.InputConfig);
            }
        }

        private VoiceLanguage _voiceLang;
        [JsonProperty("voice_lang")]
        public VoiceLanguage VoiceLanguage {
            get => _voiceLang;
            set {
                _voiceLang = value;
                this.SaveConfig(ChatMacros.Instance.InputConfig);
            }
        }

        private KeyBinding _pushToTalk;
        [JsonProperty("push_to_talk")]
        public KeyBinding PushToTalk {
            get => _pushToTalk;
            set => _pushToTalk = ResetDelegates(_pushToTalk, value);
        }

        private KeyBinding ResetDelegates(KeyBinding oldBinding, KeyBinding newBinding) {
            if (oldBinding != null) {
                oldBinding.BindingChanged -= OnPushToTalkChanged;
            }
            newBinding                ??= new KeyBinding();
            newBinding.BindingChanged +=  OnPushToTalkChanged;
            newBinding.Enabled        =   true;
            return newBinding;
        }

        private void OnPushToTalkChanged(object sender, EventArgs e) {
            this.SaveConfig(ChatMacros.Instance.InputConfig);
        }
    }
}

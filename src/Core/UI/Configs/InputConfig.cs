using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using Nekres.ChatMacros.Properties;
using Newtonsoft.Json;
using System;
using System.Globalization;

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
                _                     => default
            };
        }

        public static string ToDisplayString(this VoiceLanguage lang) {
            return lang switch {
                VoiceLanguage.English => Resources.English,
                VoiceLanguage.German  => Resources.German,
                VoiceLanguage.French  => Resources.French,
                VoiceLanguage.Spanish => Resources.Spanish,
                _                     => string.Empty
            };
        }
    }

    internal class InputConfig : ConfigBase {

        public static InputConfig Default = new() {
            _inputDevice        = Guid.Empty,
            _voiceLang          = VoiceLanguage.English,
            _secondaryVoiceLang = VoiceLanguage.English,
            PushToTalk          = new KeyBinding(Keys.LeftAlt)
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

        private VoiceLanguage _secondaryVoiceLang;
        [JsonProperty("secondary_voice_lang")]
        public VoiceLanguage SecondaryVoiceLanguage {
            get => _secondaryVoiceLang;
            set {
                _secondaryVoiceLang = value;
                this.SaveConfig(ChatMacros.Instance.InputConfig);
            }
        }

        private KeyBinding _pushToTalk;
        [JsonProperty("push_to_talk")]
        public KeyBinding PushToTalk {
            get => _pushToTalk;
            set => _pushToTalk = ResetDelegates(_pushToTalk, value);
        }

        protected override void BindingChanged() {
            SaveConfig(ChatMacros.Instance.InputConfig);
        }
    }
}

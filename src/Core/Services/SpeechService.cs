using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NAudio.Wave;
using Nekres.ChatMacros.Core.Services.Data;
using Nekres.ChatMacros.Core.Services.Speech;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekres.ChatMacros.Core.Services {
    internal class SpeechService : IDisposable {

        public event EventHandler<ValueEventArgs<bool>> InputDetected; 

        public event EventHandler<ValueEventArgs<Stream>> SecondaryVoiceStreamChanged;
        public event EventHandler<ValueEventArgs<Stream>> VoiceStreamChanged;
        public event EventHandler<EventArgs>              StartRecording;
        public event EventHandler<EventArgs>              StopRecording;

        public const int SAMPLE_RATE = 16000;
        public const int CHANNELS    = 1;
        public const int PARTIAL_RESULT_EXPIRE_MS = 1000;

        private Stream _audioStream;
        private Stream _secondaryAudioStream;

        private WaveInEvent _audioSource;

        private int _deviceNumber;

        private bool _isRecording;

        private ISpeechRecognitionProvider _recognizer;

        private SpeechRecognizerDisplay _display;

        private DateTime _lastSpeechDetected;
        private DateTime _partialResultExpiresAt;

        public SpeechService() {
            _recognizer   = new WindowsSpeech(this);

            StartRecognizer();

            ChatMacros.Instance.InputConfig.SettingChanged += OnInputConfigChanged;

            _recognizer.PartialResult  += OnPartialResultReceived;
            _recognizer.FinalResult    += OnFinalResultReceived;
            _recognizer.SpeechDetected += OnSpeechDetected;
        }

        public void UpdateGrammar() {
            ChatMacros.Instance.Macro.UpdateMacros();
            _recognizer.ChangeGrammar(false, BaseMacro.GetCommands(ChatMacros.Instance.Macro.ActiveMacros));
        }

        private void StartRecognizer() {
            _display?.Dispose();
            _display = new SpeechRecognizerDisplay(this) {
                Visible = false
            };

            _recognizer.Reset(ChatMacros.Instance.InputConfig.Value.VoiceLanguage.Culture(), 
                              ChatMacros.Instance.InputConfig.Value.SecondaryVoiceLanguage.Culture(),
                              false,
                              BaseMacro.GetCommands(ChatMacros.Instance.Macro.ActiveMacros));

            ChangeDevice(ChatMacros.Instance.InputConfig.Value.InputDevice);
        }

        private void OnSpeechDetected(object sender, EventArgs e) {
            if (DateTime.UtcNow.Subtract(_lastSpeechDetected).TotalSeconds > 2) {
                _lastSpeechDetected = DateTime.UtcNow;
                InputDetected?.Invoke(this, new ValueEventArgs<bool>(true));
            }
        }

        private void OnPartialResultReceived(object sender, ValueEventArgs<string> e) {
            _display.Text           = e.Value;
            _partialResultExpiresAt = DateTime.UtcNow.AddMilliseconds(PARTIAL_RESULT_EXPIRE_MS);
            _display.TextExpiresAt  = _partialResultExpiresAt;
        }

        private async void OnFinalResultReceived(object sender, ValueEventArgs<string> e) {
            var macro = FastenshteinUtil.FindClosestMatchBy(e.Value, ChatMacros.Instance.Macro.ActiveMacros, m => m.VoiceCommands);

            if (macro != null) {
                await macro.Fire();
            }
        }

        private void OnInputConfigChanged(object sender, ValueChangedEventArgs<InputConfig> e) {
            if (e.NewValue == null) {
                return;
            }

            StartRecognizer();
        }

        public void Update(GameTime gameTime) {
            if (ChatMacros.Instance.InputConfig.Value.PushToTalk != null) {
                if (ChatMacros.Instance.InputConfig.Value.PushToTalk.IsTriggering) {
                    Start();
                } else {
                    Stop();
                }
            }

            if (DateTime.UtcNow.Subtract(_lastSpeechDetected).TotalSeconds > 30) {
                _lastSpeechDetected = DateTime.UtcNow;
                InputDetected?.Invoke(this, new ValueEventArgs<bool>(false));
            }

            if (DateTime.UtcNow > _partialResultExpiresAt) {
                _display.Text = string.Empty;
            }
        }

        public IEnumerable<(int DeviceNumber, string ProductName, int Channels, Guid DeviceNameGuid, Guid ProductNameGuid, Guid ManufacturerGuid)> InputDevices {
            get {
                for (int deviceNumber = 0; deviceNumber < WaveInEvent.DeviceCount; ++deviceNumber) {
                    var device = WaveInEvent.GetCapabilities(deviceNumber);
                    yield return (deviceNumber, device.ProductName, device.Channels, device.NameGuid, device.ProductGuid, device.ManufacturerGuid);
                }
            }
        }

        private void ChangeDevice(Guid productNameGuid) {
            _deviceNumber = InputDevices.FirstOrDefault(device => device.ProductNameGuid.Equals(productNameGuid)).DeviceNumber;

            _audioSource?.Dispose(); // Stop and dispose the old device.
            _audioSource = new WaveInEvent {
                DeviceNumber = _deviceNumber,
                WaveFormat   = new WaveFormat(SAMPLE_RATE, CHANNELS)
            };

            _audioStream?.Dispose();
            _audioStream = new SpeechStream(4096 * 2);

            _secondaryAudioStream?.Dispose();
            _secondaryAudioStream = new SpeechStream(4096 * 2);

            InputDetected?.Invoke(this, new ValueEventArgs<bool>(false));

            _audioSource.DataAvailable += OnDataAvailable;
            _audioSource.StartRecording();

            this.VoiceStreamChanged?.Invoke(this, new ValueEventArgs<Stream>(_audioStream));
            this.SecondaryVoiceStreamChanged?.Invoke(this, new ValueEventArgs<Stream>(_secondaryAudioStream));
        }

        public void Stop() {
            if (!_isRecording) {
                return;
            }
            _isRecording = false;
            _display.Hide();
            StopRecording?.Invoke(this, EventArgs.Empty);
        }

        public void Start() {
            if (_isRecording) {
                return;
            }
            _isRecording = true;
            _display.Show();
            StartRecording?.Invoke(this, EventArgs.Empty);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e) {
            _audioStream.Write(e.Buffer, 0, e.BytesRecorded);
            _secondaryAudioStream.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public void Dispose() {
            _recognizer.PartialResult  -= OnPartialResultReceived;
            _recognizer.FinalResult    -= OnFinalResultReceived;
            _recognizer.SpeechDetected -= OnSpeechDetected;
            _recognizer?.Dispose();
            _audioSource?.Dispose();
            _audioStream?.Dispose();
            _secondaryAudioStream?.Dispose();
        }

        private sealed class SpeechRecognizerDisplay : Control {

            private string _text = string.Empty;
            public string Text {
                get => _text;
                set => SetProperty(ref _text, value);
            }

            private DateTime _textExpiresAt;
            public DateTime TextExpiresAt {
                get => _textExpiresAt;
                set => SetProperty(ref _textExpiresAt, value);
            }

            private readonly BitmapFont _font;

            private DateTime _lastTextCursorBlink;
            private DateTime _lastEllipsisBlink;

            private string ListeningText => Resources.Listening;
            private string NoInput       => Resources.No_input_is_being_detected__Verify_your_settings_;

            private bool          _inputDetected;
            private Color         _redShift;
            private SpeechService _speech;

            public SpeechRecognizerDisplay(SpeechService speech) {
                _speech   = speech;
                _redShift = new Color(255, 57, 57);
                _font     = ChatMacros.Instance.ContentsManager.GetBitmapFont("fonts/Lato-Regular.ttf", 60);
                Parent    = GameService.Graphics.SpriteScreen;
                Size      = GameService.Graphics.SpriteScreen.ContentRegion.Size;

                ZIndex =  2147483600;

                _speech.InputDetected += OnInputDetected;
                Parent.ContentResized += OnParentResized;
            }

            private void OnInputDetected(object sender, ValueEventArgs<bool> e) {
                _inputDetected = e.Value;
            }

            protected override void OnShown(EventArgs e) {
                _text = string.Empty;
                base.OnShown(e);
            }

            private void OnParentResized(object sender, RegionChangedEventArgs e) {
                Size = e.CurrentRegion.Size;
            }

            protected override void DisposeControl() {
                if (Parent != null) {
                    Parent.ContentResized -= OnParentResized;
                }
                _speech.InputDetected -= OnInputDetected;
                _font?.Dispose();
                base.DisposeControl();
            }

            protected override CaptureType CapturesInput() {
                return CaptureType.Filter;
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
                var listenSize   = _font.MeasureString(this.ListeningText);
                var listenBounds = new Rectangle(bounds.X, bounds.Y - (int)Math.Round(listenSize.Height), bounds.Width, bounds.Height);
                spriteBatch.DrawStringOnCtrl(this, this.ListeningText, _font, listenBounds, Color.White, false, true, 2, HorizontalAlignment.Center);
                DrawEllipsisCursor(spriteBatch, listenBounds, this.ListeningText, _font, ref _lastEllipsisBlink, Color.White, true, 2, HorizontalAlignment.Center);

                if (!_inputDetected) {
                    var inputDetectedSize   = _font.MeasureString(this.NoInput);
                    var inputDetectedBounds = new Rectangle(bounds.X, listenBounds.Y - (int)Math.Round(inputDetectedSize.Height), bounds.Width, bounds.Height);
                    spriteBatch.DrawStringOnCtrl(this, this.NoInput, _font, inputDetectedBounds, Color.White, false, true, 2, HorizontalAlignment.Center);
                }
                
                if (string.IsNullOrWhiteSpace(_text)) {
                    return;
                }

                spriteBatch.DrawStringOnCtrl(this, _text, _font, bounds, Color.White, false, true, 2, HorizontalAlignment.Center);

                DrawTextCursor(spriteBatch, _text, _font, bounds, ref _lastTextCursorBlink, Color.White, true, 2, HorizontalAlignment.Center);

                if (DateTime.UtcNow < _textExpiresAt) {
                    var expiresIn    = DateTime.UtcNow.Subtract(_textExpiresAt);
                    var expireText   = expiresIn.ToString(expiresIn.TotalSeconds > -1 ? @"\.ff" : expiresIn.TotalMinutes > -1 ? @"ss\.ff" : @"m\:ss").TrimStart('0');
                    var expireColor  = Color.Lerp(Color.White, _redShift, 1 + (float)expiresIn.TotalMilliseconds / PARTIAL_RESULT_EXPIRE_MS);
                    var textSize     = _font.MeasureString(_text);
                    var expireBounds = new Rectangle(bounds.X + (int)Math.Round(textSize.Width) + Panel.RIGHT_PADDING, bounds.Y, bounds.Width, bounds.Height);
                    spriteBatch.DrawStringOnCtrl(this, expireText, _font, expireBounds, expireColor, false, true, 2, HorizontalAlignment.Center);
                }
            }
            
            private void DrawTextCursor(SpriteBatch         spriteBatch, string text, BitmapFont font, Rectangle bounds, ref DateTime lastTextCursorBlink, Color color,
                                        bool                stroke              = false,
                                        int                 strokeDistance      = 1,
                                        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, 
                                        VerticalAlignment   verticalAlignment   = VerticalAlignment.Middle, 
                                        string              cursor              = "_", int intervalMs = 250) {
                double elapsedMilliseconds = DateTime.UtcNow.Subtract(lastTextCursorBlink).TotalMilliseconds;

                if (elapsedMilliseconds < intervalMs) {
                    return;
                }

                if (elapsedMilliseconds >= intervalMs * 2) {
                    lastTextCursorBlink = DateTime.UtcNow;
                }

                var textSize = font.MeasureString(text);

                var left = horizontalAlignment switch {
                    HorizontalAlignment.Left   => bounds.Left,
                    HorizontalAlignment.Center => bounds.X + (bounds.Width  - (int)Math.Round(textSize.Width))  / 2,
                    HorizontalAlignment.Right  => bounds.Right - (int)Math.Round(textSize.Width),
                    _                          => bounds.Left
                };

                var top = verticalAlignment switch {
                    VerticalAlignment.Top    => bounds.Top,
                    VerticalAlignment.Middle => bounds.Y      + (bounds.Height - (int)Math.Round(textSize.Height)) / 2,
                    VerticalAlignment.Bottom => bounds.Bottom - (int)Math.Round(textSize.Height),
                    _                        => bounds.Top
                };
                
                var textBounds = new Rectangle(left, top, (int)Math.Round(textSize.Width), (int)Math.Round(textSize.Height));
                
                var cursorSize   = font.MeasureString(cursor);
                var cursorBounds = new Rectangle(textBounds.Right, textBounds.Y, (int)Math.Round(cursorSize.Width), (int)Math.Round(cursorSize.Height));
                spriteBatch.DrawStringOnCtrl(this, cursor, font, cursorBounds, color, false, stroke, strokeDistance);
            }

            private void DrawEllipsisCursor(SpriteBatch         spriteBatch, Rectangle bounds, string text, BitmapFont font, ref DateTime lastTextCursorBlink,
                                            Color               color,
                                            bool                stroke              = false, 
                                            int                 strokeDistance      = 1,
                                            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left,
                                            VerticalAlignment   verticalAlignment   = VerticalAlignment.Middle) {

                double elapsedMilliseconds = DateTime.UtcNow.Subtract(lastTextCursorBlink).TotalMilliseconds;

                var cycleDuration = 500;

                var count = (int)(elapsedMilliseconds % cycleDuration / (cycleDuration / 3f)) + 1;

                var ellipsis = new string('.', count > 3 ? 3 : count);

                DrawTextCursor(spriteBatch, text, font, bounds, ref lastTextCursorBlink, color, stroke, strokeDistance, horizontalAlignment, verticalAlignment, ellipsis, 500);
            }
        }
    }
}

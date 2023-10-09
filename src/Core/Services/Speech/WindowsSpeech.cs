using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Nekres.ChatMacros.Core.Services.Speech;
using Nekres.ChatMacros.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;

namespace Nekres.ChatMacros.Core.Services {
    internal class WindowsSpeech : ISpeechRecognitionProvider {

        public event EventHandler<EventArgs>              SpeechDetected; 
        public event EventHandler<ValueEventArgs<string>> FinalResult;
        public event EventHandler<ValueEventArgs<string>> PartialResult;
        
        private SpeechRecognitionEngine _recognizer;
        private SpeechRecognitionEngine _secondaryLanguageRecognizer;

        private CultureInfo _voiceCulture;
        private CultureInfo _secondaryVoiceCulture;
        private Grammar     _grammar;

        private bool               _isListening;
        private int                _processing;
        private (float, string)    _lastResult;
        private AudioSignalProblem _lastAudioSignalProblem;

        private readonly SpeechService _speech;

        public WindowsSpeech(SpeechService speech) {
            _speech =  speech;

            GameService.Input.Mouse.LeftMouseButtonPressed += OnLeftMouseButtonPressed;
        }

        private void OnVoiceStreamChanged(object sender, ValueEventArgs<Stream> e) {
            ChangeDevice(_recognizer, e.Value);
        }

        private void OnSecondaryVoiceStreamChanged(object sender, ValueEventArgs<Stream> e) {
            ChangeDevice(_secondaryLanguageRecognizer, e.Value);
        }

        private void OnLeftMouseButtonPressed(object sender, MouseEventArgs e) {
            _lastResult = default;
            _lastAudioSignalProblem = AudioSignalProblem.None;
            PartialResult?.Invoke(this, new ValueEventArgs<string>(string.Empty));
        }

        private void ChangeDevice(SpeechRecognitionEngine recognizer, Stream stream) {
            recognizer.RecognizeAsyncCancel();
            recognizer.SetInputToAudioStream(stream, new SpeechAudioFormatInfo(SpeechService.SAMPLE_RATE, AudioBitsPerSample.Sixteen, (AudioChannel)SpeechService.CHANNELS));
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Reset(CultureInfo lang, CultureInfo secondaryLanguage, bool freeDictation, params string[] grammar) {
            _speech.StartRecording              -= OnStartRecording;
            _speech.StopRecording               -= OnStopRecording;
            _speech.VoiceStreamChanged          -= OnVoiceStreamChanged;
            _speech.SecondaryVoiceStreamChanged -= OnSecondaryVoiceStreamChanged;

            ChangeModel(lang, secondaryLanguage);

            if (!Refresh()) {
                return;
            }

            ChangeGrammar(freeDictation, grammar);

            RegisterDelegates(_recognizer);
            RegisterDelegates(_secondaryLanguageRecognizer);

            _speech.StartRecording              += OnStartRecording;
            _speech.StopRecording               += OnStopRecording;
            _speech.VoiceStreamChanged          += OnVoiceStreamChanged;
            _speech.SecondaryVoiceStreamChanged += OnSecondaryVoiceStreamChanged;
        }

        private void RegisterDelegates(SpeechRecognitionEngine recognizer) {
            if (recognizer == null) {
                return;
            }
            recognizer.SpeechHypothesized         += OnSpeechRecorded;
            recognizer.SpeechRecognized           += OnSpeechRecorded;
            recognizer.SpeechRecognitionRejected  += OnSpeechRecorded;
            recognizer.AudioSignalProblemOccurred += OnAudioSignalProblemOccurred;
            recognizer.SpeechDetected             += OnSpeechDetected;
        }

        private void OnSpeechDetected(object sender, SpeechDetectedEventArgs e) {
            SpeechDetected?.Invoke(this, EventArgs.Empty);
        }

        private bool Refresh() {
            _recognizer?.Dispose();
            _recognizer = null;
            _secondaryLanguageRecognizer?.Dispose();
            _secondaryLanguageRecognizer = null;

            var recognizers = 0;
            try {
                _recognizer = new SpeechRecognitionEngine(_voiceCulture);
                _recognizer.MaxAlternates = 1;
                ++recognizers;
            } catch (Exception e) {
                ScreenNotification.ShowNotification(string.Format(Resources.Speech_recognition_for__0__is_not_installed_, $"'{_voiceCulture.DisplayName}'"), ScreenNotification.NotificationType.Error);
                GameService.Content.PlaySoundEffectByName("error");
                ChatMacros.Logger.Warn(e, $"Speech recognition for '{_voiceCulture.EnglishName}' is not installed on the system.");
            }

            if (_voiceCulture.Equals(_secondaryVoiceCulture)) {
                return recognizers > 0;
            }

            try {
                _secondaryLanguageRecognizer = new SpeechRecognitionEngine(_secondaryVoiceCulture);
                _secondaryLanguageRecognizer.MaxAlternates = 1;
                ++recognizers;
            } catch (Exception e) {
                ScreenNotification.ShowNotification(string.Format(Resources.Speech_recognition_for__0__is_not_installed_, $"'{_secondaryVoiceCulture.DisplayName}'"), ScreenNotification.NotificationType.Error);
                GameService.Content.PlaySoundEffectByName("error");
                ChatMacros.Logger.Warn(e, $"Speech recognition for '{_secondaryVoiceCulture.EnglishName}' is not installed on the system.");
            }
            return recognizers > 0;
        }

        public static bool TestVoiceLanguage(CultureInfo culture) {
            try {
                using var tempRecognizer = new SpeechRecognitionEngine(culture);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        private void ChangeModel(CultureInfo lang, CultureInfo secondaryLang) {
            _voiceCulture          = lang;
            _secondaryVoiceCulture = secondaryLang;
        }

        private void ChangeGrammar(SpeechRecognitionEngine recognizer, bool freeDictation, params string[] grammar) {
            if (recognizer == null) {
                return;
            }
            recognizer.UnloadAllGrammars();
            if (grammar == null || grammar.Length == 0 || freeDictation) {
                _grammar = new DictationGrammar();
            } else {
                _grammar = new Grammar(new GrammarBuilder(new Choices(grammar)) {
                    Culture = recognizer.RecognizerInfo.Culture
                }) {
                    Name = Guid.NewGuid().ToString("n"),
                };
            }
            recognizer.LoadGrammar(_grammar);
        }

        public void ChangeGrammar(bool freeDictation, params string[] grammar) {
            ChangeGrammar(_recognizer, freeDictation, grammar);
            ChangeGrammar(_secondaryLanguageRecognizer, freeDictation, grammar);
        }

        private void OnSpeechRecorded(object sender, RecognitionEventArgs e) {
            Interlocked.Increment(ref _processing);
            ProcessSpeech(e.Result?.Alternates);
            Interlocked.Decrement(ref _processing);
        }

        private void ProcessSpeech(IEnumerable<RecognizedPhrase> phrases) {
            var best = phrases?.SelectMany(phrase => phrase.Words)
                               .OrderByDescending(phrase => phrase.Confidence)
                               .FirstOrDefault();

            ProcessWord(best);
        }

        private void ProcessWord(RecognizedWordUnit word) {
            if (word == null) {
                return;
            }

            if (word.Confidence < 0.3f) {
                return;
            }

            // The confidence will eventually max out and never be beaten unless the listener is reset.
            /*if (_lastResult.Item1 > word.Confidence) {
                return;
            }*/

            _lastResult = (word.Confidence, word.LexicalForm);
            PartialResult?.Invoke(this, new ValueEventArgs<string>(_lastResult.Item2));
        }

        private void OnAudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e) {
            _lastAudioSignalProblem = e.AudioSignalProblem;
        }

        private void InvokeResultAvailable() {
            var bestResult = _lastResult.Item2;
            _lastResult = default;

            if (!string.IsNullOrEmpty(bestResult) && _isListening) {
                FinalResult?.Invoke(this, new ValueEventArgs<string>(bestResult));
                return;
            }

            if (_lastAudioSignalProblem != AudioSignalProblem.None) {
                ScreenNotification.ShowNotification($"Audio signal problem: {_lastAudioSignalProblem.ToString().SplitCamelCase()}", ScreenNotification.NotificationType.Error);
            }
        }

        private async void OnStopRecording(object sender, EventArgs e) {
            do {
                await Task.Delay(650);
            } while (Interlocked.CompareExchange(ref _processing, 0, 0) > 0);
            InvokeResultAvailable();
            _isListening = false;
        }

        private void OnStartRecording(object sender, EventArgs e) {
            _lastAudioSignalProblem = AudioSignalProblem.None;
            _isListening = true;
        }

        public void Dispose() {
            GameService.Input.Mouse.LeftMouseButtonPressed -= OnLeftMouseButtonPressed;
            _speech.SecondaryVoiceStreamChanged            -= OnSecondaryVoiceStreamChanged;
            _speech.VoiceStreamChanged                     -= OnVoiceStreamChanged;
            _speech.StartRecording                         -= OnStartRecording;
            _speech.StopRecording                          -= OnStopRecording;
            _recognizer?.Dispose();
            _secondaryLanguageRecognizer?.Dispose();
        }
    }
}

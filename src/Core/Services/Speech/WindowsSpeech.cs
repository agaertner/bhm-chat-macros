using Blish_HUD;
using Blish_HUD.Controls;
using Nekres.ChatMacros.Core.Services.Speech;
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

        public event EventHandler<ValueEventArgs<string>> FinalResult;
        public event EventHandler<ValueEventArgs<string>> PartialResult;
        
        private SpeechRecognitionEngine _recognizer;

        private CultureInfo _voiceCulture;
        private Grammar     _grammar;

        private (float, string)    _lastResult;

        private AudioSignalProblem _lastAudioSignalProblem;

        private readonly SpeechService _speech;

        private DateTime _lastProcess;
        private bool     _awaitingResult;
        private bool     _isRecording;

        private int _processing;

        public WindowsSpeech(SpeechService speech) {
            _speech = speech;
        }

        private void OnInputDeviceChanged(object sender, ValueEventArgs<Stream> e) {
            _recognizer.RecognizeAsyncCancel();
            _recognizer.SetInputToAudioStream(e.Value, new SpeechAudioFormatInfo(SpeechService.SAMPLE_RATE, AudioBitsPerSample.Sixteen, (AudioChannel)SpeechService.CHANNELS));
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Reset(CultureInfo lang, params string[] grammar) {
            _speech.StartRecording     -= OnStartRecording;
            _speech.StopRecording      -= OnStopRecording;
            _speech.InputDeviceChanged -= OnInputDeviceChanged;

            ChangeModel(lang);
            Refresh();
            ChangeGrammar(false, grammar);
            _recognizer.SpeechHypothesized         += OnSpeechRecorded;
            _recognizer.SpeechRecognized           += OnSpeechRecorded;
            _recognizer.SpeechRecognitionRejected  += OnSpeechRecorded;
            _recognizer.AudioSignalProblemOccurred += OnAudioSignalProblemOccurred;

            _speech.StartRecording     += OnStartRecording;
            _speech.StopRecording      += OnStopRecording;
            _speech.InputDeviceChanged += OnInputDeviceChanged;
        }

        private void Refresh() {
            _recognizer?.Dispose();
            _recognizer               = new SpeechRecognitionEngine(_voiceCulture);
            _recognizer.MaxAlternates = 1;
        }

        private void ChangeModel(CultureInfo lang) {
            _voiceCulture = lang;
        }

        private void ChangeGrammar(bool freeDictation, params string[] grammar) {
            if (freeDictation) {
                _grammar = new DictationGrammar();
            } else {
                _grammar = new Grammar(new GrammarBuilder(new Choices(grammar)) {
                    Culture = _recognizer.RecognizerInfo.Culture
                }) {
                    Name = Guid.NewGuid().ToString("n"),
                };
            }
            _recognizer.LoadGrammar(_grammar);
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

            if (word.Confidence < 0.35f) {
                return;
            }

            if (_lastResult.Item1 > word.Confidence) {
                return;
            }

            _lastResult = (word.Confidence, word.LexicalForm);
            PartialResult?.Invoke(this, new ValueEventArgs<string>(_lastResult.Item2));
        }

        private void OnAudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e) {
            _lastAudioSignalProblem = e.AudioSignalProblem;
        }

        private void InvokeResultAvailable() {
            if (_isRecording || !_awaitingResult) {
                return;
            }
            _awaitingResult = false;

            var bestResult = _lastResult.Item2;
            _lastResult = default;

            if (!string.IsNullOrEmpty(bestResult)) {
                FinalResult?.Invoke(this, new ValueEventArgs<string>(bestResult));
                return;
            }

            if (_lastAudioSignalProblem != AudioSignalProblem.None) {
                ScreenNotification.ShowNotification($"Audio signal problem: {_lastAudioSignalProblem.ToString().SplitCamelCase()}", ScreenNotification.NotificationType.Error);
            }
        }

        private async void OnStopRecording(object sender, EventArgs e) {
            _isRecording = false;
            do {
                await Task.Delay(650);
            } while (Interlocked.CompareExchange(ref _processing, 0, 0) > 0);
            InvokeResultAvailable();
        }

        private void OnStartRecording(object sender, EventArgs e) {
            _lastAudioSignalProblem = AudioSignalProblem.None;
            _isRecording            = true;
            _awaitingResult         = true;
        }

        public void Dispose() {
            _speech.InputDeviceChanged -= OnInputDeviceChanged;
            _speech.StartRecording     -= OnStartRecording;
            _speech.StopRecording      -= OnStopRecording;
            _recognizer?.Dispose();
        }
    }
}

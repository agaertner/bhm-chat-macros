/*using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Extended;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Vosk;

namespace Nekres.ChatMacros.Core.Services.Speech {
    internal class VoskSpeech : ISpeechRecognitionProvider {

        public event EventHandler<ValueEventArgs<string>> FinalResult;

        private Model          _model;
        private VoskRecognizer _vRec;
        private string         _grammar;

        private byte[] _audioBuffer;

        public VoskSpeech(SpeechService speech) {
            speech.StartRecording += OnStartRecording;
            speech.StopRecording  += OnStopRecording;
        }

        private void OnStopRecording(object sender, EventArgs e) {
            if (_audioBuffer == null) {
                return;
            }

            if (!_vRec.AcceptWaveform(_audioBuffer, _audioBuffer.Length)) {
                return;
            }

            FinalResult?.Invoke(this, new ValueEventArgs<string>(_vRec.FinalResult()));
        }

        private async void OnStartRecording(object sender, ValueEventArgs<Stream> e) {
            _audioBuffer = new byte[4096];
            var count = await e.Value.ReadAsync(_audioBuffer, 0, _audioBuffer.Length);

            if (count == 0) {
                ScreenNotification.ShowNotification("Nothing detected.");
            }

        }

        public async void CreateRecognizer(CultureInfo lang, params string[] grammar) {
            await ChangeModel(lang);
            ChangeGrammar(grammar);
            Refresh();
        }

        private void Refresh() {
            _vRec?.Dispose();
            _vRec = new VoskRecognizer(_model, SpeechService.SAMPLE_RATE, _grammar);
            _vRec.SetMaxAlternatives(0);
        }

        private async Task ChangeModel(CultureInfo lang) {
            var outDir      = ChatMacros.Instance.ModuleDirectory;
            var file        = $"{lang}.zip";
            var archiveDest = Path.Combine(outDir, file);

            var mdlPath = Path.Combine(outDir, lang.TwoLetterISOLanguageName);

            try {
                await ChatMacros.Instance.ContentsManager.Extract($"vosk/{file}", archiveDest);

                if (Directory.Exists(mdlPath)) {
                    Directory.Delete(mdlPath, true);
                }

                ZipFile.ExtractToDirectory(archiveDest, mdlPath);
                File.Delete(archiveDest);
                _model?.Dispose();
                _model = new Model(mdlPath);
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, $"Failed to change speech recognition model to '{lang}'.");
            } 
        }

        private void ChangeGrammar(params string[] grammar) {
            _grammar = $"[\"{string.Join("\",\"", grammar)}\"]";
        }

        public void Dispose() {
            _model?.Dispose();
            _vRec?.Dispose();
        }
    }
}*/

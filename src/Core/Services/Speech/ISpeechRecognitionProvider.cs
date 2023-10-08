using Blish_HUD;
using System;
using System.Globalization;

namespace Nekres.ChatMacros.Core.Services.Speech {
    internal interface ISpeechRecognitionProvider : IDisposable {

        event EventHandler<EventArgs>              SpeechDetected; 
        event EventHandler<ValueEventArgs<string>> PartialResult; 
        event EventHandler<ValueEventArgs<string>> FinalResult;

        void Reset(CultureInfo lang, CultureInfo secondaryLang, bool freeDictation, params string[] grammar);

        void ChangeGrammar(bool freeDictation, params string[] grammar);
    }
}

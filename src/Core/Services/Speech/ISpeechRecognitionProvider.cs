using Blish_HUD;
using System;
using System.Globalization;

namespace Nekres.ChatMacros.Core.Services.Speech {
    internal interface ISpeechRecognitionProvider : IDisposable {

        event EventHandler<ValueEventArgs<string>> PartialResult; 
        event EventHandler<ValueEventArgs<string>> FinalResult;

        void Reset(CultureInfo lang, params string[] grammar);
    }
}

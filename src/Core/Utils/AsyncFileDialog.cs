using Blish_HUD;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nekres.ChatMacros.Core {
    public class AsyncFileDialog<T> : IDisposable
        where T : FileDialog, new() {
        public T Dialog { get; }

        private readonly TaskCompletionSource<DialogResult> _result;
        private readonly Thread                             _thread;

        public AsyncFileDialog(string title = null, string filter = null, string selectedFileName = null) {
            Dialog = new T {
                Title = title,
                Filter = filter,
                FileName = selectedFileName,
                InitialDirectory = string.IsNullOrEmpty(selectedFileName) ? string.Empty : Path.GetDirectoryName(selectedFileName)
            };

            _result = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            _thread = new Thread(() =>
            {
                void Abort(object sender, EventArgs e) => _thread?.Abort();

                try {
                    GameService.GameIntegration.Gw2Instance.Gw2AcquiredFocus += Abort;
                    _result.SetResult(Dialog.ShowDialog());
                } catch (ThreadAbortException) {
                    _result.SetResult(DialogResult.Cancel);
                } catch (Exception e) {
                    _result.SetException(e);
                } finally {
                    GameService.GameIntegration.Gw2Instance.Gw2AcquiredFocus -= Abort;
                }
            });

            _thread.SetApartmentState(ApartmentState.STA);
        }

        public Task<DialogResult> Show() {
            _thread.Start();
            return _result.Task;
        }

        public void Dispose() {
            _thread.Abort();
            Dialog?.Dispose();
        }
    }
}

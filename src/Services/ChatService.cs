using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Extended;
using Nekres.Chat_Shorts.Core;
using Nekres.Chat_Shorts.Properties;
using Nekres.Chat_Shorts.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nekres.Chat_Shorts.Services {
    internal class ChatService : IDisposable
    {
        private DataService _dataService;

        private List<Macro> _activeMacros;

        private bool isWorking;

        private CancellationTokenSource workerToken;

        public ChatService(DataService dataService)
        {
            _dataService = dataService;
            _activeMacros = new List<Macro>();
            GameService.Gw2Mumble.CurrentMap.MapChanged += OnMapChanged;
            GameService.Gw2Mumble.PlayerCharacter.IsCommanderChanged += OnIsCommanderChanged;
        }

        private void OnMapChanged(object o, ValueEventArgs<int> e)
        {
            if (!ChatShorts.Instance.Loaded) {
                return;
            }

            LoadMacros();
        }

        private void OnIsCommanderChanged(object o, ValueEventArgs<bool> e)
        {
            if (!ChatShorts.Instance.Loaded) {
                return;
            }

            LoadMacros();
        }

        public void LoadMacros()
        {
            foreach (var macro in _activeMacros)
            {
                macro.Activated -= OnMacroActivated;
                macro.Dispose();
            }
            _activeMacros.Clear();
            foreach (var entity in _dataService.GetAllActives())
            {
                this.ToggleMacro(MacroModel.FromEntity(entity));
            }
        }

        public void ToggleMacro(MacroModel model)
        {
            var macro = _activeMacros.FirstOrDefault(x => x.Model.Id.Equals(model.Id));
            _activeMacros.RemoveAll(x => x.Model.Id.Equals(model.Id));
            if (macro != null) {
                macro.Activated -= OnMacroActivated;
            }

            macro?.Dispose();
            macro = Macro.FromModel(model);
            if (!macro.CanActivate()) {
                return;
            }

            macro.Activated += OnMacroActivated;
            _activeMacros.Add(macro);
        }

        private async void OnMacroActivated(object o, EventArgs e)
        {
            var macro = (Macro)o;
            await this.Send(macro.Model.TextLines.ToArray(), macro.Model.SquadBroadcast);
        }

        public async Task Send(string[] textLines, bool squadBroadcast = false) {
            if (isWorking) {
                workerToken?.Cancel();
                ScreenNotification.ShowNotification("Message sequence canceled.");
                return;
            }
            if (squadBroadcast && !GameService.Gw2Mumble.PlayerCharacter.IsCommander) {
                return;
            }

            isWorking   = true;
            workerToken = new CancellationTokenSource();
            var ct = workerToken.Token;
            await Task.Factory.StartNew(() => {
                if (ct.IsCancellationRequested) {
                    isWorking = false;
                    return;
                };

                foreach (string textLine in textLines) {
                    ChatUtil.Send(textLine, squadBroadcast ? ChatShorts.Instance.SquadBroadcast.Value : ChatShorts.Instance.ChatMessage.Value);

                    if (ct.IsCancellationRequested) {
                        break;
                    }
                }
                isWorking   = false;
            }, ct, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void Dispose()
        {
            workerToken?.Cancel();
            workerToken?.Dispose();
            foreach (var macro in _activeMacros)
            {
                macro.Activated -= OnMacroActivated;
                macro.Dispose();
            }
            GameService.Gw2Mumble.CurrentMap.MapChanged -= OnMapChanged;
            GameService.Gw2Mumble.PlayerCharacter.IsCommanderChanged -= OnIsCommanderChanged;
        }
    }
}

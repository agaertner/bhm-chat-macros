using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Nekres.Chat_Shorts.UI.Controls;
using Nekres.Chat_Shorts.UI.Models;
using Nekres.Chat_Shorts.UI.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD.Settings;
using Blish_HUD.Settings.UI.Views;

namespace Nekres.Chat_Shorts.UI.Views
{
    internal class MacroEditView : View<MacroEditPresenter>
    {
        private bool _deleted;
        private IList<Map> _maps;

        private FlowPanel _mapsPanel;
        private FlowPanel _mapsExclusionPanel;
        private ViewContainer _settingsPanel;

        public MacroEditView(MacroModel model)
        {
            this.WithPresenter(new MacroEditPresenter(this, model));
            _maps = new List<Map>();
        }

        protected override async Task<bool> Load(IProgress<string> progress)
        {
            List<int> mapIds = this.Presenter.Model.MapIds.Concat(this.Presenter.Model.ExcludedMapIds).ToList();
            if (!mapIds.Any()) {
                return true;
            }

            _maps = (await ChatShorts.Instance.Gw2ApiManager.Gw2ApiClient.V2.Maps.ManyAsync(mapIds)).ToList();
            return _maps.Any();
        }

        protected override void Unload()
        {
            if (!_deleted)
            {
                ChatShorts.Instance.DataService.UpsertMacro(this.Presenter.Model);
            }
            _settingsPanel?.Dispose();
            base.Unload();
        }

        protected override void Build(Container buildPanel)
        {
            var editTitle = new TextBox
            {
                Parent = buildPanel,
                Size = new Point(buildPanel.ContentRegion.Width, 42),
                Location = new Point(0,0),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular),
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = this.Presenter.Model.Title
            };
            editTitle.InputFocusChanged += EditTitle_InputFocusChanged;

            var textFlowPanel = new FlowPanel {
                Parent   = buildPanel,
                Size     = new Point(buildPanel.ContentRegion.Width, buildPanel.ContentRegion.Height / 2 - 60),
                Location = new Point(0,                              editTitle.Bottom                    + Panel.BOTTOM_PADDING),
                ShowTint = true,
                ShowBorder = true
            };

            foreach (string message in this.Presenter.Model.TextLines) {
                var inputBox = new TextBox {
                    Parent          = textFlowPanel,
                    Size            = new Point(textFlowPanel.Width, 32),
                    PlaceholderText = "/say Hello World!",
                    Text = message
                };

                inputBox.InputFocusChanged += EditText_InputFocusChanged;
            }

            // MapIds selection
            _mapsPanel = new FlowPanel
            {
                Parent         = buildPanel,
                Size           = new Point(textFlowPanel.Width / 4 - 5, buildPanel.ContentRegion.Height - textFlowPanel.Height - 100),
                Location       = new Point(0,                           textFlowPanel.Bottom            + Panel.BOTTOM_PADDING),
                FlowDirection  = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 5),
                CanCollapse    = false,
                CanScroll      = true,
                Collapsed      = false,
                ShowTint       = true,
                ShowBorder     = true
            };
            foreach (int id in this.Presenter.Model.MapIds) {
                CreateMapEntry(id, _mapsPanel, OnMapClick);
            }

            var btnIncludeMap = new StandardButton
            {
                Parent = buildPanel,
                Size = new Point(150, StandardButton.STANDARD_CONTROL_HEIGHT),
                Location = new Point(_mapsPanel.Location.X + (_mapsPanel.Width - 150) / 2, _mapsPanel.Location.Y + _mapsPanel.Height),
                Text = "Include Map"
            };
            btnIncludeMap.Click += BtnIncludeMap_Click;

            // MapIds selection
            _mapsExclusionPanel = new FlowPanel
            {
                Parent         = buildPanel,
                Size           = new Point(textFlowPanel.Width / 4 - 5, buildPanel.ContentRegion.Height - textFlowPanel.Height - 100),
                Location       = new Point(_mapsPanel.Right        + 5, textFlowPanel.Bottom            + Panel.BOTTOM_PADDING),
                FlowDirection  = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 5),
                CanCollapse    = false,
                CanScroll      = true,
                Collapsed      = false,
                ShowTint       = true,
                ShowBorder     = true
            };
            foreach (int id in this.Presenter.Model.ExcludedMapIds) {
                CreateMapEntry(id, _mapsExclusionPanel, OnExcludedMapClick);
            }

            var btnExcludeMap = new StandardButton
            {
                Parent = buildPanel,
                Size = new Point(150, StandardButton.STANDARD_CONTROL_HEIGHT),
                Location = new Point(_mapsExclusionPanel.Location.X + (_mapsExclusionPanel.Width - 150) / 2, _mapsExclusionPanel.Location.Y + _mapsExclusionPanel.Height),
                Text = "Exclude Map"
            };
            btnExcludeMap.Click += BtnExcludeMap_Click;

            var settings = new SettingCollection(); 
            var contextCol = settings.AddSubCollection("Context");
            contextCol.RenderInUi = true;
            SettingEntry<bool> squadBroadcastSetting = contextCol.DefineSetting("isSquadBroadcast", this.Presenter.Model.SquadBroadcast,
                                                                                () => "Squad Broadcast",
                                                                                () => "Send this text as a squad broadcast instead.");
            SettingEntry<GameMode> gameModeSetting = contextCol.DefineSetting("inGameMode", this.Presenter.Model.Mode,
                                                                              () => "GameMode",
                                                                              () => "GameMode this macro will be active in.");
            var controlsCol = settings.AddSubCollection("Hotkeys");
            controlsCol.RenderInUi = true;
            SettingEntry<KeyBinding> keyBindingSetting = controlsCol.DefineSetting("Macro Key", this.Presenter.Model.KeyBinding,
                                                                                   () => "Macro Key",
                                                                                   () => "Shortcut to use the macro with (optional).");

            _settingsPanel = new ViewContainer
            {
                Parent = buildPanel,
                Width = buildPanel.ContentRegion.Width / 2,
                Height = buildPanel.ContentRegion.Height - textFlowPanel.Height - 100,
                Location = new Point(_mapsExclusionPanel.Right + 10, _mapsExclusionPanel.Location.Y),
                ShowBorder = false
            };
            _settingsPanel.Show(new SettingsView(settings));

            squadBroadcastSetting.SettingChanged += (_,e) => this.Presenter.Model.SquadBroadcast = e.NewValue;
            gameModeSetting.SettingChanged += (_, e) =>
            {
                GameService.Content.PlaySoundEffectByName("button-click");
                this.Presenter.Model.Mode = e.NewValue;
            };

            // Delete button
            var delBtn = new DeleteButton(ChatShorts.Instance.ContentsManager)
            {
                Parent = buildPanel,
                Size = new Point(42,42),
                Location = new Point(buildPanel.ContentRegion.Width - 42, btnIncludeMap.Location.Y + btnIncludeMap.Height - 42),
                BasicTooltipText = "Delete Macro"
            };
            delBtn.Click += DeleteButton_Click;
        }

        private void EditTitle_InputFocusChanged(object o, EventArgs e)
        {
            var ctrl = (TextBox)o;
            if (ctrl.Focused) {
                return;
            }
            this.Presenter.Model.Title = ctrl.Text;
            ((StandardWindow)ctrl.Parent).Title = $"Edit Macro - {ctrl.Text}";
        }

        private void EditText_InputFocusChanged(object o, EventArgs e)
        {
            var ctrl = (TextBox)o;
            if (ctrl.Focused) {
                return;
            }
            this.Presenter.Model.TextLines.Add(ctrl.Text);
        }

        private async void BtnIncludeMap_Click(object o, MouseEventArgs e)
        {
            GameService.Content.PlaySoundEffectByName("button-click");
            if (this.Presenter.Model.MapIds.Any(id => id.Equals(GameService.Gw2Mumble.CurrentMap.Id))) {
                return;
            }
            await ChatShorts.Instance.Gw2ApiManager.Gw2ApiClient.V2.Maps
                            .GetAsync(GameService.Gw2Mumble.CurrentMap.Id).ContinueWith(t =>
                             {
                                 if (t.IsFaulted) {
                                     return;
                                 }

                                 var map = t.Result;
                                 _maps.Add(map);
                                 this.Presenter.Model.MapIds.Add(map.Id);
                                 CreateMapEntry(map.Id, _mapsPanel, OnMapClick);
                             });
        }

        private async void BtnExcludeMap_Click(object o, MouseEventArgs e)
        {
            GameService.Content.PlaySoundEffectByName("button-click");
            if (this.Presenter.Model.ExcludedMapIds.Any(id => id.Equals(GameService.Gw2Mumble.CurrentMap.Id))) {
                return;
            }
            await ChatShorts.Instance.Gw2ApiManager.Gw2ApiClient.V2.Maps
                            .GetAsync(GameService.Gw2Mumble.CurrentMap.Id).ContinueWith(t =>
                             {
                                 if (t.IsFaulted) {
                                     return;
                                 }

                                 var map = t.Result;
                                 _maps.Add(map);
                                 this.Presenter.Model.ExcludedMapIds.Add(map.Id);
                                 CreateMapEntry(map.Id, _mapsExclusionPanel, OnExcludedMapClick);
                             });
        }

        private void CreateMapEntry(int mapId, FlowPanel parent, EventHandler<MouseEventArgs> clickAction)
        {
            var map = _maps.First(x => x.Id == mapId);
            var mapEntry = new MapEntry(map.Id, map.Name)
            {
                Parent = parent,
                Size = new Point(parent.ContentRegion.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
            };
            mapEntry.Click += clickAction;
        }

        private void OnMapClick(object o, MouseEventArgs e)
        {
            GameService.Content.PlaySoundEffectByName("button-click");
            var ctrl = (MapEntry) o;
            this.Presenter.Model.MapIds.Remove(ctrl.MapId);
            ctrl.Dispose();
        }
        private void OnExcludedMapClick(object o, MouseEventArgs e)
        {
            GameService.Content.PlaySoundEffectByName("button-click");
            var ctrl = (MapEntry)o;
            this.Presenter.Model.ExcludedMapIds.Remove(ctrl.MapId);
            ctrl.Dispose();
        }

        private void DeleteButton_Click(object o, MouseEventArgs e)
        {
            _deleted = true;
            this.Presenter.Delete();
            ((DeleteButton)o).Parent.Hide();
        }
    }
}

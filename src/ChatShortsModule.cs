using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nekres.ChatMacros.Core.Services;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Core.UI.Library;
using Nekres.ChatMacros.Core.UI.Settings;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using Gw2WebApiService = Nekres.ChatMacros.Core.Services.Gw2WebApiService;

namespace Nekres.ChatMacros {
    [Export(typeof(Module))]
    public class ChatMacros : Module
    {

        internal static readonly Logger Logger = Logger.GetLogger<ChatMacros>();

        internal static ChatMacros Instance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        public string ModuleDirectory { get; private set; }

        private TabbedWindow2 _moduleWindow;
        private CornerIcon    _cornerIcon;
        private Texture2D     _cornerTexture;

        internal SettingEntry<KeyBinding> SquadBroadcast;
        internal SettingEntry<KeyBinding> ChatMessage;
        internal SettingEntry<InputConfig> InputConfig;

        internal Gw2WebApiService Gw2Api;
        internal ResourceService  Resources;
        internal DataService      Data;
        internal MacroService     Macro;
        internal SpeechService    Speech;

        private Tab    _libraryTab;
        private Tab    _settingsTab;
        private double _lastRun;
        
        [ImportingConstructor]
        public ChatMacros([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            Instance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            var controlSettings = settings.AddSubCollection("Control Options (User Interface)", true, false,
                () => "Control Options (User Interface)");
            ChatMessage = controlSettings.DefineSetting("chatMessageKeyBinding", new KeyBinding(Keys.Enter),
                () => "Chat Message",
                () => "Give focus to the chat edit box.");
            SquadBroadcast = controlSettings.DefineSetting("squadBroadcastKeyBinding", new KeyBinding(ModifierKeys.Shift, Keys.Enter),
                () => "Squad Broadcast Message", 
                () => "Give focus to the chat edit box.");

            var selfManaged = settings.AddSubCollection("configs", false, false);
            InputConfig = selfManaged.DefineSetting("input_config", Core.UI.Configs.InputConfig.Default);
        }

        protected override void Initialize() {
            ModuleDirectory              = DirectoriesManager.GetFullDirectoryPath("chat_shorts");

            _cornerTexture               = ContentsManager.GetTexture("corner_icon.png");
            SquadBroadcast.Value.Enabled = false;
            ChatMessage.Value.Enabled    = false;
        }

        protected override void OnModuleLoaded(EventArgs e) {
            Data      = new DataService();
            Gw2Api    = new Gw2WebApiService();
            Resources = new ResourceService();
            Macro     = new MacroService();
            Speech    = new SpeechService();

            var windowRegion  = new Rectangle(40, 26, 913, 691);
            _moduleWindow = new TabbedWindow2(GameService.Content.DatAssetCache.GetTextureFromAssetId(155985),
                                              windowRegion, 
                                              new Rectangle(100, 36, 839, 605))
            {
                Parent        = GameService.Graphics.SpriteScreen,
                Emblem        = _cornerTexture,
                SavesPosition = true,
                SavesSize     = true,
                Title         = this.Name,
                Id            = $"{nameof(ChatMacros)}_42d3a11e-ffa7-4c82-8fd9-ee9d9a118914",
                Left          = (GameService.Graphics.SpriteScreen.Width  - windowRegion.Width)  / 2,
                Top           = (GameService.Graphics.SpriteScreen.Height - windowRegion.Height) / 2
            };
            _cornerIcon = new CornerIcon
            {
                Icon = ContentsManager.GetTexture("corner_icon.png"),
                BasicTooltipText = this.Name
            };
            _cornerIcon.Click += OnModuleIconClick;

            _libraryTab = new Tab(GameService.Content.DatAssetCache.GetTextureFromAssetId(155156),
                                  () => new LibraryView(), Properties.Resources.Library);

            _settingsTab = new Tab(GameService.Content.DatAssetCache.GetTextureFromAssetId(155052),
                                   () => new SettingsView(InputConfig.Value), Properties.Resources.Settings);
            _moduleWindow.Tabs.Add(_libraryTab);
            _moduleWindow.Tabs.Add(_settingsTab);
            _moduleWindow.TabChanged += OnTabChanged;

            GameService.Overlay.UserLocaleChanged += OnUserLocaleChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnUserLocaleChanged(object sender, ValueEventArgs<CultureInfo> e) {
            _libraryTab.Name  = Properties.Resources.Library;
            _settingsTab.Name = Properties.Resources.Settings;
        }

        private void OnTabChanged(object sender, ValueChangedEventArgs<Tab> e) {
            _moduleWindow.Subtitle = e.NewValue.Name;
        }

        public void OnModuleIconClick(object o, MouseEventArgs e) {
            _moduleWindow.Show();
        }

        protected override void Update(GameTime gameTime) {
            // Rate limit update
            if (gameTime.TotalGameTime.TotalMilliseconds - _lastRun < 10) {
                return;
            }
            _lastRun = gameTime.ElapsedGameTime.TotalMilliseconds;

            Speech?.Update(gameTime);
            Macro?.Update(gameTime);
            base.Update(gameTime);
        }

        /// <inheritdoc />
        protected override void Unload() {
            GameService.Overlay.UserLocaleChanged -= OnUserLocaleChanged;
            if (_cornerIcon != null)
            {
                _cornerIcon.Click -= OnModuleIconClick;
                _cornerIcon.Dispose();
            }
            _moduleWindow?.Dispose();
            _cornerTexture?.Dispose();
            Speech?.Dispose();
            Macro?.Dispose();
            Resources?.Dispose();
            Gw2Api?.Dispose();
            Data?.Dispose();
            // All static members must be manually unset
            Instance = null;
        }
    }
}

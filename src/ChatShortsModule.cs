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
using Nekres.ChatMacros.Core.UI.Settings;
using System;
using System.ComponentModel.Composition;
using Nekres.ChatMacros.Core.UI.Library;

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
        private CornerIcon _cornerIcon;
        private ContextMenuStrip _moduleContextMenu;

        private Texture2D _cornerTexture;

        internal SettingEntry<KeyBinding> SquadBroadcast;
        internal SettingEntry<KeyBinding> ChatMessage;
        internal SettingEntry<InputConfig> InputConfig;

        internal ResourceService Resources;
        internal DataService     Data;
        internal SpeechService   Speech;

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
            Resources = new ResourceService();
            Data      = new DataService();
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

            var settingsTab = new Tab(GameService.Content.DatAssetCache.GetTextureFromAssetId(155052), () => new SettingsView(InputConfig.Value), "Settings");
            var macrosTab   = new Tab(GameService.Content.DatAssetCache.GetTextureFromAssetId(155052), () => new LibraryView(), "Library");
            _moduleWindow.Tabs.Add(macrosTab);
            _moduleWindow.Tabs.Add(settingsTab);
            _moduleWindow.TabChanged  += OnTabChanged;
            _moduleWindow.SelectedTab =  settingsTab;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void OnTabChanged(object sender, ValueChangedEventArgs<Tab> e) {
            _moduleWindow.Subtitle = e.NewValue.Name;
        }

        public void OnModuleIconClick(object o, MouseEventArgs e) {
            _moduleWindow.Show();
        }

        protected override void Update(GameTime gameTime) {
            Speech?.Update(gameTime);
            base.Update(gameTime);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload here
            Data?.Dispose();
            _moduleContextMenu?.Dispose();
            if (_cornerIcon != null)
            {
                _cornerIcon.Click -= OnModuleIconClick;
                _cornerIcon.Dispose();
            }
            _moduleWindow?.Dispose();
            _cornerTexture?.Dispose();
            // All static members must be manually unset
        }
    }
}

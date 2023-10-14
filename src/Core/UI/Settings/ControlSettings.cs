using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.UI.Configs;
using Nekres.ChatMacros.Properties;

namespace Nekres.ChatMacros.Core.UI.Settings {
    internal class ControlSettings : View {

        private ControlsConfig _config;

        public ControlSettings(ControlsConfig config) {
            _config = config;
        }

        protected override void Build(Container buildPanel) {
            var miscSettings = new FlowPanel {
                Parent              = buildPanel,
                Width               = buildPanel.ContentRegion.Width,
                Height              = buildPanel.ContentRegion.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding      = new Vector2(5, 5),
                CanCollapse         = true,
                Title               = Resources.Control_Options
            };

            var quickAccessKeybinding = new KeybindingAssigner(_config.OpenQuickAccess) {
                Parent           = miscSettings,
                KeyBindingName   = Resources.Open_Quick_Access,
                BasicTooltipText = Resources.Show_or_hide_the_quick_access_menu_to_all_active_macros_
            };

            var userInterface = new FlowPanel {
                Parent              = miscSettings,
                Width               = miscSettings.ContentRegion.Width,
                Height              = miscSettings.ContentRegion.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding      = new Vector2(5, 5),
                CanCollapse         = true,
                Title = Resources.User_Interface
            };

            var chatMessageKeybinding = new KeybindingAssigner(_config.ChatMessage) {
                Parent           = userInterface,
                KeyBindingName   = Resources.Chat_Message,
                BasicTooltipText = Resources.Give_focus_to_the_chat_edit_box_
            };

            var squadBroadcastMessageKeybinding = new KeybindingAssigner(_config.SquadBroadcastMessage) {
                Parent           = userInterface,
                KeyBindingName   = Resources.Squad_Broadcast_Message,
                BasicTooltipText = Resources.Give_focus_to_the_chat_edit_box_
            };

            base.Build(buildPanel);
        }
    }
}

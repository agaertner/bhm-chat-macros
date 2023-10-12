using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.UI.Configs;

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
                Title               = "Misc"
            };

            var quickAccessKeybinding = new KeybindingAssigner(_config.OpenQuickAccess) {
                Parent           = miscSettings,
                KeyBindingName   = "Open Quick Access"
            };

            base.Build(buildPanel);
        }
    }
}

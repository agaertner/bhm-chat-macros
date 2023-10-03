using System.Collections.Generic;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Core.Services.Data;

namespace Nekres.ChatMacros.Core.UI.Library {
    internal class LibraryView : View {

        public LibraryView() {
            
        }

        protected override void Build(Container buildPanel) {

            var flowPanel = new FlowPanel {
                Parent = buildPanel,
                Width = buildPanel.ContentRegion.Width / 2,
                Height = buildPanel.ContentRegion.Height - 50,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding = new Vector2(5, 5)
            };

            var createNewBttn = new StandardButton {
                Parent = buildPanel,
                Width  = flowPanel.Width,
                Height = 50,
                Top    = flowPanel.Bottom,
                Text   = "Create New"
            };


            var macros = ChatMacros.Instance.Data.GetAllChatMacros();

            foreach (var macro in macros) {
                AddMacroEntry(flowPanel, macro);
            }

            createNewBttn.Click += delegate {
                var newMacro = new ChatMacro {
                    Title = "My Macro",
                    Lines = new List<(Channel, string)> {
                        (Channel.Say, "Hello, world!")
                    }
                };

                if (!ChatMacros.Instance.Data.Upsert(newMacro)) {
                    ScreenNotification.ShowNotification("Something went wrong. Please try again", ScreenNotification.NotificationType.Error);
                    return;
                }

                AddMacroEntry(flowPanel, newMacro);
            };

            base.Build(buildPanel);
        }

        private void AddMacroEntry(Container parent, ChatMacro macro) {
            var macroEntry = new ViewContainer {
                Parent = parent,
                Width  = parent.ContentRegion.Width,
                Height = 100,
                Title  = macro.Title,
                CanCollapse = true
            };
            macroEntry.Show(new MacroView(macro));
        }

        private class MacroView : View {

            private readonly ChatMacro _macro;

            public MacroView(ChatMacro macro) {
                _macro = macro;
            }

            protected override void Build(Container buildPanel) {

                var flowPanel = new FlowPanel {
                    Parent = buildPanel,
                    Width = buildPanel.ContentRegion.Width,
                    Height = buildPanel.ContentRegion.Height,
                    FlowDirection = ControlFlowDirection.SingleTopToBottom,
                    OuterControlPadding = new Vector2(5, 5),
                    ControlPadding = new Vector2(5, 5),
                    CanCollapse = true
                };
                base.Build(buildPanel);
            }

        }
    }
}

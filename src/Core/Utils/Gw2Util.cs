using Blish_HUD;

namespace Nekres.ChatMacros.Core {
    internal static class Gw2Util {
        public static bool IsInGame() {
            return GameService.Gw2Mumble.IsAvailable                    &&
                   GameService.GameIntegration.Gw2Instance.Gw2IsRunning &&
                   GameService.GameIntegration.Gw2Instance.Gw2HasFocus  &&
                   GameService.GameIntegration.Gw2Instance.IsInGame     &&
                   !GameService.Gw2Mumble.UI.IsTextInputFocused &&
                   !GameService.Gw2Mumble.UI.IsMapOpen;
        }
    }
}

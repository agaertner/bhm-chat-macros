using Microsoft.Xna.Framework.Graphics;

namespace Nekres.ChatMacros.Core.Services {
    internal class ResourceService {
        public Texture2D DragReorderIcon { get; private set; }

        public ResourceService() {
            DragReorderIcon = ChatMacros.Instance.ContentsManager.GetTexture("icons/drag-reorder.png");
        }
    }
}

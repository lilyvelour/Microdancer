using System.Numerics;
using ImGuiNET;

namespace Microdancer
{
    public abstract class Theme
    {
        public abstract void Begin();

        public abstract void End();

        public abstract Vector4 GetColor(ImGuiCol color);
    }
}

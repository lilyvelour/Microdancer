using ImGuiNET;

namespace Microdancer
{
    public class NodeContextMenu : PluginUiBase, IDrawable<INode>
    {
        public bool Draw(INode node)
        {
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Selectable("Select"))
                {
                    Config.LibrarySelection = node.Id;
                    PluginInterface.SavePluginConfig(Config);
                }

                if (!node.IsReadOnly)
                {
                    if (ImGui.Selectable("Open"))
                    {
                        OpenNode(node);
                    }

                    if (ImGui.Selectable("Reveal in File Explorer"))
                    {
                        RevealNode(node);
                    }
                }

                if (node is Micro micro)
                {
                    ImGui.Separator();

                    if (ImGui.Selectable("Play"))
                    {
                        Config.LibrarySelection = micro.Id;
                        PluginInterface.SavePluginConfig(Config);
                        MicroManager.StartMicro(micro);
                    }

                    if (ImGui.Selectable($"Copy run command"))
                    {
                        ImGui.SetClipboardText($"/runmicro {micro.Id}");
                    }

                    if (!node.IsReadOnly)
                    {
                        ImGui.Separator();

                        var isShared = Config.SharedItems.Contains(micro.Id);
                        if (ImGui.Selectable(isShared ? "Stop sharing" : "Share"))
                        {
                            if (isShared)
                            {
                                Config.SharedItems.Remove(micro.Id);
                            }
                            else
                            {
                                Config.SharedItems.Add(micro.Id);
                            }

                            PluginInterface.SavePluginConfig(Config);
                        }
                    }
                }

                ImGui.EndPopup();

                return true;
            }

            return false;
        }
    }
}

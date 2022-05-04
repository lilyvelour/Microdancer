using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ImGuiNET;

namespace Microdancer
{
    public class RegionBar : PluginUiBase, IDrawable<Micro>
    {
        private MicroInfo? _info;

        private float _lastColumnWidth;

        public bool Draw(Micro micro)
        {
            if (MicroManager.Current?.Micro == micro)
            {
                _info = MicroManager.Current;
            }
            else if (_info?.Micro != micro || _info.CurrentTime > TimeSpan.Zero)
            {
                _info = new MicroInfo(micro);
            }

            ImGui.BeginChildFrame(
                439483,
                new Vector2(-1, -112 * ImGuiHelpers.GlobalScale),
                ImGuiWindowFlags.NoBackground
            );

            var inCombat = Condition[ConditionFlag.InCombat];
            var regions = _info!.AllRegions;
            var regionButtonSize = Vector2.Zero;

            float? newColumnWidth = null;

            ImGui.TextWrapped(micro.Name);

            ImGui.Spacing();

            if (regions.Length == 0 || regions.All(r => r.IsDefaultRegion))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f));
                if (micro.IsReadOnly)
                {
                    ImGui.TextWrapped("- No regions -");
                }
                else
                {
                    ImGui.TextWrapped(
                        "Add a region to your file (using #region [name] and #endregion) to have it show up here.\n\nRegions starting with \":\" will show up as special buttons that are not part of the timeline."
                    );
                }
                ImGui.PopStyleColor();
            }
            else
            {
                regions = regions.Where(r => !r.IsDefaultRegion).ToArray();

                for (int i = 0; i < regions.Length; i++)
                {
                    var sz = ImGui.CalcTextSize($"{i + 1}");
                    if (sz.X >= regionButtonSize.X)
                    {
                        regionButtonSize = sz;
                    }
                }

                regionButtonSize.X += 80 * ImGuiHelpers.GlobalScale;
                regionButtonSize.Y += 40 * ImGuiHelpers.GlobalScale;

                var col = 0;
                var regionNumber = 1;

                var columnWidth = ImGui.GetColumnWidth();
                if (Math.Abs(columnWidth - _lastColumnWidth) > 1.0f)
                {
                    var w = columnWidth;
                    var gridWidth =
                        regionButtonSize.X
                        + ImGui.GetStyle().ItemSpacing.X * 2.0f
                        + ImGui.GetStyle().ColumnsMinSpacing * 2.0f
                        + ImGui.GetStyle().FramePadding.X * 2.0f;

                    if (w % gridWidth < gridWidth * 0.5f)
                        w -= w % gridWidth;
                    else
                        w += gridWidth - w % gridWidth;

                    newColumnWidth = Math.Max(w, gridWidth);
                }

                for (int i = 0; i < regions.Length; i++)
                {
                    if (regions[i].IsDefaultRegion)
                    {
                        continue;
                    }

                    col++;

                    var region = regions[i];
                    var isNamedRegion = false;
                    var size = regionButtonSize;

                    if (region.IsNamedRegion)
                    {
                        while (
                            size.X + regionButtonSize.X
                            < ImGui.GetContentRegionAvail().X + Theme.GetStyle<Vector2>(ImGuiStyleVar.FramePadding).X
                        )
                        {
                            size.X += regionButtonSize.X + ImGui.GetStyle().ItemSpacing.X;
                        }
                        isNamedRegion = true;
                    }

                    var currentRegion = MicroManager.Current?.CurrentCommand?.Region;

                    if (
                        !isNamedRegion
                        && MicroManager.Current?.Micro == micro
                        && MicroManager.PlaybackState != PlaybackState.Stopped
                        && currentRegion == region
                    )
                    {
                        ImGui.ProgressBar(currentRegion.GetProgress(), size, $"{regionNumber++}");
                    }
                    else if (ImGui.Button(isNamedRegion ? region.Name : $"{regionNumber++}", size))
                    {
                        if (!inCombat)
                        {
                            MicroManager.StartMicro(micro, region.Name);
                        }
                    }

                    ImGuiExt.TextTooltip(region.Name);

                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.Selectable($"Copy run command"))
                        {
                            ImGui.SetClipboardText(
                                $"/runmicro {micro.Id} \"{(region.IsNamedRegion ? ":" : string.Empty)}{region.Name}\""
                            );
                        }

                        ImGui.EndPopup();
                    }

                    if (
                        i < regions.Length - 1
                        && !isNamedRegion
                        && ImGui.GetContentRegionAvail().X - ((size.X + ImGui.GetStyle().ItemSpacing.X) * col) > size.X
                    )
                    {
                        var nextRegion = i == regions.Length - 1 ? null : regions[i + 1];

                        if (nextRegion?.IsNamedRegion != true)
                        {
                            ImGui.SameLine();
                        }
                    }
                    else
                    {
                        col = 0;
                    }
                }
            }

            ImGui.EndChildFrame();

            if (newColumnWidth != null && regions.Length > 0 && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                ImGui.SetColumnOffset(2, Math.Max(ImGui.GetWindowContentRegionWidth() - newColumnWidth.Value, 2));
                _lastColumnWidth = newColumnWidth.Value;
            }

            return true;
        }
    }
}

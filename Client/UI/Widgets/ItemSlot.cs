namespace Client.UI.Widgets;

using System;
using Raylib_cs;
using global::Client.UI.Rendering;

public class ItemSlot : Widget
{
    private float _cooldownRatio;

    public int SlotIndex { get; }
    public string ItemName { get; set; }
    public string AssetId { get; set; }
    public bool IsSelected { get; set; }

    public float CooldownRatio
    {
        get => _cooldownRatio;
        set => _cooldownRatio = Math.Clamp(value, 0.0f, 1.0f);
    }

    public ItemSlot(Rectangle bounds, IRenderer renderer, int slotIndex, string itemName, string assetId)
        : base(bounds, renderer)
    {
        SlotIndex = slotIndex;
        ItemName = itemName ?? string.Empty;
        AssetId = assetId ?? string.Empty;
        IsSelected = false;
        CooldownRatio = 0.0f;
    }

    protected override void Render()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        VisualRole frameRole = IsSelected ? VisualRole.SurfaceHighlight : VisualRole.Surface;
        Renderer.DrawSurface(Bounds, new VisualKey(frameRole));

        const float padding = 6.0f;
        float iconW = Math.Max(0.0f, Bounds.Width - (padding * 2.0f));
        float iconH = Math.Max(0.0f, Bounds.Height - (padding * 2.0f));

        if (!string.IsNullOrEmpty(AssetId) && iconW > 0 && iconH > 0)
        {
            Rectangle iconBounds = new(Bounds.X + padding, Bounds.Y + padding, iconW, iconH);
            Renderer.DrawSurface(iconBounds, new VisualKey(VisualRole.Graphic, AssetId));
        }

        if (CooldownRatio > 0.0f)
        {
            Renderer.DrawClippedSurface(Bounds, new VisualKey(VisualRole.OverlayShade), CooldownRatio, ClipAxis.Vertical);
        }

        if (SlotIndex > 0)
        {
            Rectangle hotkeyBounds = new(Bounds.X + 3.0f, Bounds.Y + 2.0f, 14.0f, 14.0f);
            Renderer.DrawText(hotkeyBounds, SlotIndex.ToString(), TextAlign.Left, TextSize.Small);
        }
    }
}

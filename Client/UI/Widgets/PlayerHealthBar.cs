namespace Client.UI.Widgets;

using System;
using global::Client.UI.Rendering;
using Raylib_cs;

public class PlayerHealthBar : ProgressBar
{
    public Func<Player?> PlayerSupplier { get; }
    public float WarningThresholdRatio { get; set; } = 0.25f;

    public PlayerHealthBar(Rectangle bounds, IRenderer renderer, Func<Player?> playerSupplier, VisualKey? backgroundKey = null, VisualKey? fillKey = null)
        : base(
            bounds,
            renderer,
            100f,
            100f,
            backgroundKey ?? new VisualKey(VisualRole.Surface),
            fillKey ?? new VisualKey(VisualRole.FillDanger),
            ClipAxis.Horizontal
        )
    {
        PlayerSupplier = playerSupplier;
    }

    public PlayerHealthBar(Rectangle bounds, IRenderer renderer, Func<Player?> playerSupplier)
        : this(bounds, renderer, playerSupplier, null, null) { }

    protected override void Render()
    {
        Player? player = null;
        try
        {
            player = PlayerSupplier?.Invoke();
        }
        catch
        {
            player = null;
        }

        if (player != null && player.Health > 0)
        {
            Value = Math.Max(0, player.Health);
            MaxValue = player.MaxHealth > 0 ? player.MaxHealth : 100f;
        }
        else
        {
            Value = 0f;
            MaxValue = (player != null && player.MaxHealth > 0) ? player.MaxHealth : 100f;
        }

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        float iconSize = Bounds.Height;
        float gap = 4.0f;
        Rectangle iconBounds = new(Bounds.X, Bounds.Y, iconSize, iconSize);

        float barX = Bounds.X + iconSize + gap;
        float barWidth = Math.Max(0.0f, Bounds.Width - iconSize - gap);
        Rectangle barBounds = new(barX, Bounds.Y, barWidth, Bounds.Height);

        float ratio = FillRatio;
        float threshold = Math.Clamp(WarningThresholdRatio, 0.0f, 1.0f);
        bool isLowHealth = ratio <= threshold;

        bool pulseActive = isLowHealth;
        try
        {
            pulseActive = isLowHealth && ((int)(Raylib.GetTime() * 4.0) % 2 == 0);
        }
        catch
        {
            pulseActive = isLowHealth;
        }

        if (pulseActive)
        {
            Renderer.DrawSurface(iconBounds, new VisualKey(VisualRole.SurfaceHighlight));
        }
        Renderer.DrawSurface(iconBounds, new VisualKey(VisualRole.Graphic, "Heart"));

        if (barBounds.Width > 0)
        {
            Renderer.DrawSurface(barBounds, BackgroundKey);
            if (ratio > 0.0f)
            {
                Renderer.DrawClippedSurface(barBounds, FillKey, ratio, Axis);
            }

            if (threshold > 0.0f && threshold < 1.0f)
            {
                float markerX = barBounds.X + barBounds.Width * threshold;
                Rectangle markerBounds = new(markerX - 1.0f, barBounds.Y, 2.0f, barBounds.Height);
                VisualRole markerRole = isLowHealth ? VisualRole.FillDanger : VisualRole.SurfaceHighlight;
                Renderer.DrawSurface(markerBounds, new VisualKey(markerRole));
            }

            Rectangle textBounds = barBounds;
            string text = $"HP: {(int)Value}/{(int)MaxValue}";
            Renderer.DrawText(textBounds, text, TextAlign.Center, TextSize.Small);
        }
    }
}

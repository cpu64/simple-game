namespace Client.UI.Rendering;

using System;
using Raylib_cs;

public abstract class RendererBase : IRenderer
{
    protected virtual float TextHorizontalPadding => 2.0f;

    protected virtual int MeasureTextWidth(string text, int fontSize) => Raylib.MeasureText(text, fontSize);

    public void DrawSurface(Rectangle bounds, VisualKey key)
    {
        if (!(bounds.Width > 0 && bounds.Height > 0))
        {
            return;
        }

        RenderSurface(bounds, key);
    }

    public void DrawClippedSurface(Rectangle bounds, VisualKey key, float ratio, ClipAxis axis)
    {
        if (!(bounds.Width > 0 && bounds.Height > 0))
        {
            return;
        }

        float clampedRatio = Math.Clamp(ratio, 0.0f, 1.0f);
        if (!(clampedRatio > 0.0f))
        {
            return;
        }

        Rectangle clipped = axis switch
        {
            ClipAxis.Horizontal => new Rectangle(bounds.X, bounds.Y, bounds.Width * clampedRatio, bounds.Height),
            ClipAxis.Vertical => key.Role == VisualRole.OverlayShade
                ? new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height * clampedRatio)
                : new Rectangle(bounds.X, bounds.Y + bounds.Height * (1.0f - clampedRatio), bounds.Width, bounds.Height * clampedRatio),
            _ => bounds,
        };

        if (!(clipped.Width > 0 && clipped.Height > 0))
        {
            return;
        }

        RenderClippedSurface(bounds, clipped, key, axis);
    }

    public void DrawText(Rectangle bounds, string text, TextAlign align, TextSize size)
    {
        if (string.IsNullOrEmpty(text) || !(bounds.Width > 0 && bounds.Height > 0))
        {
            return;
        }

        int fontSize = (int)size;
        int textWidth = MeasureTextWidth(text, fontSize);
        float padding = TextHorizontalPadding;

        float x = align switch
        {
            TextAlign.Left => bounds.X + padding,
            TextAlign.Center => bounds.X + (bounds.Width - textWidth) * 0.5f,
            TextAlign.Right => bounds.X + bounds.Width - textWidth - padding,
            _ => bounds.X,
        };
        float y = bounds.Y + (bounds.Height - fontSize) * 0.5f;

        RenderText(text, (int)Math.Round(x), (int)Math.Round(y), fontSize);
    }

    protected abstract void RenderSurface(Rectangle bounds, VisualKey key);

    protected abstract void RenderClippedSurface(Rectangle bounds, Rectangle clipped, VisualKey key, ClipAxis axis);

    protected abstract void RenderText(string text, int x, int y, int fontSize);
}

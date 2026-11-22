namespace Client.UI.Rendering;

using System;
using Raylib_cs;

public abstract class RendererBase : IRenderer // AbstractClass
{
    protected virtual float TextHorizontalPadding => 2.0f; // Hook

    protected virtual int MeasureTextWidth(string text, int fontSize) => Raylib.MeasureText(text, fontSize); // Hook

    public void DrawSurface(Rectangle bounds, VisualKey key) // TemplateMethod
    {
        if (!(bounds.Width > 0 && bounds.Height > 0))
        {
            return;
        }

        if (TryRenderAsset(bounds, key))
        {
            return;
        }

        RenderSurfaceFallback(bounds, key);
    }

    public void DrawClippedSurface(Rectangle bounds, VisualKey key, float ratio, ClipAxis axis) // TemplateMethod
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

        if (TryRenderClippedAsset(bounds, clipped, key))
        {
            return;
        }

        RenderClippedFill(clipped, key);
        RenderClipEdgeAccent(clipped, key, axis);
    }

    public void DrawText(Rectangle bounds, string text, TextAlign align, TextSize size) // TemplateMethod
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

        int ix = (int)Math.Round(x);
        int iy = (int)Math.Round(y);
        RenderTextShadow(text, ix, iy, fontSize);
        RenderTextForeground(text, ix, iy, fontSize);
    }

    protected virtual bool TryRenderAsset(Rectangle bounds, VisualKey key) => false; // Hook

    protected abstract void RenderSurfaceFallback(Rectangle bounds, VisualKey key); // PrimitiveOperation

    protected virtual bool TryRenderClippedAsset(Rectangle bounds, Rectangle clipped, VisualKey key) => false; // Hook

    protected abstract void RenderClippedFill(Rectangle clipped, VisualKey key); // PrimitiveOperation

    protected virtual void RenderClipEdgeAccent(Rectangle clipped, VisualKey key, ClipAxis axis) { } // Hook

    protected virtual void RenderTextShadow(string text, int x, int y, int fontSize) { } // Hook

    protected abstract void RenderTextForeground(string text, int x, int y, int fontSize); // PrimitiveOperation
}

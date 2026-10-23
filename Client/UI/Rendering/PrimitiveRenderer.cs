namespace Client.UI.Rendering;

using System;
using Raylib_cs;

public class PrimitiveRenderer : IRenderer
{
    private static readonly Color SurfaceFillColor = new(35, 35, 40, 255);
    private static readonly Color SurfaceWireColor = new(160, 160, 170, 255);

    private static readonly Color HighlightFillColor = new(45, 45, 55, 255);
    private static readonly Color HighlightWireColor = new(255, 215, 0, 255);

    private static readonly Color DangerFillColor = new(210, 35, 35, 255);
    private static readonly Color DangerWireColor = new(255, 120, 120, 255);

    private static readonly Color AccentFillColor = new(35, 135, 215, 255);
    private static readonly Color AccentWireColor = new(120, 200, 255, 255);

    private static readonly Color ShadeFillColor = new(0, 0, 0, 170);
    private static readonly Color ShadeWireColor = new(100, 100, 100, 160);

    private static readonly Color GraphicFillColor = new(25, 25, 30, 255);
    private static readonly Color GraphicWireColor = new(120, 120, 130, 255);
    private static readonly Color GraphicCrossColor = new(80, 80, 90, 255);
    private static readonly Color GraphicTextColor = new(255, 230, 100, 255);

    private static readonly Color DefaultTextColor = Color.White;

    public void DrawSurface(Rectangle bounds, VisualKey key)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        switch (key.Role)
        {
            case VisualRole.Surface:
                Raylib.DrawRectangleRec(bounds, SurfaceFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 1.0f, SurfaceWireColor);
                break;

            case VisualRole.SurfaceHighlight:
                Raylib.DrawRectangleRec(bounds, HighlightFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 2.0f, HighlightWireColor);
                break;

            case VisualRole.FillDanger:
                Raylib.DrawRectangleRec(bounds, DangerFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 1.0f, DangerWireColor);
                break;

            case VisualRole.FillAccent:
                Raylib.DrawRectangleRec(bounds, AccentFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 1.0f, AccentWireColor);
                break;

            case VisualRole.OverlayShade:
                Raylib.DrawRectangleRec(bounds, ShadeFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 1.0f, ShadeWireColor);
                break;

            case VisualRole.Graphic:
                Raylib.DrawRectangleRec(bounds, GraphicFillColor);
                Raylib.DrawRectangleLinesEx(bounds, 1.0f, GraphicWireColor);

                int x1 = (int)Math.Round(bounds.X);
                int y1 = (int)Math.Round(bounds.Y);
                int x2 = (int)Math.Round(bounds.X + bounds.Width);
                int y2 = (int)Math.Round(bounds.Y + bounds.Height);
                Raylib.DrawLine(x1, y1, x2, y2, GraphicCrossColor);
                Raylib.DrawLine(x1, y2, x2, y1, GraphicCrossColor);

                if (!string.IsNullOrEmpty(key.AssetId))
                {
                    string label = key.AssetId;
                    int fontSize = (int)TextSize.Small;
                    int textWidth = Raylib.MeasureText(label, fontSize);
                    if (textWidth > bounds.Width - 4 && label.Length > 3)
                    {
                        label = label[..3];
                        textWidth = Raylib.MeasureText(label, fontSize);
                    }
                    int textX = (int)Math.Round(bounds.X + (bounds.Width - textWidth) / 2.0f);
                    int textY = (int)Math.Round(bounds.Y + (bounds.Height - fontSize) / 2.0f);
                    Raylib.DrawText(label, textX, textY, fontSize, GraphicTextColor);
                }
                break;
        }
    }

    public void DrawClippedSurface(Rectangle bounds, VisualKey key, float ratio, ClipAxis axis)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float clampedRatio = Math.Clamp(ratio, 0.0f, 1.0f);
        if (clampedRatio <= 0.0f)
        {
            return;
        }

        Rectangle clipped = axis switch
        {
            ClipAxis.Horizontal => new Rectangle(
                bounds.X,
                bounds.Y,
                bounds.Width * clampedRatio,
                bounds.Height),

            ClipAxis.Vertical => key.Role == VisualRole.OverlayShade
                ? new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height * clampedRatio)
                : new Rectangle(bounds.X, bounds.Y + bounds.Height * (1.0f - clampedRatio), bounds.Width, bounds.Height * clampedRatio),

            _ => bounds
        };

        DrawSurface(clipped, key);
    }

    public void DrawText(Rectangle bounds, string text, TextAlign align, TextSize size)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        int fontSize = (int)size;
        int textWidth = Raylib.MeasureText(text, fontSize);

        int posY = (int)Math.Round(bounds.Y + (bounds.Height - fontSize) / 2.0f);

        int posX = align switch
        {
            TextAlign.Left => (int)Math.Round(bounds.X + 2.0f),
            TextAlign.Center => (int)Math.Round(bounds.X + (bounds.Width - textWidth) / 2.0f),
            TextAlign.Right => (int)Math.Round(bounds.X + bounds.Width - textWidth - 2.0f),
            _ => (int)Math.Round(bounds.X)
        };

        Raylib.DrawText(text, posX, posY, fontSize, DefaultTextColor);
    }
}

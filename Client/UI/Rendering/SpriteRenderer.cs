namespace Client.UI.Rendering;

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

public sealed class SpriteRenderer : IRenderer, IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingAssets = new(StringComparer.OrdinalIgnoreCase);

    public void DrawSurface(Rectangle bounds, VisualKey key)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (TryGetTexture(key.AssetId, out Texture2D texture))
        {
            DrawLoadedTexture(texture, bounds);
            return;
        }

        DrawSurfaceFallback(bounds, key);
    }

    public void DrawClippedSurface(Rectangle bounds, VisualKey key, float ratio, ClipAxis axis)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float clamped = Math.Clamp(ratio, 0.0f, 1.0f);
        if (clamped <= 0.0f)
        {
            return;
        }

        Rectangle clipped = axis switch
        {
            ClipAxis.Horizontal => new Rectangle(bounds.X, bounds.Y, bounds.Width * clamped, bounds.Height),

            ClipAxis.Vertical => key.Role == VisualRole.OverlayShade
                ? new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height * clamped)
                : new Rectangle(bounds.X, bounds.Y + bounds.Height * (1.0f - clamped), bounds.Width, bounds.Height * clamped),

            _ => bounds,
        };

        if (clipped.Width <= 0 || clipped.Height <= 0)
        {
            return;
        }

        if (TryGetTexture(key.AssetId, out Texture2D texture))
        {
            DrawClippedTexture(texture, bounds, clipped);
            return;
        }

        DrawClippedSurfaceFallback(clipped, key, axis);
    }

    public void DrawText(Rectangle bounds, string text, TextAlign align, TextSize size)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        int fontSize = (int)size;
        int textWidth = Raylib.MeasureText(text, fontSize);

        float x = align switch
        {
            TextAlign.Left => bounds.X + 4.0f,
            TextAlign.Center => bounds.X + (bounds.Width - textWidth) * 0.5f,
            TextAlign.Right => bounds.X + bounds.Width - textWidth - 4.0f,
            _ => bounds.X,
        };

        float y = bounds.Y + (bounds.Height - fontSize) * 0.5f;

        Raylib.DrawText(text, (int)Math.Round(x) + 1, (int)Math.Round(y) + 1, fontSize, new Color(15, 15, 20, 230));
        Raylib.DrawText(text, (int)Math.Round(x), (int)Math.Round(y), fontSize, new Color(245, 245, 250, 255));
    }

    public void Dispose()
    {
        foreach (Texture2D tex in _textures.Values)
        {
            if (tex.Id > 0)
            {
                Raylib.UnloadTexture(tex);
            }
        }
        _textures.Clear();
        _missingAssets.Clear();
    }

    private bool TryGetTexture(string assetId, out Texture2D texture)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            texture = default;
            return false;
        }

        if (_textures.TryGetValue(assetId, out texture))
        {
            return texture.Id > 0;
        }

        if (_missingAssets.Contains(assetId))
        {
            texture = default;
            return false;
        }

        string[] searchPaths =
        [
            assetId,
            $"{assetId}.png",
            Path.Combine("Assets", $"{assetId}.png"),
            Path.Combine("assets", $"{assetId}.png"),
            Path.Combine("Client", "Assets", $"{assetId}.png"),
        ];

        foreach (string path in searchPaths)
        {
            if (File.Exists(path))
            {
                Texture2D loaded = Raylib.LoadTexture(path);
                if (loaded.Id > 0)
                {
                    _textures[assetId] = loaded;
                    texture = loaded;
                    return true;
                }
            }
        }

        _missingAssets.Add(assetId);
        texture = default;
        return false;
    }

    private static void DrawLoadedTexture(Texture2D texture, Rectangle bounds)
    {
        Rectangle source = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, bounds, Vector2.Zero, 0.0f, Color.White);
    }

    private static void DrawClippedTexture(Texture2D texture, Rectangle bounds, Rectangle clip)
    {
        Raylib.BeginScissorMode((int)clip.X, (int)clip.Y, (int)clip.Width, (int)clip.Height);
        Rectangle source = new(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, bounds, Vector2.Zero, 0.0f, Color.White);
        Raylib.EndScissorMode();
    }

    private static void DrawSurfaceFallback(Rectangle bounds, VisualKey key)
    {
        switch (key.Role)
        {
            case VisualRole.Surface:
                DrawSurfaceStyled(bounds);
                break;

            case VisualRole.SurfaceHighlight:
                DrawSurfaceHighlightStyled(bounds);
                break;

            case VisualRole.FillDanger:
                DrawFillDangerStyled(bounds);
                break;

            case VisualRole.FillAccent:
                DrawFillAccentStyled(bounds);
                break;

            case VisualRole.OverlayShade:
                DrawOverlayShadeStyled(bounds);
                break;

            case VisualRole.Graphic:
                DrawGraphicStyled(bounds, key.AssetId);
                break;
        }
    }

    private static void DrawSurfaceStyled(Rectangle bounds)
    {
        Rectangle shadow = new(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height);
        Raylib.DrawRectangleRounded(shadow, 0.18f, 4, new Color(0, 0, 0, 95));
        Raylib.DrawRectangleRounded(bounds, 0.18f, 4, new Color(32, 38, 48, 240));

        int ix = (int)bounds.X + 2;
        int iy = (int)bounds.Y + 2;
        int iw = Math.Max(1, (int)bounds.Width - 4);
        int ih = Math.Max(1, (int)bounds.Height - 4);
        Raylib.DrawRectangleGradientV(ix, iy, iw, ih, new Color(48, 56, 70, 255), new Color(22, 26, 34, 255));

        Raylib.DrawLine(ix + 1, iy, ix + iw - 1, iy, new Color(90, 105, 130, 200));
        Raylib.DrawRectangleRoundedLinesEx(bounds, 0.18f, 4, 1.5f, new Color(75, 90, 115, 255));
    }

    private static void DrawSurfaceHighlightStyled(Rectangle bounds)
    {
        Rectangle shadow = new(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height);
        Raylib.DrawRectangleRounded(shadow, 0.18f, 4, new Color(40, 25, 5, 140));
        Raylib.DrawRectangleRounded(bounds, 0.18f, 4, new Color(55, 45, 20, 245));

        int ix = (int)bounds.X + 2;
        int iy = (int)bounds.Y + 2;
        int iw = Math.Max(1, (int)bounds.Width - 4);
        int ih = Math.Max(1, (int)bounds.Height - 4);
        Raylib.DrawRectangleGradientV(ix, iy, iw, ih, new Color(85, 70, 30, 255), new Color(40, 32, 14, 255));

        Raylib.DrawLine(ix + 1, iy, ix + iw - 1, iy, new Color(255, 235, 140, 220));
        Raylib.DrawRectangleRoundedLinesEx(bounds, 0.18f, 4, 2.0f, new Color(255, 205, 40, 255));
    }

    private static void DrawFillDangerStyled(Rectangle bounds)
    {
        Raylib.DrawRectangleGradientV(
            (int)bounds.X,
            (int)bounds.Y,
            (int)bounds.Width,
            (int)bounds.Height,
            new Color(220, 40, 60, 255),
            new Color(120, 15, 30, 255)
        );
        Raylib.DrawLine((int)bounds.X, (int)bounds.Y, (int)(bounds.X + bounds.Width), (int)bounds.Y, new Color(255, 160, 180, 220));
        Raylib.DrawRectangleLinesEx(bounds, 1.0f, new Color(160, 30, 45, 255));
    }

    private static void DrawFillAccentStyled(Rectangle bounds)
    {
        Raylib.DrawRectangleGradientV(
            (int)bounds.X,
            (int)bounds.Y,
            (int)bounds.Width,
            (int)bounds.Height,
            new Color(50, 150, 250, 255),
            new Color(20, 50, 140, 255)
        );
        Raylib.DrawLine((int)bounds.X, (int)bounds.Y, (int)(bounds.X + bounds.Width), (int)bounds.Y, new Color(160, 220, 255, 220));
        Raylib.DrawRectangleLinesEx(bounds, 1.0f, new Color(40, 100, 200, 255));
    }

    private static void DrawOverlayShadeStyled(Rectangle bounds)
    {
        Raylib.DrawRectangleGradientV(
            (int)bounds.X,
            (int)bounds.Y,
            (int)bounds.Width,
            (int)bounds.Height,
            new Color(15, 18, 25, 160),
            new Color(5, 8, 12, 210)
        );
    }

    private static void DrawClippedSurfaceFallback(Rectangle clipped, VisualKey key, ClipAxis axis)
    {
        switch (key.Role)
        {
            case VisualRole.FillDanger:
                Raylib.DrawRectangleGradientV(
                    (int)clipped.X,
                    (int)clipped.Y,
                    (int)clipped.Width,
                    (int)clipped.Height,
                    new Color(235, 45, 70, 255),
                    new Color(130, 12, 28, 255)
                );
                int highH = Math.Max(2, (int)(clipped.Height * 0.35f));
                Raylib.DrawRectangleGradientV(
                    (int)clipped.X,
                    (int)clipped.Y,
                    (int)clipped.Width,
                    highH,
                    new Color(255, 140, 160, 170),
                    new Color(235, 45, 70, 0)
                );
                Raylib.DrawLine((int)clipped.X, (int)clipped.Y, (int)(clipped.X + clipped.Width), (int)clipped.Y, new Color(255, 200, 215, 220));
                if (axis == ClipAxis.Horizontal && clipped.Width >= 2)
                {
                    int edgeX = (int)(clipped.X + clipped.Width - 1);
                    Raylib.DrawLine(edgeX, (int)clipped.Y, edgeX, (int)(clipped.Y + clipped.Height), new Color(255, 210, 225, 230));
                }
                break;

            case VisualRole.FillAccent:
                Raylib.DrawRectangleGradientV(
                    (int)clipped.X,
                    (int)clipped.Y,
                    (int)clipped.Width,
                    (int)clipped.Height,
                    new Color(60, 160, 255, 255),
                    new Color(18, 55, 150, 255)
                );
                int highAccentH = Math.Max(2, (int)(clipped.Height * 0.35f));
                Raylib.DrawRectangleGradientV(
                    (int)clipped.X,
                    (int)clipped.Y,
                    (int)clipped.Width,
                    highAccentH,
                    new Color(180, 225, 255, 170),
                    new Color(60, 160, 255, 0)
                );
                Raylib.DrawLine((int)clipped.X, (int)clipped.Y, (int)(clipped.X + clipped.Width), (int)clipped.Y, new Color(210, 240, 255, 220));
                if (axis == ClipAxis.Horizontal && clipped.Width >= 2)
                {
                    int edgeX = (int)(clipped.X + clipped.Width - 1);
                    Raylib.DrawLine(edgeX, (int)clipped.Y, edgeX, (int)(clipped.Y + clipped.Height), new Color(220, 245, 255, 230));
                }
                break;

            case VisualRole.OverlayShade:
                Raylib.DrawRectangleGradientV(
                    (int)clipped.X,
                    (int)clipped.Y,
                    (int)clipped.Width,
                    (int)clipped.Height,
                    new Color(10, 12, 18, 150),
                    new Color(5, 6, 10, 210)
                );
                float lineY = clipped.Y + clipped.Height - 1;
                Raylib.DrawLineEx(new Vector2(clipped.X, lineY), new Vector2(clipped.X + clipped.Width, lineY), 1.5f, new Color(255, 255, 255, 140));
                break;

            default:
                DrawSurfaceFallback(clipped, key);
                break;
        }
    }

    private static void DrawGraphicStyled(Rectangle bounds, string assetId)
    {
        if (string.Equals(assetId, "Heart", StringComparison.OrdinalIgnoreCase))
        {
            DrawHeartIcon(bounds);
        }
        else if (string.Equals(assetId, "Pickaxe", StringComparison.OrdinalIgnoreCase))
        {
            DrawPickaxeIcon(bounds);
        }
        else if (string.Equals(assetId, "Sword", StringComparison.OrdinalIgnoreCase))
        {
            DrawSwordIcon(bounds);
        }
        else if (
            string.Equals(assetId, "Platforms", StringComparison.OrdinalIgnoreCase) || string.Equals(assetId, "Platform", StringComparison.OrdinalIgnoreCase)
        )
        {
            DrawPlatformsIcon(bounds);
        }
        else if (string.Equals(assetId, "Ropes", StringComparison.OrdinalIgnoreCase) || string.Equals(assetId, "Rope", StringComparison.OrdinalIgnoreCase))
        {
            DrawRopesIcon(bounds);
        }
        else
        {
            DrawDefaultGemIcon(bounds);
        }
    }

    private static void DrawHeartIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float cy = bounds.Y + bounds.Height * 0.5f;
        float span = Math.Min(bounds.Width, bounds.Height) * 0.42f;
        float r = span * 0.45f;
        Color ruby = new(225, 30, 55, 255);

        Raylib.DrawCircle((int)(cx - span * 0.4f), (int)(cy - span * 0.25f), r, ruby);
        Raylib.DrawCircle((int)(cx + span * 0.4f), (int)(cy - span * 0.25f), r, ruby);

        Vector2 pLeft = new(cx - span * 0.85f, cy - span * 0.10f);
        Vector2 pRight = new(cx + span * 0.85f, cy - span * 0.10f);
        Vector2 pBottom = new(cx, cy + span * 0.95f);
        Raylib.DrawTriangle(pLeft, pBottom, pRight, ruby);

        Raylib.DrawCircle((int)(cx - span * 0.48f), (int)(cy - span * 0.38f), span * 0.14f, new Color(255, 180, 200, 220));
    }

    private static void DrawPickaxeIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float cy = bounds.Y + bounds.Height * 0.5f;
        float span = Math.Min(bounds.Width, bounds.Height) * 0.36f;

        Vector2 shaftStart = new(cx - span * 0.85f, cy + span * 0.85f);
        Vector2 shaftEnd = new(cx + span * 0.55f, cy - span * 0.55f);
        Raylib.DrawLineEx(shaftStart, shaftEnd, 3.0f, new Color(130, 80, 40, 255));
        Raylib.DrawLineEx(shaftStart, shaftEnd, 1.2f, new Color(180, 120, 65, 255));

        Vector2 headCenter = new(cx + span * 0.40f, cy - span * 0.40f);
        Vector2 leftTip = new(cx - span * 0.15f, cy - span * 0.95f);
        Vector2 rightTip = new(cx + span * 0.95f, cy + span * 0.15f);

        Raylib.DrawLineEx(leftTip, headCenter, 3.5f, new Color(175, 195, 215, 255));
        Raylib.DrawLineEx(headCenter, rightTip, 3.5f, new Color(175, 195, 215, 255));

        Raylib.DrawCircle((int)leftTip.X, (int)leftTip.Y, 1.5f, Color.White);
        Raylib.DrawCircle((int)rightTip.X, (int)rightTip.Y, 1.5f, Color.White);
        Raylib.DrawCircle((int)headCenter.X, (int)headCenter.Y, 2.5f, new Color(70, 75, 85, 255));
    }

    private static void DrawSwordIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float cy = bounds.Y + bounds.Height * 0.5f;
        float span = Math.Min(bounds.Width, bounds.Height) * 0.38f;

        Vector2 pommel = new(cx - span * 0.80f, cy + span * 0.80f);
        Vector2 guardCenter = new(cx - span * 0.35f, cy + span * 0.35f);
        Vector2 bladeTip = new(cx + span * 0.85f, cy - span * 0.85f);

        Raylib.DrawCircle((int)pommel.X, (int)pommel.Y, 3.0f, new Color(240, 190, 40, 255));
        Raylib.DrawLineEx(pommel, guardCenter, 3.0f, new Color(100, 60, 30, 255));

        Vector2 g1 = new(guardCenter.X - span * 0.28f, guardCenter.Y - span * 0.28f);
        Vector2 g2 = new(guardCenter.X + span * 0.28f, guardCenter.Y + span * 0.28f);
        Raylib.DrawLineEx(g1, g2, 3.0f, new Color(240, 190, 40, 255));

        Raylib.DrawLineEx(guardCenter, bladeTip, 4.0f, new Color(195, 215, 235, 255));
        Raylib.DrawLineEx(guardCenter, bladeTip, 1.5f, Color.White);
        Raylib.DrawCircle((int)bladeTip.X, (int)bladeTip.Y, 1.8f, Color.White);
    }

    private static void DrawPlatformsIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float cy = bounds.Y + bounds.Height * 0.5f;
        float pw = bounds.Width * 0.55f;
        float ph = Math.Max(4.0f, bounds.Height * 0.12f);

        Rectangle plank1 = new(cx - pw * 0.60f, cy - ph * 1.6f, pw, ph);
        Raylib.DrawRectangleRounded(plank1, 0.4f, 2, new Color(165, 115, 65, 255));
        Raylib.DrawLine((int)plank1.X + 2, (int)plank1.Y + 1, (int)(plank1.X + plank1.Width - 2), (int)plank1.Y + 1, new Color(210, 160, 105, 255));

        Rectangle plank2 = new(cx - pw * 0.40f, cy + ph * 0.6f, pw, ph);
        Raylib.DrawRectangleRounded(plank2, 0.4f, 2, new Color(145, 95, 50, 255));
        Raylib.DrawLine((int)plank2.X + 2, (int)plank2.Y + 1, (int)(plank2.X + plank2.Width - 2), (int)plank2.Y + 1, new Color(190, 140, 90, 255));

        Raylib.DrawLineEx(new Vector2(plank1.X + 6, plank1.Y + plank1.Height), new Vector2(plank2.X + 6, plank2.Y), 2.0f, new Color(115, 75, 40, 255));
    }

    private static void DrawRopesIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float topY = bounds.Y + bounds.Height * 0.15f;
        float botY = bounds.Y + bounds.Height * 0.85f;

        Raylib.DrawLineEx(new Vector2(cx, topY), new Vector2(cx, botY), 3.5f, new Color(160, 120, 65, 255));

        for (float y = topY + 3.0f; y < botY - 3.0f; y += 5.0f)
        {
            Raylib.DrawLineEx(new Vector2(cx - 3.5f, y - 2.0f), new Vector2(cx + 3.5f, y + 2.0f), 1.8f, new Color(220, 180, 115, 255));
        }

        Raylib.DrawCircle((int)cx, (int)botY - 2, 3.0f, new Color(140, 100, 50, 255));
        Raylib.DrawLineEx(new Vector2(cx - 2, botY), new Vector2(cx - 3, botY + 4), 1.2f, new Color(200, 160, 100, 255));
        Raylib.DrawLineEx(new Vector2(cx + 2, botY), new Vector2(cx + 3, botY + 4), 1.2f, new Color(200, 160, 100, 255));
    }

    private static void DrawDefaultGemIcon(Rectangle bounds)
    {
        float cx = bounds.X + bounds.Width * 0.5f;
        float cy = bounds.Y + bounds.Height * 0.5f;
        float rad = Math.Min(bounds.Width, bounds.Height) * 0.32f;

        Vector2 top = new(cx, cy - rad);
        Vector2 right = new(cx + rad, cy);
        Vector2 bottom = new(cx, cy + rad);
        Vector2 left = new(cx - rad, cy);

        Raylib.DrawTriangle(top, left, bottom, new Color(60, 180, 240, 255));
        Raylib.DrawTriangle(top, bottom, right, new Color(30, 130, 200, 255));
        Raylib.DrawCircle((int)cx, (int)(cy - rad * 0.3f), rad * 0.25f, new Color(230, 250, 255, 230));
    }
}

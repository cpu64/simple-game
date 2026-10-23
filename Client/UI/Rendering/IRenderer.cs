namespace Client.UI.Rendering;

using Raylib_cs;

public interface IRenderer
{
    void DrawSurface(Rectangle bounds, VisualKey key);
    void DrawClippedSurface(Rectangle bounds, VisualKey key, float ratio, ClipAxis axis);
    void DrawText(Rectangle bounds, string text, TextAlign align, TextSize size);
}

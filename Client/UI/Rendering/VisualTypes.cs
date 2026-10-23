namespace Client.UI.Rendering;

public enum VisualRole
{
    Surface,
    SurfaceHighlight,
    FillDanger,
    FillAccent,
    OverlayShade,
    Graphic
}

public enum ClipAxis
{
    Horizontal,
    Vertical
}

public enum TextAlign
{
    Left,
    Center,
    Right
}

public enum TextSize
{
    Small = 12,
    Medium = 16,
    Large = 20
}

public readonly record struct VisualKey(VisualRole Role, string AssetId = "");

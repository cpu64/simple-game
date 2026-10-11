using System;
using System.Numerics;
using Raylib_cs;

class RenderRectangle : RenderEntity
{
    private readonly Vector2 size;
    private readonly Color color;

    public RenderRectangle(Vector2 previousPosition, Vector2 currentPosition, Vector2 size, Color color)
        : base(previousPosition, currentPosition)
    {
        this.size = size;
        this.color = color;
    }

    public override void Render(float alpha)
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        var position = InterpolatedPosition(alpha);
        Vector2 topLeft = position * 20 - size * 0.5f;

        Raylib.DrawRectangle((int)Math.Round(topLeft.X), (int)Math.Round(topLeft.Y), (int)Math.Round(size.X), (int)Math.Round(size.Y), color);
    }
}

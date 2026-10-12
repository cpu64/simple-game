using System.Numerics;

public abstract class RenderEntity
{
    public Vector2 PreviousPosition;
    public Vector2 CurrentPosition;

    protected RenderEntity(Vector2 previousPosition, Vector2 currentPosition)
    {
        PreviousPosition = previousPosition;
        CurrentPosition = currentPosition;
    }

    protected Vector2 InterpolatedPosition(float alpha) => Vector2.Lerp(PreviousPosition, CurrentPosition, alpha);

    public abstract void Render(float alpha);
}

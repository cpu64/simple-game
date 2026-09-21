using System.Numerics;

public interface IKinematicBody
{
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    Vector2 Size { get; }
    float GravityScale => 1.0f;
    float Drag => 0.85f;
    bool CollidesWithBlocks => true;
}

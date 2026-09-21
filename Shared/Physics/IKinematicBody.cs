using System.Numerics;

public interface IKinematicBody
{
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    Vector2 Size { get; }
    float GravityScale => 1.0f;
    float Drag => 1.0f;
    bool CollidesWithBlocks => true;
}

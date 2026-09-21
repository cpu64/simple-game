using System.Numerics;

public readonly struct TargetInfo
{
    public readonly EntityId Id;
    public readonly Vector2 Offset;
    public readonly float DistanceSquared;

    public TargetInfo(EntityId id, Vector2 offset, float distanceSquared)
    {
        Id = id;
        Offset = offset;
        DistanceSquared = distanceSquared;
    }
}

public readonly struct AISenses
{
    public readonly bool IsGrounded;
    public readonly bool HitHorizontal;
    public readonly bool HitCeiling;
    public readonly bool ObstacleAhead;
    public readonly TargetInfo? NearestTarget;

    public bool HitWall => HitHorizontal;

    public AISenses(bool isGrounded, bool hitHorizontal, bool hitCeiling, bool obstacleAhead, TargetInfo? nearestTarget)
    {
        IsGrounded = isGrounded;
        HitHorizontal = hitHorizontal;
        HitCeiling = hitCeiling;
        ObstacleAhead = obstacleAhead;
        NearestTarget = nearestTarget;
    }
}

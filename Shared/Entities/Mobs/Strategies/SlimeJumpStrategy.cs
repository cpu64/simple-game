using System;
using System.Numerics;

public class SlimeJumpStrategy : IMobMovementStrategy
{
    public void UpdateMovement(Mob mob, World world)
    {
        if (mob is not Slime slime)
            return;

        bool isGrounded = CollisionService.IsGrounded(slime.Position, slime.CollisionSize, world.Blocks);

        Player? target = AiSystem.FindNearestPlayer(world, slime.Position, Slime.DetectionRange);

        if (target != null)
        {
            Vector2 offset = target.Position - slime.Position;

            if (offset.X != 0.0f)
                slime.Rotation = offset.X >= 0.0f ? 0.0f : MathF.PI;
        }

        if (slime.JumpTimer > 0)
        {
            slime.JumpTimer--;
            return;
        }

        if (!isGrounded)
            return;

        if (target != null)
        {
            Vector2 offset = target.Position - slime.Position;

            float direction = offset.X >= 0.0f ? 1.0f : -1.0f;
            float horizontalDistance = MathF.Abs(offset.X);

            bool obstacleAhead = AiSystem.IsObstacleAhead(slime.Position, slime.CollisionSize, direction, world.Blocks);

            if (!obstacleAhead && horizontalDistance < 3.5f && offset.Y >= -0.5f)
            {
                slime.Velocity = new Vector2(Slime.WeakJumpVelocity.X * direction, Slime.WeakJumpVelocity.Y);
            }
            else
            {
                slime.Velocity = new Vector2(Slime.JumpVelocity.X * direction, Slime.JumpVelocity.Y);
            }
        }
        else
        {
            float facingDirection = slime.Rotation >= 0.0f && slime.Rotation < MathF.PI ? 1.0f : -1.0f;
            slime.Velocity = new Vector2(facingDirection * 2.0f, -4.5f);
        }

        slime.JumpTimer = Slime.JumpIntervalTicks;
    }
}

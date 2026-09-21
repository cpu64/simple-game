using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlayerSystem
{
    private const float Speed = 8.0f;
    private const float Gravity = 10.0f;
    private const float Width = 1.0f;
    private const float Height = 1.0f;
    private const float SimpleBulletCooldown = 0.2f;
    private const float HeavyBulletCooldown = 0.35f;

    public static void ProcessCommands(World world, List<InputCommand> commands, bool isReplay = false)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        // Decrement timers for all players each tick independently of input commands
        foreach (Player player in world.Entities.OfType<Player>())
        {
            if (player.InvulnerabilityTimer > 0)
            {
                player.InvulnerabilityTimer = Math.Max(0, player.InvulnerabilityTimer - dt);
            }

            if (player.AttackCooldownTimer > 0)
            {
                player.AttackCooldownTimer = Math.Max(0, player.AttackCooldownTimer - dt);
            }
        }

        HashSet<Guid> commandedPlayers = new HashSet<Guid>();

        foreach (InputCommand command in commands)
        {
            Player? player = world.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == command.PlayerId);

            if (player == null || player.IsDead || player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;
            commandedPlayers.Add(player.UserId);

            StepPlayer(player, command.Keys, command.Pointer, dt, world, isReplay);
        }

        // Advance physics for idle players who sent no command this tick
        foreach (Player player in world.Entities.OfType<Player>())
        {
            if (commandedPlayers.Contains(player.UserId) || player.IsDead)
                continue;

            StepPlayer(player, KeyState.None, Vector2.Zero, dt, world, isReplay: true);
        }
    }

    private static void StepPlayer(Player player, KeyState keys, Vector2 pointer, float dt, World world, bool isReplay)
    {
        float moveX = 0f;
        if ((keys & KeyState.Left) != 0)
            moveX -= Speed * dt;
        if ((keys & KeyState.Right) != 0)
            moveX += Speed * dt;

        float moveY = 0f;
        if ((keys & KeyState.Up) != 0)
            moveY -= Speed * dt;

        // Vertical gravity accelerates player's physics velocity
        float velY = player.Velocity.Y + Gravity * dt;
        float velX = player.Velocity.X * 0.88f;

        Vector2 totalMovement = new Vector2(moveX, moveY) + new Vector2(velX, velY) * dt;

        MovementResult result = PhysicsSystem.MoveDetailed(player.Position, Width, Height, totalMovement, world.Blocks);
        player.Position = result.Position;

        if (result.HitFloor && velY > 0)
            velY = 0f;
        else if (result.HitCeiling && velY < 0)
            velY = 0f;

        if (result.HitHorizontal)
            velX = 0f;

        if (Math.Abs(velX) < 0.05f)
            velX = 0f;

        player.Velocity = new Vector2(velX, velY);

        if (!isReplay && player.AttackCooldownTimer <= 0)
        {
            if ((keys & KeyState.MouseLeft) != 0)
            {
                ProjectileSystem.SpawnSimpleBullet(world, player, pointer);
                player.AttackCooldownTimer = SimpleBulletCooldown;
            }
            else if ((keys & KeyState.MouseRight) != 0)
            {
                ProjectileSystem.SpawnHeavyBullet(world, player, pointer);
                player.AttackCooldownTimer = HeavyBulletCooldown;
            }
        }
    }
}

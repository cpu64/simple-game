using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlayerSystem
{
    private const float Speed = 8.0f;
    private const float SimpleBulletCooldown = 0.2f;
    private const float HeavyBulletCooldown = 0.35f;

    public static void ProcessCommands(World world, List<InputCommand> commands)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        // Decrement timers for all players each tick independently of input commands
        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Player p)
                continue;

            if (p.InvulnerabilityTimer > 0)
            {
                p.InvulnerabilityTimer = Math.Max(0, p.InvulnerabilityTimer - dt);
            }

            if (p.AttackCooldownTimer > 0)
            {
                p.AttackCooldownTimer = Math.Max(0, p.AttackCooldownTimer - dt);
            }
        }

        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Player player || player.IsDead)
                continue;

            bool hadCommand = false;
            while (true)
            {
                int nextIndex = -1;
                long nextSequence = long.MaxValue;

                for (int c = 0; c < commands.Count; c++)
                {
                    InputCommand cmd = commands[c];
                    if (cmd.PlayerId == player.UserId && cmd.Sequence > player.LastCommand && cmd.Sequence < nextSequence)
                    {
                        nextSequence = cmd.Sequence;
                        nextIndex = c;
                    }
                }

                if (nextIndex == -1)
                    break;

                hadCommand = true;
                InputCommand command = commands[nextIndex];
                player.LastCommand = command.Sequence;

                Vector2 inputMovement = ComputeInputMovement(command.Keys, dt);
                PhysicsSystem.StepBody(player, world.Blocks, dt, inputMovement);

                ProcessWeaponFiring(world, player, command);
            }

            if (!hadCommand)
            {
                PhysicsSystem.StepBody(player, world.Blocks, dt, Vector2.Zero);
            }
        }
    }

    public static void ProcessWeaponFiring(World world, Player player, InputCommand command)
    {
        if (player.AttackCooldownTimer <= 0)
        {
            if ((command.Keys & KeyState.MouseLeft) != 0)
            {
                ProjectileSystem.SpawnSimpleBullet(world, player, command.Pointer);
                player.AttackCooldownTimer = SimpleBulletCooldown;
            }
            else if ((command.Keys & KeyState.MouseRight) != 0)
            {
                ProjectileSystem.SpawnHeavyBullet(world, player, command.Pointer);
                player.AttackCooldownTimer = HeavyBulletCooldown;
            }
        }
    }

    public static Vector2 ComputeInputMovement(KeyState keys, float dt)
    {
        float moveX = 0f;
        if ((keys & KeyState.Left) != 0)
            moveX -= Speed * dt;
        if ((keys & KeyState.Right) != 0)
            moveX += Speed * dt;

        float moveY = 0f;
        if ((keys & KeyState.Up) != 0)
            moveY -= Speed * dt;

        return new Vector2(moveX, moveY);
    }
}

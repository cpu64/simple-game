# Netcode

- **Local server:** predicts the world and provides the state shown to the player.
- **Remote server:** authoritative world state.
- **Command:** player input with a local tick and a sequence number (monotonically increasing identifier owned by the player who created the command).
- **Snapshot:** authoritative world state at `world.Tick`.
- **Snapshot rate** remote server sends a snapshot every `N` ticks.
- **Interpolation buffer:** initially displays other players `N + 5` ticks behind the latest authoritative world, then catches up to a 5-tick delay.

## Flow

The local server gets the latest world from the remote server, or uses the last simulated world.

It gets the current input state and creates a command using `world.Tick + 1`.

It applies the command immediately to produce the next world, where `newWorld.Tick = world.Tick + 1`.

It sends every command to the remote server.

The remote server applies each command when it receives it, even if the command was created in the previous tick.

## Snapshots

A snapshot contains:

- **Current state:** the complete authoritative world at `world.Tick`.
- **Last player command sequence number:** so the local server know which commands to reapply.

## Reconciliation

The client retains the authoritative worlds needed for interpolation. With `N = 20`, three worlds are sufficient to maintain an `N + 5 = 25` tick interpolation delay.

When a new snapshot arrives:

1. Use the newest authoritative world as the latest known state.
2. Apply all missing local commands, identified by their sequence numbers.
3. Initially display other players `N + 5` ticks behind the newest authoritative world.
4. Interpolate other-player positions between the two authoritative worlds surrounding the display tick.
5. As local ticks advance, the display catches up until it is 5 ticks behind the newest authoritative world.
6. When the next snapshot arrives, continue interpolating toward it.

For example, when world `1000` arrives, the display starts at tick `975`. Over the next 20 ticks it catches up toward tick `995`. In the happy path, world `1020` arrives just as the display reaches `995`, maintaining the 5-tick buffer.

If the next snapshot is delayed by up to 5 ticks, the interpolation buffer absorbs the delay.

## Invariant

**Local state = latest authoritative world + all unacknowledged local commands replayed in order.**

**Other-player render state = interpolated state between authoritative snapshots.**

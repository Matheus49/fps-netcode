﻿using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Priority_Queue;

// Simple structure representing a particular players inputs at a world tick.
public struct TickInput {
  public uint WorldTick;
  public Player Player;
  public PlayerInputs Inputs;
}

// Processes input network commands from a set of players and presents them in
// a way to the simulation which is easier to interact with.
public class PlayerInputProcessor {
  private SimplePriorityQueue<TickInput> queue = new SimplePriorityQueue<TickInput>();

  // Monitoring.
  private const int QUEUE_SIZE_AVERAGE_WINDOW = 10;
  private int[] playerInputQueueSizes = new int[QUEUE_SIZE_AVERAGE_WINDOW];
  private int playerInputQueueSizesIdx;
  private int maxInputArraySize;
  private int staleInputs;

  public void LogQueueStatsForPlayer(Player player) {
    int count = 0;
    foreach (var entry in queue) {
      if (entry.Player.PlayerId == player.PlayerId) {
        count++;
      }
    }
    // TODO: Moving average should go into a delegate.
    playerInputQueueSizesIdx = (playerInputQueueSizesIdx + 1) % QUEUE_SIZE_AVERAGE_WINDOW;
    playerInputQueueSizes[playerInputQueueSizesIdx] = count;
    DebugUI.ShowValue(
        "avg input queue", playerInputQueueSizes.Sum() / playerInputQueueSizes.Length);
  }

  public List<TickInput> DequeueInputsForTick(uint worldTick) {
    var ret = new List<TickInput>();
    TickInput entry;
    while (queue.TryDequeue(out entry)) {
      if (entry.WorldTick < worldTick) {
      } else if (entry.WorldTick == worldTick) {
        ret.Add(entry);
      } else {
        // We dequeued a future input, put it back in.
        queue.Enqueue(entry, entry.WorldTick);
        break;
      }
    }
    return ret;
  }

  public void EnqueueInput(NetCommand.PlayerInput command, Player player, uint serverWorldTick) {
    // Monitoring.
    if (command.Inputs.Length > maxInputArraySize) {
      maxInputArraySize = command.Inputs.Length;
    }
    DebugUI.ShowValue("max # inputs", maxInputArraySize);
    DebugUI.ShowValue("stale inputs", staleInputs);

    // Calculate the last tick in the incoming command.
    uint maxTick = command.StartWorldTick + (uint)command.Inputs.Length - 1;

    // Scan for inputs which haven't been handled yet.
    if (maxTick >= serverWorldTick) {
      uint start = serverWorldTick > command.StartWorldTick
          ? serverWorldTick - command.StartWorldTick : 0;
      for (int i = (int)start; i < command.Inputs.Length; ++i) {
        // Apply inputs to the associated player controller and simulate the world.
        var worldTick = command.StartWorldTick + i;
        var pinput = new TickInput {
            WorldTick = (uint) worldTick,
            Player = player,
            Inputs = command.Inputs[i],
          };
        queue.Enqueue(pinput, worldTick);
      }
    } else {
      staleInputs++;
      Debug.Log($"Discarding player inputs which are too old ({serverWorldTick - maxTick} ticks behind)");
    }
  }
}
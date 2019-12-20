using System;
using LiteNetLib;

/// An interface for sending/receiving network commands.
public interface INetChannel {
  void Subscribe<T>(Action<T> onReceiveHandler) where T : class, new();
  void Subscribe<T>(Action<T, NetPeer> onReceiveHandler) where T : class, new();
  void SendCommand<T>(NetPeer peer, T command) where T : class, new();
  void BroadcastCommand<T>(T command) where T : class, new();
  void BroadcastCommand<T>(T command, NetPeer excludedPeer) where T : class, new();
}

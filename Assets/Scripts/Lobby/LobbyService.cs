using System;
using NetCodeTest.Core.Contracts;

namespace NetCodeTest.Lobby
{
    public sealed class LobbyService : ILobbyService
    {
        private readonly LobbyManager _lobby;

        public LobbyService(LobbyManager lobby)
        {
            _lobby = lobby;
        }

        public void UpdatePing(ulong clientId, ushort pingMs)
        {
            _lobby.UpdatePing(clientId, pingMs);
        }

        public void Kick(ulong id) => _lobby.KickPlayerServerRpc(id);
    }
}
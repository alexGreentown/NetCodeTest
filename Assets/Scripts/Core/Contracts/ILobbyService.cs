namespace NetCodeTest.Core.Contracts
{
    public interface ILobbyService
    {
        void Kick(ulong id);

        void UpdatePing(ulong clientId, ushort pingMs);
    }
}
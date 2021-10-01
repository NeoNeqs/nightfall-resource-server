using Godot;
using Nightfall.ServerUtils.Net;
using Nightfall.SharedUtils;
using Nightfall.SharedUtils.InfoCodes;
using Nightfall.SharedUtils.Net.Messaging;

namespace Nightfall.Net
{
    public partial class CommandServer : Node // TODO: Rename CommandServer to sth else. The name doesn't really fit it's purpose.
    {
        private readonly TCPMessageServer _tcpMessageServer = new();

        public override void _Ready()
        {
            _tcpMessageServer.MessageReceivedEvent += OnMessageReceived;

            _tcpMessageServer.Listen(6901);
        }

        private static void OnMessageReceived(NFGuid nfGuid, Message message, long error)
        {
            if (error != NFError.Ok) return;
        }
    }
}

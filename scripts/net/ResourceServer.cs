using Godot;
using Nightfall.Net.Messaging;
using Nightfall.ServerUtils.Net;
using Nightfall.ServerUtils.Profiling;
using Nightfall.SharedUtils.InfoCodes;
using Nightfall.SharedUtils.Logging;
using Nightfall.SharedUtils.Net.Messaging;
using Directory = System.IO.Directory;

namespace Nightfall.Net
{
    public partial class ResourceServer : Node
    {
        private readonly TCPMessageServer _tcpMessageServer = new();
        private static readonly Logger NetLogger = NFInstance.GetLogger("Net");

        public override void _Ready()
        {
            RegisterMessages();
            SetProcess(false);
            SetPhysicsProcess(false);

            _tcpMessageServer.MessageReceivedEvent += OnMessageReceived;
            _tcpMessageServer.Listen(6900);
        }

        private static void RegisterMessages()
        {
            MessageFactory.RegisterMessage<RootHashMessage>();
        }

        private static void OnMessageReceived(NFGuid guid, Message message, long error)
        {
            using var p = Profiler.Profile();
            if (error != NFError.Ok) return;

            switch (message)
            {
                case RootHashMessage rootHashMessage:
                    var hash = rootHashMessage.ToString();
                    
                    if (hash.Length == 0)
                    {
                        break;
                    }
                    
                    if (Directory.Exists(ProjectSettings.GlobalizePath($"user://{hash}")))
                    {
                        // Prepare files to send to the user (create a patch)
                    }
                    else
                    {
                        NetLogger.Debug(@$"Given Hash ""{rootHashMessage}"" does not represent any known update");
                        // Send all files from latest update zipped
                    }

                    break;
                case EmptyMessage emptyMessage:
                    NetLogger.Debug("Empty Message");
                    break;
                default:
                    NetLogger.Error("Bug");
                    break;
            }
        }

        public override void _ExitTree()
        {
            _tcpMessageServer.Stop();
        }
    }
}
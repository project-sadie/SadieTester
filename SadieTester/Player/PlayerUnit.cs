using System.Drawing;
using System.Threading.Channels;
using Bogus;
using SadieTester.Networking.Packets;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Networking.Packets.Writers.Handshake;
using SadieTester.Networking.Packets.Writers.Navigator;
using SadieTester.Networking.Packets.Writers.Rooms;
using Serilog;
using Websocket.Client;
using System.Net.WebSockets;

namespace SadieTester.Player
{
    public class PlayerUnit : IAsyncDisposable
    {
        public readonly PlayerTrimmedDown Player;
        private readonly IWebsocketClient websocketClient;
        private readonly INetworkPacketHandler packetHandler;
        private readonly PlayerRepository playerRepository;

        private readonly Channel<byte[]> incoming =
            Channel.CreateUnbounded<byte[]>(new() { SingleReader = true });

        private readonly Channel<byte[]> outgoing =
            Channel.CreateUnbounded<byte[]>(new() { SingleReader = true });

        private readonly CancellationTokenSource cts = new();
        private readonly NetworkPacketDecoder decoder = new();

        public PlayerUnit(
            PlayerTrimmedDown player,
            IWebsocketClient websocketClient,
            INetworkPacketHandler packetHandler,
            PlayerRepository playerRepository)
        {
            this.Player = player;
            this.websocketClient = websocketClient;
            this.packetHandler = packetHandler;
            this.playerRepository = playerRepository;
        }

        public async Task ConnectAsync()
        {
            websocketClient.MessageReceived.Subscribe(msg =>
            {
                if (msg.MessageType == WebSocketMessageType.Binary)
                    incoming.Writer.TryWrite(msg.Binary);
            });

            websocketClient.DisconnectionHappened.Subscribe(info =>
            {
                Log.Error(
                    "{Username} disconnected: Type={Type}, CloseStatus={CloseStatus}, Desc={Desc}, Exception={Ex}",
                    Player.Username,
                    info.Type,
                    info.CloseStatus,
                    info.CloseStatusDescription ?? "(none)",
                    info.Exception?.ToString() ?? "(none)");

                playerRepository.PlayerUnits.TryRemove(Player.Id, out _);
            });

            websocketClient.ReconnectionHappened.Subscribe(info =>
            {
                if (info.Type != ReconnectionType.Initial)
                    Log.Warning("Player reconnected: {Type}", info.Type);
            });

            _ = Task.Run(ProcessIncomingLoop, cts.Token);
            _ = Task.Run(ProcessOutgoingLoop, cts.Token);
            _ = Task.Run(HeartbeatLoop, cts.Token);

            await websocketClient.Start();
        }

        public Task SafeSendAsync(byte[] data)
        {
            outgoing.Writer.TryWrite(data);
            return Task.CompletedTask;
        }

        private async Task ProcessOutgoingLoop()
        {
            await foreach (var packet in outgoing.Reader.ReadAllAsync(cts.Token))
            {
                try
                {
                    await websocketClient.SendInstant(packet);
                }
                catch { }
            }
        }

        private async Task ProcessIncomingLoop()
        {
            await foreach (var frame in incoming.Reader.ReadAllAsync(cts.Token))
            {
                var packets = decoder.Feed(frame);

                foreach (var p in packets)
                {
                    try
                    {
                        await packetHandler.HandleAsync(this, p);
                    }
                    catch { }
                }
            }
        }

        private async Task HeartbeatLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await SafeSendAsync(new PlayerPongWriter().GetAllBytes());
                }
                catch { }

                await Task.Delay(5000);
            }
        }

        public async Task<bool> TrySendHandshakeAsync()
        {
            var token = Player.Tokens.FirstOrDefault()?.Token;
            if (token == null) return false;

            await SafeSendAsync(new ClientVersionWriter().GetAllBytes());
            await SafeSendAsync(new SecureLoginWriter(token).GetAllBytes());
            return true;
        }

        public bool HasAuthenticated { get; set; }
        public bool InRoom { get; set; }
        public PlayerUnitRoomSession? RoomSession { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastPong { get; set; }
        public DateTime ReceivedNavigatorSearchResults { get; set; }
        public DateTime LastNavigatorSearch { get; set; } = DateTime.Now;

        public async Task WaitForAuthenticationAsync(Func<Task> onSuccess, Action onTimeout)
        {
            var start = DateTime.Now;

            while (!HasAuthenticated)
            {
                if ((DateTime.Now - start).TotalSeconds > 10)
                {
                    onTimeout();
                    return;
                }

                await Task.Delay(100);
            }

            await onSuccess();
        }

        public async Task RunPeriodicChecksAsync()
        {
            if (!HasAuthenticated)
                return;

            LastCheck = DateTime.Now;
            await CheckRandomnessAsync();
        }

        public int NoRoomTicks = 0;

        private async Task CheckRandomnessAsync()
        {
            if (RoomSession == null)
            {
                if (NoRoomTicks >= 11)
                {
                    Console.WriteLine("Tryna make a room due to 10+ no room ticks");
                    await CreateRoomAsync();
                }
                else if (NoRoomTicks >= 3)
                {
                    await WaitForNavigatorResultsAsync(TimeSpan.FromSeconds(1));

                    var enterableResults = NavigatorSearchResults
                        .Where(x => x.UsersNow < x.MaxUsers - 1)
                        .ToList();

                    if (enterableResults.Count != 0)
                    {
                        var roomId = enterableResults
                            .OrderBy(x => x.UsersNow)
                            .First().Id;

                        await LoadRoomAsync(roomId);
                        NavigatorSearchResults.Clear();
                    }
                    else if (SecureRandom.OneIn(5))
                    {
                        await CreateRoomAsync();
                    }
                }

                NoRoomTicks++;
                return;
            }

            NoRoomTicks = 0;

            if (SecureRandom.OneIn(5))
            {
                await WaitForNavigatorResultsAsync(TimeSpan.FromSeconds(1));

                var enterableResults = NavigatorSearchResults
                    .Where(x => x.UsersNow < x.MaxUsers - 1)
                    .ToList();

                if (enterableResults.Count != 0)
                {
                    var roomId = enterableResults
                        .OrderBy(x => x.UsersNow)
                        .First().Id;

                    await LoadRoomAsync(roomId);
                    NavigatorSearchResults.Clear();
                    return;
                }
            }

            if (SecureRandom.OneIn(6))
                await WalkToAsync(RoomSession.GetRandomPoint());
            else if (SecureRandom.OneIn(4) && RoomSession.Users.Count > 2)
                await LookToPointAsync(RoomSession.GetRandomUser(Player.Id).Position);
            else if (SecureRandom.OneIn(68))
                await SafeSendAsync(new RoomUserDanceWriter(GlobalState.Random.Next(1, 4)).GetAllBytes());
            else if (SecureRandom.OneIn(200))
                await CreateRoomAsync();
            else if (SecureRandom.OneIn(4))
                await SayInRoomAsync(RandomHelpers.GetRandomChatMessage());
            else if ((DateTime.Now - RoomSession.LoadedAt).TotalSeconds > 120 && SecureRandom.OneIn(165))
                await LoadRandomRoomAsync();
        }

        private bool ShouldMakeRoom()
        {
            var capped = Math.Min(NoRoomTicks, 70);
            return SecureRandom.OneIn(150 - capped * 2);
        }

        public List<NavigatorSearchRoomResult> NavigatorSearchResults = [];

        private async Task WaitForNavigatorResultsAsync(TimeSpan timeout)
        {
            if (NavigatorSearchResults.Any())
                return;

            LastNavigatorSearch = DateTime.Now;
            var started = DateTime.Now;

            await SafeSendAsync(new NavigatorSearchWriter().GetAllBytes());

            while (NavigatorSearchResults.Count == 0 &&
                   ReceivedNavigatorSearchResults < started)
            {
                if ((DateTime.Now - started) > timeout)
                    return;

                await Task.Delay(100);
            }
        }

        public async Task LoadRandomRoomAsync()
        {
            await WaitForNavigatorResultsAsync(TimeSpan.FromSeconds(2));

            var options = NavigatorSearchResults
                .Where(x => x.UsersNow < x.MaxUsers - 1)
                .OrderBy(x => x.UsersNow)
                .ToList();
            
            if (options.Count != 0)
            {
                var roomId = options
                    .First().Id;

                await LoadRoomAsync(roomId);
                NavigatorSearchResults.Clear();
            }
            else if (SecureRandom.OneIn(100))
            {
                await CreateRoomAsync();
            }
        }

        public async Task LoadRoomAsync(int roomId)
        {
            RoomSession = null;
            await SafeSendAsync(new LoadRoomWriter(roomId).GetAllBytes());
        }

        public async Task LookToPointAsync(Point p)
        {
            await SafeSendAsync(new RoomUserLookToPointWriter(p).GetAllBytes());
        }

        public async Task WalkToAsync(Point p)
        {
            await SafeSendAsync(new RoomUserWalkWriter(p.X, p.Y).GetAllBytes());
        }

        public async Task SayInRoomAsync(string msg, int bubble = 0)
        {
            await SafeSendAsync(new RoomUserStartTypingWriter().GetAllBytes());
            await Task.Delay(msg.Length * 95);
            await SafeSendAsync(new RoomUserChat(msg, bubble).GetAllBytes());
            await Task.Delay(100);
            await SafeSendAsync(new RoomUserStopTypingWriter().GetAllBytes());
        }

        public async Task CreateRoomAsync()
        {
            NoRoomTicks = 0;
            
            Console.WriteLine("Making a room...");

            var faker = new Faker<CreateRoomData>()
                .RuleFor(u => u.Name, f => f.Address.StreetAddress())
                .RuleFor(u => u.Description, f => f.Lorem.Sentences(3))
                .RuleFor(u => u.Layout, f => ModelSelector.GetRandomModel());

            var data = faker.Generate();

            await SafeSendAsync(new CreateRoomWriter(data).GetAllBytes());
        }

        public ValueTask DisposeAsync()
        {
            cts.Cancel();

            if (websocketClient is IAsyncDisposable asyncWs)
                return asyncWs.DisposeAsync();

            websocketClient.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

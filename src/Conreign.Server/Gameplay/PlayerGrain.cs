using System.Threading.Tasks;
using Conreign.Contracts.Communication;
using Conreign.Contracts.Gameplay.Data;
using Conreign.Contracts.Gameplay.Events;
using Conreign.Server.Communication;
using Conreign.Server.Contracts.Communication;
using Conreign.Server.Contracts.Communication.Events;
using Conreign.Server.Contracts.Gameplay;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace Conreign.Server.Gameplay
{
    [Reentrant]
    public class PlayerGrain : Grain<PlayerState>, IPlayerGrain
    {
        private Player _player;
        private StreamSubscriptionHandle<IServerEvent> _subscription;

        public Task UpdateOptions(PlayerOptionsData options)
        {
            return _player.UpdateOptions(options);
        }

        public Task UpdateGameOptions(GameOptionsData options)
        {
            return _player.UpdateGameOptions(options);
        }

        public async Task StartGame()
        {
            await _player.StartGame();
            await WriteStateAsync();
        }

        public Task LaunchFleet(FleetData fleet)
        {
            return _player.LaunchFleet(fleet);
        }

        public Task CancelFleet(FleetCancelationData fleetCancelation)
        {
            return _player.CancelFleet(fleetCancelation);
        }

        public Task EndTurn()
        {
            return _player.EndTurn();
        }

        public Task SendMessage(TextMessageData textMessage)
        {
            return _player.SendMessage(textMessage);
        }

        public async Task<IRoomData> GetState()
        {
            return await State.Room.GetState(State.UserId);
        }

        public Task Handle(GameStartedServer @event)
        {
            return _player.Handle(@event);
        }

        public async Task Handle(Connected @event)
        {
            await _player.Handle(@event);
        }

        public async Task Handle(Disconnected @event)
        {
            await _player.Handle(@event);
        }

        public Task Ping()
        {
            return Task.CompletedTask;
        }

        public async Task Handle(GameEnded @event)
        {
            await _player.Handle(@event);
            await WriteStateAsync();
        }

        public override async Task OnActivateAsync()
        {
            await InitializeState();
            var provider = GetStreamProvider(StreamConstants.ProviderName);
            var stream = provider.GetServerStream(TopicIds.Player(State.UserId, State.RoomId));
            _player = new Player(State);
            _subscription = await this.EnsureIsSubscribedOnce(stream);
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await _subscription.UnsubscribeAsync();
            await base.OnDeactivateAsync();
        }

        private Task InitializeState()
        {
            State.UserId = this.GetPrimaryKey(out string roomId);
            State.RoomId = roomId;
            if (State.Lobby == null)
            {
                State.Lobby = GrainFactory.GetGrain<ILobbyGrain>(roomId);
            }
            return Task.CompletedTask;
        }
    }
}
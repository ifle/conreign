using System.Threading.Tasks;
using Conreign.Core.Communication;
using Conreign.Core.Contracts.Communication;
using Conreign.Core.Contracts.Communication.Events;
using Conreign.Core.Contracts.Gameplay;
using Conreign.Core.Contracts.Gameplay.Data;
using Conreign.Core.Contracts.Gameplay.Events;
using Orleans;

namespace Conreign.Core.Gameplay
{
    public class PlayerGrain : Grain<PlayerState>, IPlayerGrain
    {
        private Player _player;
        private IPublisherGrain _publisher;
        private IBus _playerBus;

        public override async Task OnActivateAsync()
        {
            await InitializeState();
            _publisher = GrainFactory.GetGrain<IPublisherGrain>(ServerTopics.Player(State.UserId, State.RoomId));
            _player = new Player(State, _publisher);
            _playerBus = GrainFactory.GetGrain<IBusGrain>(ServerTopics.Player(State.UserId, State.RoomId));
            await _playerBus.Subscribe(this.AsReference<IPlayerGrain>());
            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await _playerBus.Unsubscribe(this.AsReference<IPlayerGrain>());
            await base.OnDeactivateAsync();
        }

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

        public Task Write(TextMessageData textMessage)
        {
            return _player.Write(textMessage);
        }

        public Task<IRoomData> GetState()
        {
            return _player.GetState();
        }

        private Task InitializeState()
        {
            string roomId;
            State.UserId = this.GetPrimaryKey(out roomId);
            State.RoomId = roomId;
            if (State.Lobby == null)
            {
                State.Lobby = GrainFactory.GetGrain<ILobbyGrain>(roomId);
            }
            return Task.CompletedTask;
        }

        public Task Handle(GameStarted.Server @event)
        {
            return _player.Handle(@event);
        }

        public async Task Handle(Connected @event)
        {
            await _publisher.Handle(@event);
            await _player.Handle(@event);
        }

        public async Task Handle(Disconnected @event)
        {
            await _publisher.Handle(@event);
            await _player.Handle(@event);
        }

        public Task Handle(GameEnded @event)
        {
            return _player.Handle(@event);
        }
    }
}
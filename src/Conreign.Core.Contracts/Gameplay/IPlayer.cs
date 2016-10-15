﻿using System.Threading.Tasks;
using Conreign.Core.Contracts.Gameplay.Data;

namespace Conreign.Core.Contracts.Gameplay
{
    public interface IPlayer
    {
        Task UpdateOptions(PlayerOptionsData options);
        Task UpdateGameOptions(GameOptionsData options);
        Task StartGame();
        Task LaunchFleet(FleetData fleet);
        Task CancelFleet(FleetCancelationData fleetCancelation);
        Task EndTurn();
        Task Write(TextMessageData textMessage);
        Task<IRoomData> GetState();
    }
}
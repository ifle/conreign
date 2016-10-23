﻿using System;
using System.Threading.Tasks;
using Conreign.Core.Client.Messages;
using Conreign.Core.Contracts.Gameplay.Data;
using MediatR;

namespace Conreign.Core.Client.Handlers
{
    internal class LaunchFleetHandler : IAsyncRequestHandler<LaunchFleetCommand, Unit>
    {
        private readonly IHandlerContext _context;

        public LaunchFleetHandler(IHandlerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public async Task<Unit> Handle(LaunchFleetCommand message)
        {
            var player = await _context.User.JoinRoom(message.RoomId, _context.Connection.Id);
            await player.LaunchFleet(new FleetData());
            return Unit.Value;
        }
    }
}
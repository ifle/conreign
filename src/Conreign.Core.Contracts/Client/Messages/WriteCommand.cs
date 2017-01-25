﻿using MediatR;

namespace Conreign.Core.Contracts.Client.Messages
{
    public class WriteCommand : IRequest<Unit>
    {
        public string RoomId { get; set; }
        public string Text { get; set; }
    }
}
﻿using Conreign.Core.Contracts.Abstractions;
using Conreign.Core.Contracts.Auth;
using Conreign.Core.Contracts.Auth.Data;
using Conreign.Core.Contracts.Game.Data;

namespace Conreign.Core.Contracts.Game.Actions
{
    public class CheckGameStatusAction : IPayloadContainer<GameReferencePayload>, IMetadataContainer<IUserMeta>
    {
        public IUserMeta Meta { get; set; }
        public GameReferencePayload Payload { get; set; }
    }
}
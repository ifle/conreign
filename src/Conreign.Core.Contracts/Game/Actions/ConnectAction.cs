﻿using Conreign.Core.Contracts.Abstractions;
using Conreign.Core.Contracts.Auth;
using Conreign.Core.Contracts.Auth.Data;

namespace Conreign.Core.Contracts.Game.Actions
{
    public class ConnectAction : IMetadataContainer<IUserMeta>
    {
        public IUserMeta Meta { get; set; }
    }
}
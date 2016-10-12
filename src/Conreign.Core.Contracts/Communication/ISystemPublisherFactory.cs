﻿using System.Threading.Tasks;

namespace Conreign.Core.Contracts.Communication
{
    public interface ISystemPublisherFactory
    {
        Task<ISystemPublisher> CreateSystemPublisher(string topic);
    }
}
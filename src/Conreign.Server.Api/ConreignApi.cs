﻿using System;
using Conreign.Server.Api.Configuration;
using Conreign.Server.Contracts.Communication;
using Orleans.Runtime.Configuration;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Conreign.Server.Api
{
    public class ConreignApi
    {
        private ConreignApi(ClientConfiguration orleansConfiguration, ConreignApiConfiguration apiConfiguration,
            ILogger logger)
        {
            OrleansConfiguration = orleansConfiguration;
            Configuration = apiConfiguration;
            Logger = logger;
        }

        public ClientConfiguration OrleansConfiguration { get; }
        public ConreignApiConfiguration Configuration { get; }
        public ILogger Logger { get; }

        public static ConreignApi Create(
            ClientConfiguration baseOrleansConfiguration = null,
            ConreignApiConfiguration apiConfiguration = null)
        {
            var orleansConfig = baseOrleansConfiguration ?? ClientConfiguration.LocalhostSilo();
            apiConfiguration = apiConfiguration ?? ConreignApiConfiguration.Load();
            orleansConfig.GatewayProvider = apiConfiguration.SystemStorageType;
            orleansConfig.DataConnectionString = apiConfiguration.SystemStorageConnectionString;
            orleansConfig.AddSimpleMessageStreamProvider(StreamConstants.ProviderName);

            var loggerConfiguration = new LoggerConfiguration();
            if (!string.IsNullOrEmpty(apiConfiguration.ElasticSearchUri))
            {
                var elasticOptions = new ElasticsearchSinkOptions(new Uri(apiConfiguration.ElasticSearchUri))
                {
                    AutoRegisterTemplate = true,
                    BufferBaseFilename = "logs/elastic-buffer"
                };
                loggerConfiguration.WriteTo.Elasticsearch(elasticOptions);
            }
            loggerConfiguration.WriteTo.LiterateConsole();
            var logger = loggerConfiguration
                .MinimumLevel.Is(apiConfiguration.MinimumLogLevel)
                .Enrich.FromLogContext()
                .CreateLogger()
                .ForContext("ApplicationId", "Conreign.Api");
            return new ConreignApi(orleansConfig, apiConfiguration, logger);
        }
    }
}
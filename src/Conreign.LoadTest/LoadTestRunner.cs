﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conreign.Client.SignalR;
using Conreign.Core.AI;
using Conreign.Core.AI.LoadTest;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Conreign.LoadTest
{
    public static class LoadTestRunner
    {
        private const string ConfigurationFileKey = "ConfigurationFileName";

        public static Task Run(string[] args)
        {
            var options = ParseOptions(args);
            return Run(options);
        }

        public static async Task Run(LoadTestOptions options)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.LiterateConsole();
            if (!string.IsNullOrEmpty(options.ElasticSearchUri))
            {
                var elasticOptions = new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
                {
                    AutoRegisterTemplate = true,
                    BufferBaseFilename = "logs/elastic-buffer"
                };
                loggerConfiguration.WriteTo.Elasticsearch(elasticOptions);
            }
            Log.Logger = loggerConfiguration
                .MinimumLevel.Is(options.MinimumLogLevel)
                .CreateLogger()
                .ForContext("ApplicationId", "Conreign.LoadTest")
                .ForContext("InstanceId", options.InstanceId);
            Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg));
            var logger = Log.Logger;
            var botOptions = options.BotOptions;
            ServicePointManager.DefaultConnectionLimit = botOptions.RoomsCount*botOptions.BotsPerRoomCount*2;
            var signalrOptions = new SignalRClientOptions(options.ConnectionUri);
            var client = new SignalRClient(signalrOptions);
            var factory = new LoadTestBotFactory(botOptions);
            var farm = new BotFarm(options.InstanceId, client, factory, new BotFarmOptions());
            var cts = new CancellationTokenSource();
            cts.CancelAfter(options.Timeout);
            try
            {
                await farm.Run(cts.Token);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Load test failed: {ErrorMessage}.", ex.Message);
            }
            Log.CloseAndFlush();
        }

        private static LoadTestOptions ParseOptions(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();

            var configFileName = config.GetValue<string>(ConfigurationFileKey, null);
            if (configFileName != null)
            {
                builder = new ConfigurationBuilder();
                builder.AddJsonFile(configFileName, false);
                builder.AddCommandLine(args);
                config = builder.Build();
            }
            var options = new LoadTestOptions();
            config.Bind(options);
            return options;
        }
    }
}

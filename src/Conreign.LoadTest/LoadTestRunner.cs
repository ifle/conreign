﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conreign.Client.SignalR;
using Conreign.Core.AI;
using Conreign.Core.AI.LoadTest;
using Ionic.Zip;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using CompressionLevel = Ionic.Zlib.CompressionLevel;

namespace Conreign.LoadTest
{
    public static class LoadTestRunner
    {
        public static Task Run(string[] args)
        {
            var options = LoadTestOptions.Parse(args);
            return Run(options);
        }

        public static async Task Run(LoadTestOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            var loggerConfiguration = new LoggerConfiguration();
            if (options.LogToConsole)
            {
                loggerConfiguration.WriteTo.LiterateConsole();
            }
            if (!string.IsNullOrEmpty(options.LogFileName))
            {
                var formatter = new JsonFormatter(renderMessage: true, closingDelimiter: $",{Environment.NewLine}");
                var logFilePath = Environment.ExpandEnvironmentVariables(options.LogFileName);
                loggerConfiguration.WriteTo.File(formatter, logFilePath, buffered: true);
            }
            if (!string.IsNullOrEmpty(options.ElasticSearchUri))
            {
                var elasticOptions = new ElasticsearchSinkOptions(new Uri(options.ElasticSearchUri))
                {
                    AutoRegisterTemplate = true,
                    BufferBaseFilename = "logs/elastic-buffer",
                    BatchPostingLimit = 100,
                    BufferLogShippingInterval = options.ElasticSearchFlushInterval
                };
                loggerConfiguration.WriteTo.Elasticsearch(elasticOptions);
            }
            Log.Logger = loggerConfiguration
                .MinimumLevel.Is(options.MinimumLogLevel)
                .Enrich.FromLogContext()
                .CreateLogger()
                .ForContext("ApplicationId", "Conreign.LoadTest")
                .ForContext("InstanceId", options.InstanceId);
            Serilog.Debugging.SelfLog.Enable(msg => Trace.WriteLine(msg));
            var logger = Log.Logger;
            try
            {
                var botOptions = options.BotOptions;
                logger.Information("Initialized with the following configuration: {@Configuration}", options);
                ServicePointManager.DefaultConnectionLimit = botOptions.RoomsCount * botOptions.BotsPerRoomCount * 2;
                var signalrOptions = new SignalRClientOptions(options.ConnectionUri);
                var client = new SignalRClient(signalrOptions);
                var factory = new LoadTestBotFactory(botOptions);
                var farm = new BotFarm(options.InstanceId, client, factory, new BotFarmOptions());
                var cts = new CancellationTokenSource();
                cts.CancelAfter(options.Timeout);
                await farm.Run(cts.Token);
                logger.Information("Load test complete.");
                if (!string.IsNullOrEmpty(options.LogFileName) && options.ZipLogFile)
                {
                    logger.Information("Waiting a bit for Serilog to flush.");
                    // ReSharper disable once MethodSupportsCancellation
                    // HACK: magic delay here, how to explicitely flush Serilog without closing?
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    var zipPath = ZipLogFile(options.LogFileName);
                    logger.Information("Zipped log file {LogFilePath} to {ZippedLogFilePath}", options.LogFileName, zipPath);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Load test failed: {ErrorMessage}.", ex.Message);
                throw;
            }
            finally
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(options.LogFlushTimeout);
                    await Task.Run(() => Log.CloseAndFlush(), cts.Token);
                }
                catch (TaskCanceledException)
                {
                    logger.Warning("Log flush timeout.");
                }
            }
        }

        private static string ZipLogFile(string logFilePath)
        {
            var directory = Path.GetDirectoryName(logFilePath);
            var fileName = Path.GetFileNameWithoutExtension(logFilePath);
            if (directory == null)
            {
                throw new InvalidOperationException("Log directory is null.");
            }
            var zipPath = Path.Combine(directory, $"{fileName}.zip");
            using (var archive = new ZipFile(zipPath))
            {
                archive.CompressionLevel = CompressionLevel.BestCompression;
                archive.AddFile(logFilePath);
                archive.Save();
            }
            return zipPath;
        }
    }
}

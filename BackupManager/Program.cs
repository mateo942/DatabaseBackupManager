using BackupManager.Cron;
using BackupManager.Notification;
using BackupManager.Pipelines;
using BackupManager.Settings;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager
{
    class Program
    {
        readonly static IDictionary<string, Type> _pipelines = new Dictionary<string, Type>()
        {
            { "BACKUP", typeof(MsSQLBackupPipeline) },
            { "ZIP", typeof(ZipPipeline) },
            { "DELETE_FILE", typeof(DeleteFilePipeline) },
            { "DELETE_OLD_FILE", typeof(DeleteOldFilesPipeline) },
            { "FTP", typeof(FtpPipeline) },
            { "MAIL", typeof(MailPipeline) },
            { "MOVE", typeof(MoveFilePipeline) }
        };

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", false)
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMediatR(typeof(Program).Assembly);
            serviceCollection.AddOptions();
            serviceCollection.Configure<BackupSettings>(configuration.GetSection("BackupSettings"));
            serviceCollection.AddLogging(cfg =>
            {
                cfg.SetMinimumLevel(LogLevel.Trace);
                cfg.AddConsole();
            });
            serviceCollection.AddTransient<MsSQLBackupPipeline>();
            serviceCollection.AddTransient<ZipPipeline>();
            serviceCollection.AddTransient<DeleteFilePipeline>();
            serviceCollection.AddTransient<DeleteOldFilesPipeline>();
            serviceCollection.AddTransient<FtpPipeline>();
            serviceCollection.AddTransient<MailPipeline>();
            serviceCollection.AddTransient<MoveFilePipeline>();

            serviceCollection.AddTransient<DumpNotificationHandler>();

            serviceCollection.AddSingleton<TcpBackupManager>();
            serviceCollection.AddSingleton<TcpMessageHandler>();

            serviceCollection.AddSingleton<PipelineManger>();
            serviceCollection.AddSingleton<ICronDaemon, CronDaemon>();

            var provider = serviceCollection.BuildServiceProvider();

            var settings = provider.GetRequiredService<IOptions<BackupSettings>>().Value;
            var cronDeamon = provider.GetRequiredService<ICronDaemon>();
            cronDeamon.Start();

            foreach (var item in settings.BackupDatabases)
            {
                var setup = new PipelineSetup();
                BuildStage(setup, item);

                var pipelineManager = provider.GetService<PipelineManger>();
                pipelineManager.Execute(setup, default(CancellationToken)).ConfigureAwait(true);

                cronDeamon.AddJob(item.Cron, async () =>
                {
                    var setup = new PipelineSetup();
                    BuildStage(setup, item);

                    var pipelineManager = provider.GetService<PipelineManger>();
                    await pipelineManager.Execute(setup, default(CancellationToken));
                });
            }

            Console.ReadKey(true);
            cronDeamon.Stop();

            provider.Dispose();
        }

        static PipelineSetup BuildStage(PipelineSetup pipelineSetup, BackupDatabase settings)
        {
            pipelineSetup.AddVariables(settings.Variables);

            foreach (var item in settings.Pipeline)
            {
                if (_pipelines.ContainsKey(item) == false)
                    throw new InvalidOperationException("Invalid pipeline name");

                var pipeline = _pipelines[item];
                pipelineSetup.AddStage(pipeline, settings);
            }

            return pipelineSetup;
        }
    }
}

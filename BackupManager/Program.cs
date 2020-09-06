using BackupManager.Cron;
using BackupManager.Notification;
using BackupManager.Pipelines;
using BackupManager.Settings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            { "FTP", typeof(FtpPipeline) }
        };

        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMediatR(typeof(Program).Assembly);
            serviceCollection.AddLogging(cfg =>
            {
                cfg.SetMinimumLevel(LogLevel.Trace);
                cfg.AddConsole();
            });
            serviceCollection.AddSingleton<BackupSettings>(new BackupSettings
            {
                BackupDatabases = new List<BackupDatabase>
                {
                    new BackupDatabase
                    {
                        ConnectionString = "Server=.;Database=APP_MASTER;Trusted_Connection=True;",
                        Name = "APP_MASTER",
                        OutputDirectory = @"F:\TestBackup\diff",
                        Pipeline = new[] { "BACKUP", "ZIP", "DELETE_FILE", "DELETE_OLD_FILE" },
                        Type = BackupType.Diff,
                        Cron = "* * * * *"
                    },
                    new BackupDatabase
                    {
                        ConnectionString = "Server=.;Database=APP_MASTER;Trusted_Connection=True;",
                        Name = "APP_MASTER",
                        OutputDirectory = @"F:\TestBackup\full",
                        Pipeline = new[] { "BACKUP" },
                        Type = BackupType.Full,
                        Cron = "*/2 * * * *"
                    }
                }
            });
            serviceCollection.AddTransient<MsSQLBackupPipeline>();
            serviceCollection.AddTransient<ZipPipeline>();
            serviceCollection.AddTransient<DeleteFilePipeline>();
            serviceCollection.AddTransient<DeleteOldFilesPipeline>();
            serviceCollection.AddTransient<FtpPipeline>();

            serviceCollection.AddTransient<DumpNotificationHandler>();

            serviceCollection.AddSingleton<TcpBackupManager>();
            serviceCollection.AddSingleton<TcpMessageHandler>();

            serviceCollection.AddSingleton<PipelineManger>();
            serviceCollection.AddSingleton<ICronDaemon, CronDaemon>();

            var provider = serviceCollection.BuildServiceProvider();

            var tcpBackupManager = provider.GetRequiredService<TcpBackupManager>();
            tcpBackupManager.Start();

            var settings = provider.GetRequiredService<BackupSettings>();
            var cronDeamon = provider.GetRequiredService<ICronDaemon>();
            cronDeamon.Start();

            foreach (var item in settings.BackupDatabases)
            {
                var setup = new PipelineSetup();

                BuildStage(setup, item);

                var pipelineManager = provider.GetService<PipelineManger>();
                pipelineManager.Execute(setup, default(CancellationToken)).ConfigureAwait(true);

                //cronDeamon.AddJob(item.Cron, async () =>
                //{
                //    var setup = new PipelineSetup();

                //    BuildStage(setup, item);

                //    var pipelineManager = provider.GetService<PipelineManger>();
                //    await pipelineManager.Execute(setup, default(CancellationToken));
                //});
            }

            Console.ReadKey(true);
            cronDeamon.Stop();

            provider.Dispose();
        }

        static PipelineSetup BuildStage(PipelineSetup pipelineSetup, BackupDatabase settings)
        {
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

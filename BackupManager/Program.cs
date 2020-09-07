using BackupManager.Cron;
using BackupManager.Notification;
using BackupManager.Pipelines;
using BackupManager.Settings;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace BackupManager
{
    class Program
    {

        static void Main(string[] args)
        {
            var rc = HostFactory.Run(cfg =>
            {
                cfg.SetDisplayName("Database Backup");
                cfg.SetDescription("Database Backup");
                cfg.SetServiceName("Database Backup");
                cfg.Service<Wrapper>(s =>
                {
                    s.ConstructUsing(x => new Wrapper());
                    s.WhenStarted(x => { x.Start(); x.RunInCron(); });
                    s.WhenStopped(x => x.Stop());
                });
                cfg.StartAutomatically();
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  //11
            Environment.ExitCode = exitCode;
        }
    }

    class Wrapper
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

        private ServiceProvider provider;

        public Wrapper()
        {

        }

        public Wrapper Start()
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("config.json", false)
               .Build();

            var serviceCollection = new ServiceCollection();
            Configure(serviceCollection, configuration);

            provider = serviceCollection.BuildServiceProvider();

            return this;
        }

        public void Stop()
        {
            provider.Dispose();
        }

        void Configure(ServiceCollection serviceCollection, IConfigurationRoot configuration)
        {
            serviceCollection.AddMediatR(typeof(Program).Assembly);

            serviceCollection.AddOptions();
            serviceCollection.Configure<BackupSettings>(configuration.GetSection("BackupSettings"));

            serviceCollection.AddLogging(cfg =>
            {
                cfg.SetMinimumLevel(LogLevel.Trace);
                cfg.AddConsole();
                cfg.AddSerilog();
                cfg.AddFile(configuration.GetSection("Logging"));
            });
            serviceCollection.AddTransient<MsSQLBackupPipeline>();
            serviceCollection.AddTransient<ZipPipeline>();
            serviceCollection.AddTransient<DeleteFilePipeline>();
            serviceCollection.AddTransient<DeleteOldFilesPipeline>();
            serviceCollection.AddTransient<FtpPipeline>();
            serviceCollection.AddTransient<MailPipeline>();
            serviceCollection.AddTransient<MoveFilePipeline>();

            serviceCollection.AddTransient<DumpNotificationHandler>();

            serviceCollection.AddSingleton<PipelineManger>();
            serviceCollection.AddSingleton<ICronDaemon, CronDaemon>();
        }

        public void RunOne(string id)
        {
            var settings = provider.GetRequiredService<IOptions<BackupSettings>>().Value;

            var backupSettings = settings.BackupDatabases.SingleOrDefault(x => x.Id == id);
            if (backupSettings == null)
                throw new InvalidOperationException($"Id: {id} not found");

            var setup = new PipelineSetup();
            BuildStage(setup, backupSettings);

            var pipelineManager = provider.GetService<PipelineManger>();
            pipelineManager.Execute(setup, default(CancellationToken)).ConfigureAwait(true).GetAwaiter().GetResult();
        }

        public void RunInCron()
        {
            var settings = provider.GetRequiredService<IOptions<BackupSettings>>().Value;
            var cronDeamon = provider.GetRequiredService<ICronDaemon>();
            cronDeamon.Start();

            foreach (var item in settings.BackupDatabases)
            {
                cronDeamon.AddJob(item.Cron, async () =>
                {
                    var setup = new PipelineSetup();
                    BuildStage(setup, item);

                    var pipelineManager = provider.GetService<PipelineManger>();
                    await pipelineManager.Execute(setup, default(CancellationToken));
                });
            }
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

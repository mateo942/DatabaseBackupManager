using BackupManager.Helpers;
using BackupManager.Notification;
using BackupManager.Pipelines;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pipeline;
using Pipeline.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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

        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("config.json", false)
               .Build();

            var serviceCollection = new ServiceCollection();
            Configure(serviceCollection, configuration);

            var provider = serviceCollection.BuildServiceProvider();

            //Run pipeline
            using (var scope = provider.CreateScope())
            {
                var pipelineManager = scope.ServiceProvider.GetRequiredService<IPipelineManager>();

                var pipelines = provider.GetRequiredService<IOptions<List<Settings.PipelineSettings>>>();
                foreach (var pipeline in pipelines.Value)
                {
                    var pipelineConfiguration = new PipelineConfiguration();
                    pipelineConfiguration.AddAlwaysEnd(x =>
                    {
                        if (pipeline.DeleteWorkingDir)
                        {
                            var workingDir = DirectoryHelper.GetWorkingDir(x);
                            Directory.Delete(workingDir, true);
                        }
                    });
                    pipelineConfiguration.AddGlobalVariables(pipeline.Variables);

                    ConfigureByStep(pipelineConfiguration, pipeline);

                    await pipelineManager.Configure(pipelineConfiguration).Run();
                }
            }

            

            await provider.DisposeAsync();
        }

        static void Configure(ServiceCollection serviceCollection, IConfigurationRoot configuration)
        {
            serviceCollection.AddMediatR(typeof(Program).Assembly);

            serviceCollection.AddPipeline();
            serviceCollection.AddOptions();
            serviceCollection.Configure<List<Settings.PipelineSettings>>(configuration.GetSection("Pipelines"));

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
        }

        static PipelineConfiguration ConfigureByStep(PipelineConfiguration cfg, Settings.PipelineSettings pipelineSettings)
        {
            foreach (var item in pipelineSettings.Steps)
            {
                var pipelineType = _pipelines[item.Name];
                var variables = new Variables();
                variables.AddRange(item.Variables);

                var interfaceWithCommand = pipelineType.GetInterface(typeof(IPipeline<>).Name);
                if (interfaceWithCommand != null)
                {
                    var genericType = interfaceWithCommand.GetGenericArguments()[0];
                    var command = item.Command.Get(genericType);

                    cfg.NextStep(pipelineType, variables, (IPipelineCommand)command);
                }
                else
                {
                    cfg.NextStep(pipelineType, variables);
                }
            }

            return cfg;
        }
    }
}

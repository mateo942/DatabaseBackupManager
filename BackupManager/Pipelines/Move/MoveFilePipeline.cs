using BackupManager.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class MoveFilePipeline : PipelineBase<BackupDatabase>
    {
        private readonly ILogger<MoveFilePipeline> _logger;

        public MoveFilePipeline(ILogger<MoveFilePipeline> logger)
        {
            _logger = logger;
        }

        public override Task Execute(BackupDatabase command, Variables variables, CancellationToken cancellationToken)
        {
            var filesToUpload = variables.GetFilesToUpload();
            var outputPath = variables.GetMoveOutputPath();
            if (!string.IsNullOrEmpty(outputPath))
            {
                foreach (var item in filesToUpload)
                {
                    var name = Path.GetFileName(item);

                    if (File.Exists(item))
                    {
                        File.Move(item, Path.Combine(outputPath, name));
                        _logger.LogInformation("Moved file: {0}", name);
                    } else
                    {
                        _logger.LogInformation("File: {0} not found", name);
                    }
                }
            }

            variables.AddUploadTo("Move files");

            return Task.CompletedTask;
        }
    }
}

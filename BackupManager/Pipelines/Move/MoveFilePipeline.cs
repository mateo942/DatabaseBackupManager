using BackupManager.Helpers;
using BackupManager.Settings;
using Microsoft.Extensions.Logging;
using Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class MoveFilePipeline : IPipeline
    {
        const string FILES_TO_MOVE = "FILES_TO_MOVE";
        const string OUTPUT_PATH = "OUTPUT_PATH";

        private readonly ILogger<MoveFilePipeline> _logger;

        public MoveFilePipeline(ILogger<MoveFilePipeline> logger)
        {
            _logger = logger;
        }

        public Task Execute(PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            var outputPath = pipelineContext.LocalVariables.Get<string>(OUTPUT_PATH);
            outputPath = DirectoryHelper.GetAbsolutePath(pipelineContext, outputPath);
            DirectoryHelper.CreateIfNotExists(outputPath);

            if(pipelineContext.LocalVariables.TryGetEnumerate<string>(FILES_TO_MOVE, out IEnumerable<string> filesToMove))
            {
                var tmpList = filesToMove.Select(x => PathHelper.GetPathWithVariable(pipelineContext, DirectoryHelper.GetAbsolutePath(pipelineContext, x)))
                    .Select(x => new {
                        Path = x,
                        IsDir = DirectoryHelper.IsDir(x)
                    }).ToList();

                foreach (var item in tmpList)
                {
                    if (item.IsDir)
                    {
                        var name = Path.GetDirectoryName(item.Path);

                        if (Directory.Exists(item.Path))
                        {
                            Directory.Move(item.Path, outputPath);
                            _logger.LogInformation("Moved dir: {0}", name);
                        }
                        else
                        {
                            _logger.LogInformation("Dir: {0} not found", name);
                        }
                    } else
                    {
                        var name = Path.GetFileName(item.Path);

                        if (File.Exists(item.Path))
                        {
                            File.Move(item.Path, Path.Combine(outputPath, name));
                            _logger.LogInformation("Moved file: {0}", name);
                        }
                        else
                        {
                            _logger.LogInformation("File: {0} not found", name);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}

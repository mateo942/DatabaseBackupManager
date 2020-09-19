using BackupManager.Helpers;
using BackupManager.Settings;
using Microsoft.Extensions.Logging;
using Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class ZipPipeline : IPipeline
    {
        const string FILES_TO_ZIP = "FILES_TO_ZIP";
        const string OUTPUT_FILE = "OUTPUT_FILE";

        private readonly ILogger<ZipPipeline> _logger;

        public ZipPipeline(ILogger<ZipPipeline> logger)
        {
            _logger = logger;
        }

        public Task Execute(PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            var outputFile = pipelineContext.LocalVariables.Get<string>(OUTPUT_FILE);
            outputFile = PathHelper.GetPathWithVariable(pipelineContext, DirectoryHelper.GetAbsolutePath(pipelineContext, outputFile));

            if (pipelineContext.LocalVariables.TryGetEnumerate<string>(FILES_TO_ZIP, out IEnumerable<string> filesToZip))
            {
                var tmpList = filesToZip.Select(x => PathHelper.GetPathWithVariable(pipelineContext, DirectoryHelper.GetAbsolutePath(pipelineContext, x)))
                .Select(x => new { 
                    Path = x,
                    IsDir = DirectoryHelper.IsDir(x)
                }).ToList();

                var workingDir = DirectoryHelper.GetAbsolutePath(pipelineContext, Guid.NewGuid().ToString());
                try
                {
                    DirectoryHelper.CreateIfNotExists(workingDir);

                    foreach (var item in tmpList)
                    {
                        if (item.IsDir)
                        {
                            var name = Path.GetDirectoryName(item.Path);

                            if (Directory.Exists(item.Path))
                            {
                                Directory.Move(item.Path, workingDir);
                                _logger.LogInformation("Moved dir: {0}", name);
                            }
                            else
                            {
                                _logger.LogInformation("Dir: {0} not found", name);
                            }
                        }
                        else
                        {
                            var name = Path.GetFileName(item.Path);

                            if (File.Exists(item.Path))
                            {
                                File.Move(item.Path, Path.Combine(workingDir, name));
                                _logger.LogInformation("Moved file: {0}", name);
                            }
                            else
                            {
                                _logger.LogInformation("File: {0} not found", name);
                            }
                        }
                    }

                    ZipFile.CreateFromDirectory(workingDir, outputFile);
                }
                finally
                {
                    Directory.Delete(workingDir, true);
                }
            }

            return Task.CompletedTask;
        }

        private bool IsDir(string value)
        {
            FileAttributes attr = File.GetAttributes(value);
            return attr.HasFlag(FileAttributes.Directory);
        }
    }
}

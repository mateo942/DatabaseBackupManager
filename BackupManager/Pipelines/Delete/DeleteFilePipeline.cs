using BackupManager.Helpers;
using BackupManager.Settings;
using Pipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class DeleteFilePipeline : IPipeline
    {
        const string FILES_TO_DELETE = "FILES_TO_DELETE";

        public Task Execute(PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            if(pipelineContext.LocalVariables.TryGetEnumerate(FILES_TO_DELETE, out IEnumerable<string> filesToDelete))
            {
                if (filesToDelete != null)
                {
                    Parallel.ForEach(filesToDelete, x =>
                    {
                        var absolutePath = DirectoryHelper.GetAbsolutePath(pipelineContext, x);
                        if (DirectoryHelper.IsDir(absolutePath))
                        {
                            if (Directory.Exists(absolutePath))
                                Directory.Delete(absolutePath, true);
                        }
                        else {
                            if (File.Exists(absolutePath))
                                File.Delete(absolutePath);
                        }
                        
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}

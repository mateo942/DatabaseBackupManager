using BackupManager.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class DeleteFilePipeline : PipelineBase<BackupDatabase>
    {
        public override Task Execute(BackupDatabase command, Variables variables, CancellationToken cancellationToken)
        {
            var filesToDelete = variables.GetFilesToDelete();
            if(filesToDelete != null)
            {
                Parallel.ForEach(filesToDelete, x =>
                {
                    if (File.Exists(x))
                        File.Delete(x);
                });
            }

            return Task.CompletedTask;
        }
    }
}

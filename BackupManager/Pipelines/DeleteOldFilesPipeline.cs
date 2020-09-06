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
    public class DeleteOldFilesPipeline : PipelineBase<BackupDatabase>
    {
        public override Task Execute(BackupDatabase command, Variables variables, CancellationToken cancellationToken)
        {
            if (command.FileExpireDays == 0)
                return Task.CompletedTask;

            var expireDate = DateTime.UtcNow.AddDays(command.FileExpireDays * -1);

            var files = Directory.GetFiles(command.OutputDirectory);
            Parallel.ForEach(files, file =>
            {
                var creationDate = File.GetCreationTime(file);
                if (creationDate < expireDate)
                    File.Delete(file);
            });

            return Task.CompletedTask;
        }
    }
}

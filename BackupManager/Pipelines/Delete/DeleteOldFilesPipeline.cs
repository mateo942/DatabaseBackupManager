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
    public class DeleteOldFilesPipeline : IPipeline
    {
        const string EXPIRE_DAYS = "EXPIRE_DAYS";
        const string EXPIRE_HOURS = "EXPIRE_HOURS";
        const string DIRECTORIES_TO_DELETE = "DIRECTORIES_TO_DELETE";


        public Task Execute(PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var expireDate = now.AddDays(-128);

            if (pipelineContext.LocalVariables.TryGet(EXPIRE_DAYS, out int expDays))
            {
                expireDate = now.AddDays(expDays * -1);
            }
            else if (pipelineContext.LocalVariables.TryGet(EXPIRE_HOURS, out int expHours))
            {
                expireDate = now.AddDays(expDays * -1);
            }

            if (pipelineContext.LocalVariables.TryGetEnumerate(DIRECTORIES_TO_DELETE, out IEnumerable<string> directoriesToDelete))
            {
                if (directoriesToDelete != null)
                {
                    foreach (var item in directoriesToDelete)
                    {
                        if (!Directory.Exists(item))
                            continue;

                        var files = Directory.GetFiles(item);
                        Parallel.ForEach(files, file =>
                        {
                            var creationDate = File.GetCreationTime(file);
                            if (creationDate < expireDate)
                                File.Delete(file);
                        });
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}

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
    public class ZipPipeline : PipelineBase<BackupDatabase>
    {
        public override Task Execute(BackupDatabase command, Variables variables, CancellationToken cancellationToken)
        {
            var backupPath = variables.GetBackupPath();

            var backupDirectory = Path.GetDirectoryName(backupPath);
            var backupName = Path.GetFileName(backupPath);
            var pathTmpDirectory = Path.Combine(backupDirectory, Guid.NewGuid().ToString());
            Directory.CreateDirectory(pathTmpDirectory);

            File.Copy(backupPath, Path.Combine(pathTmpDirectory, backupName));

            var outputZipPath = Path.Combine(backupDirectory, DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip");
            ZipFile.CreateFromDirectory(pathTmpDirectory, outputZipPath);

            Directory.Delete(pathTmpDirectory, true);

            variables.AddFilesToDelete(backupPath);

            variables.AddFilesToUpload(outputZipPath);
            variables.RemoveFilesToUpload(x => x == backupPath);

            return Task.CompletedTask;
        }
    }
}

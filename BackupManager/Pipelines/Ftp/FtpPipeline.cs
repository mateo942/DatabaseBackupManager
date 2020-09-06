using BackupManager.Settings;
using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class FtpPipeline : PipelineBase<BackupDatabase>
    {
        private readonly ILogger<FtpPipeline> _logger;

        public FtpPipeline(ILogger<FtpPipeline> logger)
        {
            _logger = logger;
        }

        public async override Task Execute(BackupDatabase command, Variables variables, CancellationToken cancellationToken)
        {
            var ftpSettings = command.BackupProvider.FtpProvider;
            if (ftpSettings.Enabled == false)
                return;

            var filesToUpload = variables.GetFilesToUpload();
            if(filesToUpload != null && filesToUpload.Any())
            {
                FtpClient client = null;
                try
                {
                    client = new FtpClient(ftpSettings.Host, ftpSettings.Port,
                    new System.Net.NetworkCredential(ftpSettings.Username, ftpSettings.Password));

                    _logger.LogInformation("Connect with FTP");
                    await client.ConnectAsync(cancellationToken);

                    foreach (var item in filesToUpload)
                    {
                        var fileName = Path.GetFileName(item);
                        _logger.LogInformation("Uploading: {0} ...", fileName);
                        await client.UploadFileAsync(item, ftpSettings.Folder);
                    }

                } catch(Exception ex)
                {
                    _logger.LogError(ex, "Error upload file via FTP");
                }
                finally
                {
                    if (client != null)
                        await client.DisconnectAsync();
                }
            }
        }
    }
}

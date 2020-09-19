using BackupManager.Settings;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Pipeline;
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
    public class FtpPipeline : IPipeline
    {
        const string HOST = "FTP_HOST";
        const string PORT = "FTP_PORT";
        const string USERNAME = "FTP_USERNAME";
        const string PASSWORD = "FTP_PASSWORD";
        const string PATH = "FTP_PATH";
        const string FILES_TO_UPLOAD = "FILES_TO_UPLOAD";

        private readonly ILogger<FtpPipeline> _logger;

        public FtpPipeline(ILogger<FtpPipeline> logger)
        {
            _logger = logger;
        }

        public async Task Execute(PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            var host = pipelineContext.GetVariable<string>(HOST);
            var port = pipelineContext.GetVariable<int>(PORT);
            var username = pipelineContext.GetVariable<string>(USERNAME);
            var password = pipelineContext.GetVariable<string>(PASSWORD);
            var path = pipelineContext.GetVariable<string>(PATH);

            if(pipelineContext.LocalVariables.TryGetEnumerate<string>(FILES_TO_UPLOAD, out IEnumerable<string> filesToUpload))
            {
                FtpClient client = null;
                try
                {
                    client = new FtpClient(host, port,
                    new System.Net.NetworkCredential(username, password));

                    _logger.LogInformation("Connect with FTP");
                    await client.ConnectAsync(cancellationToken);

                    foreach (var item in filesToUpload)
                    {
                        var fileName = Path.GetFileName(item);
                        _logger.LogInformation("Uploading: {0} ...", fileName);
                        await client.UploadFileAsync(item, path);
                    }
                }
                catch (Exception ex)
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

using BackupManager.Helpers;
using BackupManager.Notification;
using BackupManager.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using Pipeline;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class MsSQLNotification : NotificationMessage
    {
        public DateTime DateTimeUtc { get; set; }
        public string Database { get; set; }
        public string BackupType { get; set; }
        public Exception Exception { get; set; }

        public override string Action
            => "BACKUP";

        public MsSQLNotification(string database, string type)
        {
            DateTimeUtc = DateTime.UtcNow;

            Database = database;
            BackupType = type;

            Type = NotificationType.Info;
        }

        public static MsSQLNotification StartBackup(string database, BackupType type)
        {
            var tmp = new MsSQLNotification(database, type.ToString());

            return tmp;
        }

        public static MsSQLNotification EndBackup(string database, BackupType type)
        {
            var tmp = new MsSQLNotification(database, type.ToString());
            tmp.Type = NotificationType.Success;

            return tmp;
        }

        public static MsSQLNotification ErrorBackup(string database, BackupType type, Exception ex)
        {
            var tmp = new MsSQLNotification(database, type.ToString());
            tmp.Exception = ex;
            tmp.Type = NotificationType.Error;

            return tmp;
        }
    }

    public class BackupCommand : IPipelineCommand
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public BackupType BackupType { get; set; }
    }

    public enum BackupType
    {
        Full,
        Diff
    }

    public class MsSQLBackupPipeline : IPipeline<BackupCommand>
    {
        const string OUTPUT_BAK = "OUTPUT_BAK";

        private readonly ILogger<MsSQLBackupPipeline> _logger;
        private readonly IMediator _mediator;

        public MsSQLBackupPipeline(ILogger<MsSQLBackupPipeline> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task Execute(BackupCommand command, PipelineContext pipelineContext, CancellationToken cancellationToken)
        {
            var outputBackupPath = pipelineContext.LocalVariables.Get<string>(OUTPUT_BAK);
            outputBackupPath = DirectoryHelper.GetAbsolutePath(pipelineContext, outputBackupPath);

            try
            {
                _logger.LogInformation("Connectig to server...");
                await _mediator.Send(NotificationRequest.Send(MsSQLNotification.StartBackup(command.DatabaseName, command.BackupType)));

                using (var sqlConnection = new SqlConnection(command.ConnectionString))
                {
                    sqlConnection.FireInfoMessageEventOnUserErrors = true;
                    sqlConnection.InfoMessage += SqlConnection_InfoMessage;
                    await sqlConnection.OpenAsync(cancellationToken);

                    _logger.LogInformation("Connected");
                    _logger.LogInformation("Creating backup...");

                    string name = PathHelper.GetPathWithVariable(pipelineContext, outputBackupPath);

                    string type = string.Empty;
                    if (command.BackupType == BackupType.Diff)
                    {
                        type = "DIFFERENTIAL, ";
                    }

                    var sqlCommand = string.Format("BACKUP DATABASE [{0}] TO DISK = '{1}' WITH {4} NOFORMAT, DESCRIPTION = '{2}', NAME = '{3}', STATS = 5",
                                command.DatabaseName, name, "AutoBackup", DateTime.Now.ToShortDateString(), type);

                    using (var cmd = new SqlCommand(
                            sqlCommand,
                            sqlConnection
                        ))
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }

                    _logger.LogInformation("Backup created: {0}", name);
                    await _mediator.Send(NotificationRequest.Send(MsSQLNotification.EndBackup(command.DatabaseName, command.BackupType)));

                    sqlConnection.InfoMessage -= SqlConnection_InfoMessage;
                    sqlConnection.FireInfoMessageEventOnUserErrors = false;

                    _logger.LogInformation("Closing connection...");
                    await sqlConnection.CloseAsync();
                    _logger.LogInformation("Closed");
                }
            }
            catch (Exception ex)
            {
                await _mediator.Send(NotificationRequest.Send(MsSQLNotification.ErrorBackup(command.DatabaseName, command.BackupType, ex)));
                _logger.LogError(ex, $"Error backup database: '{command.DatabaseName}'");

                throw;
            }
        }

        private void SqlConnection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            foreach (SqlError item in e.Errors)
            {
                if(item.Class > 10)
                {
                    _logger.LogError(item.Message);
                } else
                {
                    _logger.LogInformation(item.Message);
                }
            }
        }
    }
}

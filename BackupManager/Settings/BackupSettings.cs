using BackupManager.Pipelines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BackupManager.Settings
{
    public class BackupSettings
    {
        public IEnumerable<BackupDatabase> BackupDatabases { get; set; }
    }

    public enum BackupType
    {
        Full,
        Diff
    }

    public class BackupDatabase: IPipelineCommand
    {
        public string Id { get; set; }
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string OutputDirectory { get; set; }
        public BackupType Type { get; set; }
        public string Cron { get; set; }
        public int FileExpireDays { get; set; }
        public IDictionary<string, string> Variables { get; set; }

        public BackupProvider BackupProvider { get; set; }

        public IEnumerable<string> Pipeline { get; set; } //Backup, Zip, DeleteLocalBak, Move zip
    }

    public class BackupProvider
    {
        //FTP
        public FtpProvider FtpProvider { get; set; }
        //TCP
        public TcpProvider TcpProvider { get; set; }
        public MailProvider MailProvider { get; set; }
    }

    public class FtpProvider
    {
        public bool Enabled { get; set; } = false;
        public string Host { get; set; }
        public int Port { get; set; } = 21;
        public string Username { get; set; }
        public string Password { get; set; }

        public string Folder { get; set; } = "/";
    }

    public class TcpProvider
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }

        public string ApiKey { get; set; }
    }

    public class MailProvider
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string FromMail { get; set; }
        public string ToMail { get; set; }

    }
}

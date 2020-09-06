using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BackupManager.Notification
{
    public interface INotification
    {
        Task Send(NotificationMessage message);
    }
}

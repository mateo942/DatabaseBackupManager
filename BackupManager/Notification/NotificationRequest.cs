using MediatR;

namespace BackupManager.Notification
{
    public class NotificationRequest : IRequest
    {
        public NotificationMessage NotificationMessage { get; private set; }

        private NotificationRequest(NotificationMessage notificationMessage)
        {
            NotificationMessage = notificationMessage;
        }

        public static NotificationRequest Send(NotificationMessage notification)
            => new NotificationRequest(notification);
    }
}

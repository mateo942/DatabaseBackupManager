namespace BackupManager.Notification
{
    public enum NotificationType
    {
        Info = 0,
        Error = 1,
        Success = 2
    }

    public class NotificationMessage
    {
        public NotificationType Type { get; set; }

        public virtual string Action { get; set; }
        public virtual string Message { get; set; }

        public virtual byte[] Data { get; set; }

        protected NotificationMessage()
        {
        }

        public static NotificationMessage Create(NotificationType type, string action, string message)
            => new NotificationMessage()
            {
                Type = type,
                Message = message,
                Action = action
            };

        public static NotificationMessage Success(string action, string message)
            => Create(NotificationType.Success, action, message);

        public static NotificationMessage Info(string action, string message)
            => Create(NotificationType.Info, action, message);

        public static NotificationMessage Error(string action, string message)
            => Create(NotificationType.Error, action, message);

        public NotificationMessage AddData<T>(T instance)
        {
            var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(instance);

            return AddData(data);
        }

        public NotificationMessage AddData(byte[] data)
        {
            Data = data;
            return this;
        }

    }
}

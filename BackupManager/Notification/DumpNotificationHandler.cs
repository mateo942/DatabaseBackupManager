using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Notification
{
    public class DumpNotificationHandler : IRequestHandler<NotificationRequest>
    {
        public Task<Unit> Handle(NotificationRequest request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}

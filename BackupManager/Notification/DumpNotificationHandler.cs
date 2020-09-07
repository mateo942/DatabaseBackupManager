using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Notification
{
    public class DumpNotificationHandler : IRequestHandler<NotificationRequest>
    {
        private readonly ILogger<DumpNotificationHandler> _logger;

        public DumpNotificationHandler(ILogger<DumpNotificationHandler> logger)
        {
            _logger = logger;
        }

        public Task<Unit> Handle(NotificationRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Message: {0}", request.NotificationMessage.Message);

            return Unit.Task;
        }
    }
}

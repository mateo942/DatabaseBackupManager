using BackupManager.Notification;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager
{
    public class TcpBackupManager : IDisposable
    {
        private readonly ILogger<TcpBackupManager> _logger;

        private TcpClient _tcpClient;
        private NetworkStream _networkStream;


        public TcpBackupManager(ILogger<TcpBackupManager> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            Connect("127.0.0.1");
            ReadData();
        }

        private void Connect(String server)
        {
            try
            {
                // Create a TcpClient.
                // Note, for this client to work you need to have a TcpServer
                // connected to the same address as specified by the server, port
                // combination.
                Int32 port = 13000;
                _tcpClient = new System.Net.Sockets.TcpClient(server, port);

                _networkStream = _tcpClient.GetStream();
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e, "ArgumentNullException: {0}", e.Message);
            }
            catch (SocketException e)
            {
                _logger.LogError(e, "SocketException: {0}", e.Message);
            }
        }

        private void ReadData()
        {
            if(_tcpClient != null)
            {
                Task.Run(() =>
                {
                    var data = new Byte[256];
                    while (true)
                    {
                        // String to store the response ASCII representation.
                        String responseData = String.Empty;

                        // Read the first batch of the TcpServer response bytes.
                        Int32 bytes = _networkStream.Read(data, 0, data.Length);
                        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                        _logger.LogInformation("Received: {0}", responseData);
                    }
                });

                Task.Run(async () =>
               {
                   while (true)
                   {
                       Byte[] data = System.Text.Encoding.ASCII.GetBytes("PING");

                       await _networkStream.WriteAsync(data);
                       await Task.Delay(TimeSpan.FromMinutes(1));
                   }
               });
            }
        }

        public void Dispose()
        {
            _networkStream.Close();
            _tcpClient.Close();

            _tcpClient.Dispose();

            _logger.LogInformation("Close tcp connection");
        }

        public Task Write(byte[] data, CancellationToken cancellationToken)
        {
            if (_tcpClient != null)
            {
                if (_networkStream != null)
                    _networkStream.WriteAsync(data, cancellationToken);
            }

            return Task.CompletedTask;
        }
    }

    public class TcpMessageHandler : IRequestHandler<NotificationRequest>
    {
        private readonly TcpBackupManager _tcpBackupManager;

        public TcpMessageHandler(TcpBackupManager tcpBackupManager)
        {
            _tcpBackupManager = tcpBackupManager;
        }

        public async Task<Unit> Handle(NotificationRequest request, CancellationToken cancellationToken)
        {
            var message = request.NotificationMessage;

            var type = message.GetType();
            var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message, type);
            await _tcpBackupManager.Write(data, cancellationToken);

            return Unit.Value;
        }
    }
}

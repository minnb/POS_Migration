using RabbitMQ.Client;
using System;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.DbContext
{
    public class RabbitMQConnection
    {
        private static ConnectionFactory factory;
        private static IConnection connection;
        private static string _connectionString;
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        public static IConnection GetConnection()
        {
            if (connection == null || !connection.IsOpen)
            {
                var arr = StringHelper.ConverStringToArray(_connectionString, ';');
                factory = new ConnectionFactory()
                {
                    HostName = arr[0],
                    Port = 5672,
                    UserName = arr[1],
                    Password = arr[2],
                    RequestedHeartbeat = TimeSpan.FromSeconds(30),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };
                connection = factory.CreateConnection();
            }
            return connection;
        }
    }
}

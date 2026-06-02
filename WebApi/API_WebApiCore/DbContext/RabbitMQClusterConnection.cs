using RabbitMQ.Client;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;
using TCX.API.Common.Helpers;

public static class RabbitMQClusterConnection
{
    private static string _connectionString;
    private static string _userRabbitMQCluster;
    public static void Initialize(string connectionString, string user)
    {
        _connectionString = connectionString;
        _userRabbitMQCluster = user;
    }
    private static readonly Lazy<ConnectionFactory> lazyFactory = new Lazy<ConnectionFactory>(() =>
    {
        var userRabbitNodes = StringHelper.ConverStringToArray(_userRabbitMQCluster, ';');
        var rabbitNodes = StringHelper.ConverStringToArray(_connectionString, ';');
        
        var endpoints = rabbitNodes.Select(ip => new AmqpTcpEndpoint(ip)).ToArray();
        var factory = new ConnectionFactory()
        {
            UserName = userRabbitNodes[0],
            Password = userRabbitNodes[1],
            VirtualHost = "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            //HostName = rabbitNodes[0],
            //EndpointResolverFactory = (service) =>
            //    new DefaultEndpointResolver(
            //        rabbitNodes.Select(ip => new AmqpTcpEndpoint(ip)).ToArray()
            //    )
        };
        return factory;
    });
    public static ConnectionFactory Factory => lazyFactory.Value;
    public static IConnection GetConnection()
    {
        var rabbitNodes = StringHelper.ConverStringToArray(_connectionString, ';');
        var endpoints = rabbitNodes.Select(ip => new AmqpTcpEndpoint(ip)).ToArray();
        return Factory.CreateConnection(endpoints);
    }
}
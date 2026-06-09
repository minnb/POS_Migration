namespace POS.Infrastructure.Messaging;

public sealed class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    public string[] Hosts { get; set; } = [];
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int RequestedHeartbeat { get; set; } = 60;
}

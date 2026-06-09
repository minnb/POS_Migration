namespace POS.Common.Dtos;

public class KafkaMessageCache
{
    public string? Topic { get; set; }
    public string? Type { get; set; }
    public string? Key { get; set; }
    public object? Data { get; set; }
}

public class KafkaMessageData
{
    public string? DataType { get; set; }
    public string? Message { get; set; }
}

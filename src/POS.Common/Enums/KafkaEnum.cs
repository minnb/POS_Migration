namespace POS.Common.Enums;

public enum TypeKafkaEnum
{
    MEMCACHED = 1,
    REDIS = 2,
}

public enum TopicKafkaEnum
{
    pos_data = 0,
    bluepos_loyalty = 1,
}

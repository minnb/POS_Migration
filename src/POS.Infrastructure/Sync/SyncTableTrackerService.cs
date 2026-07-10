using System.Threading.Channels;

namespace POS.Infrastructure.Sync;

/// <inheritdoc cref="ISyncTableTrackerService"/>
public sealed class SyncTableTrackerService : ISyncTableTrackerService
{
    private readonly Channel<(string TableName, long Counter)> _channel;

    public SyncTableTrackerService(Microsoft.Extensions.Options.IOptions<SyncTableTrackerOptions> options)
    {
        var capacity = Math.Max(1, options.Value.ChannelCapacity);
        _channel = Channel.CreateBounded<(string, long)>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <summary>Reader nội bộ — chỉ <see cref="SyncTableCounterFlushWorker"/> (cùng process) tiêu thụ.</summary>
    internal ChannelReader<(string TableName, long Counter)> Reader => _channel.Reader;

    public void Track(string tableName, long counter)
    {
        if (string.IsNullOrWhiteSpace(tableName) || counter <= 0) return;
        _channel.Writer.TryWrite((tableName, counter));
    }
}

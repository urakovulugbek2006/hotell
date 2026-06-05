using HotelOS.Shared.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace HotelOS.Shared.Infrastructure;

public class RedisMessageBroker : IMessageBroker, IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisMessageBroker> _logger;
    private readonly Dictionary<string, IDisposable> _subscriptions;
    private bool _disposed = false;

    public RedisMessageBroker(IConnectionMultiplexer connection, ILogger<RedisMessageBroker> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _subscriber = _connection.GetSubscriber();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptions = new Dictionary<string, IDisposable>();
    }

    public async Task PublishAsync<T>(T eventData, string topic) where T : IEvent
    {
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisMessageBroker));

            var serializedData = JsonConvert.SerializeObject(eventData, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            await _subscriber.PublishAsync(topic, serializedData);
            
            _logger.LogInformation("Published event {EventType} to topic {Topic} with ID {EventId}", 
                eventData.EventType, topic, eventData.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", 
                eventData.EventType, topic);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler) where T : IEvent
    {
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisMessageBroker));

            await _subscriber.SubscribeAsync(topic, async (channel, message) =>
            {
                try
                {
                    var eventData = JsonConvert.DeserializeObject<T>(message!, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                    if (eventData != null)
                    {
                        await handler(eventData);
                        _logger.LogInformation("Successfully handled event {EventType} from topic {Topic}", 
                            eventData.EventType, topic);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle message from topic {Topic}: {Message}", 
                        topic, message);
                }
            });

            _logger.LogInformation("Subscribed to topic {Topic} for event type {EventType}", 
                topic, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to topic {Topic}", topic);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string topic)
    {
        try
        {
            if (_disposed)
                return;

            await _subscriber.UnsubscribeAsync(topic);
            
            if (_subscriptions.ContainsKey(topic))
            {
                _subscriptions[topic].Dispose();
                _subscriptions.Remove(topic);
            }

            _logger.LogInformation("Unsubscribed from topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from topic {Topic}", topic);
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            return _connection.IsConnected && (await _subscriber.PingAsync()).TotalMilliseconds > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (!_disposed)
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription.Dispose();
                }
                _subscriptions.Clear();

                await _connection.CloseAsync();
                _logger.LogInformation("Disconnected from Redis message broker");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from Redis message broker");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
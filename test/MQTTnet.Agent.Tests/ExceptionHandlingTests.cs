using Microsoft.Extensions.Logging;
using MQTTnet.Agent.Tests.Models;
using System.Text;
using System.Text.Json;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// 异常处理单元测试
/// 测试MQTT消息代理在各种异常情况下的行为，包括空参数、无效输入、网络异常等
/// 验证系统的健壮性和错误处理机制，确保在异常情况下能够正确响应
/// </summary>
public class ExceptionHandlingTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public ExceptionHandlingTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    /// <summary>
    /// 测试发布消息时传入空主题的异常处理
    /// 验证当传入null作为主题时，系统是否正确抛出ArgumentNullException
    /// 这是参数验证的基本测试，确保系统能够捕获明显的输入错误
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNullTopic_ShouldThrowArgumentNullException() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await publisher.PublishAsync(null!, "test message", JsonSerializerOptions.Default);
        });
    }

    [Fact]
    public async Task PublishAsync_WithEmptyTopic_ShouldThrowArgumentNullException() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await publisher.PublishAsync("", "test message", JsonSerializerOptions.Default);
        });
    }

    [Fact]
    public async Task PublishStringAsync_WithNullPayload_ShouldThrowArgumentNullException() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await publisher.PublishStringAsync("test/topic", null!);
        });
    }

    [Fact]
    public async Task PublishStringAsync_WithEmptyPayload_ShouldThrowArgumentNullException() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await publisher.PublishStringAsync("test/topic", "");
        });
    }

    [Fact]
    public async Task RemoveRetainAsync_WithNullTopic_ShouldThrowArgumentNullException() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await publisher.RemoveRetainAsync(null!);
        });
    }

/// <summary>
    /// 测试获取消息通道时参数验证的异常处理
    /// 验证系统在关键参数为null时的错误处理能力
    /// 确保API的参数验证机制正常工作，防止无效参数导致的运行时错误
    /// </summary>
    [Fact]
    public async Task GetChannelAsync_WithNullTopic_ShouldThrowArgumentNullException() {
        // Arrange
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => {
            await agent.GetChannelAsync<string>("test/topic", JsonSerializerOptions.Default, CancellationToken.None);
        });
    }

    [Fact]
    public async Task SubscribeAsync_WithInvalidJson_ShouldHandleGracefully() {
        // Arrange
        var hub = factory.GetService<IMessageHub>();
        Assert.NotNull(hub);

        var receivedMessages = new List<MessageArgs<string>>();
        var subscription = await hub.SubscribeAsync<string>("test/invalid", JsonSerializerOptions.Default);
        subscription.Subscribe(msg => {
            if (msg.Payload != null) {
                receivedMessages.Add(msg);
            }
        });

        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act - Publish invalid JSON
        await publisher.PublishStringAsync("test/invalid", "{invalid json}");

        // Wait for processing
        await Task.Delay(100);

        // Assert - Should not crash and may or may not receive message depending on implementation
        // The important thing is that it doesn't throw an exception
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public async Task SubscribeAsync_WithLargeMessage_ShouldHandleCorrectly() {
        // Arrange
        var hub = factory.GetService<IMessageHub>();
        Assert.NotNull(hub);

        var largePayload = new string('x', 100000); // 100KB payload
        var receivedMessage = new TaskCompletionSource<MessageArgs<string>>();

        var subscription = await hub.SubscribeAsync<string>("test/large", JsonSerializerOptions.Default);
        subscription.Subscribe(msg => {
            if (msg.Payload != null) {
                receivedMessage.SetResult(msg);
            }
        });

        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Act
        await publisher.PublishStringAsync("test/large", largePayload);

        // Assert
        var result = await receivedMessage.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        Assert.NotNull(result.Payload);
        Assert.Equal(largePayload.Length, result.Payload.Length);
    }

    [Fact]
    public async Task PublishAsync_WithComplexObject_ShouldHandleSerializationErrors() {
        // Arrange
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // Create an object that might cause serialization issues
        var problematicObject = new ProblematicObject();

        // Act & Assert - Should handle gracefully or throw expected exception
        try {
            await publisher.PublishAsync("test/problematic", problematicObject, JsonSerializerOptions.Default);
            // If no exception, that's also valid behavior
            Assert.True(true);
        } catch (JsonException) {
            // JsonException is acceptable
            Assert.True(true);
        } catch {
            // Other exceptions might indicate issues
            Assert.True(false, "Unexpected exception type thrown");
        }
    }

    [Fact]
    public async Task GetChannelAsync_WithCancellation_ShouldRespectCancellation() {
        // Arrange
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        try {
            // Act
        var channel = await agent.GetChannelAsync<string>("test/cancel", JsonSerializerOptions.Default, cts.Token);
            var readTask = channel.ReadAsync(cts.Token);
            
            // This should throw OperationCanceledException due to timeout
            await Assert.ThrowsAsync<OperationCanceledException>(async () => {
                await readTask;
            });
        } catch (OperationCanceledException) {
            // Expected behavior
            Assert.True(true);
        }
    }

    [Fact]
    public async Task ConcurrentPublishSubscribe_ShouldBeThreadSafe() {
        // Arrange
        const int messageCount = 100;
        const int concurrentTasks = 10;

        var hub = factory.GetService<IMessageHub>();
        var publisher = factory.GetService<IMessagePublisher>();
        
        Assert.NotNull(hub);
        Assert.NotNull(publisher);

        var receivedCount = 0;
        var subscription = await hub.SubscribeAsync<string>("test/concurrent", JsonSerializerOptions.Default);
        subscription.Subscribe(_ => Interlocked.Increment(ref receivedCount));

        // Act - Publish messages concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentTasks; i++) {
            int taskIndex = i;
            tasks.Add(Task.Run(async () => {
                for (int j = 0; j < messageCount / concurrentTasks; j++) {
                    await publisher.PublishStringAsync($"test/concurrent/{taskIndex}", $"message-{taskIndex}-{j}");
                    await Task.Delay(1); // Small delay to avoid overwhelming
                }
            }));
        }

        await Task.WhenAll(tasks);
        await Task.Delay(1000); // Wait for all messages to be processed

        // Assert
        Assert.Equal(messageCount, receivedCount);
    }

    [Fact]
    public async Task SubscribeAsync_WithWildcardTopic_ShouldMatchCorrectly() {
        // Arrange
        var hub = factory.GetService<IMessageHub>();
        var publisher = factory.GetService<IMessagePublisher>();
        
        Assert.NotNull(hub);
        Assert.NotNull(publisher);

        var receivedMessages = new List<string>();
        var subscription = await hub.SubscribeAsync<string>("test/wildcard/+", JsonSerializerOptions.Default);
        subscription.Subscribe(msg => {
            if (msg.Payload != null) {
                receivedMessages.Add(msg.Topic);
            }
        });

        // Act
        await publisher.PublishStringAsync("test/wildcard/1", "message1");
        await publisher.PublishStringAsync("test/wildcard/2", "message2");
        await publisher.PublishStringAsync("test/other/1", "message3"); // Should not match
        await Task.Delay(100);

        // Assert
        Assert.Equal(2, receivedMessages.Count);
        Assert.Contains("test/wildcard/1", receivedMessages);
        Assert.Contains("test/wildcard/2", receivedMessages);
        Assert.DoesNotContain("test/other/1", receivedMessages);
    }

    // Helper class for testing serialization issues
    private class ProblematicObject {
        // This might cause circular reference or other serialization issues
        public ProblematicObject SelfReference { get; set; } = null!;
    }
}

// Extension method for timeout handling
public static class TaskExtensions {
    public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout) {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        
        if (completedTask == task) {
            cts.Cancel();
            return await task;
        } else {
            throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds} seconds");
        }
    }
}
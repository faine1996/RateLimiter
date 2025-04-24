using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RateLimiter.Core;

namespace RateLimiter.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public async Task should_honor_rate_limit_delay()
        {
            //write the brew times
            var timestamps = new List<DateTime>();
            using var cts = new CancellationTokenSource();

            // start the espresso machine
            var limiter = new RateLimiter<string>(async (order) =>
            {
                await Task.Delay(1); //preted were making a quick espresso
                lock (timestamps)
                {
                    timestamps.Add(DateTime.UtcNow);
                    Console.WriteLine($"served {order} at {DateTime.UtcNow:HH:mm:ss.fff}");
                }
            },
            new[] { new RateLimit(2, TimeSpan.FromMilliseconds(300)) }, cts.Token);

            // place three orders at once
            await limiter.Perform("espresso 1");
            await limiter.Perform("espresso 2");
            await limiter.Perform("espresso 3");

            //let the third order finish before stopping to make
            await Task.Delay(500);
            cts.Cancel();

            Assert.Equal(3, timestamps.Count);
            var diff = timestamps[2] - timestamps[1];
            Assert.True(diff >= TimeSpan.FromMilliseconds(300), $"barista poured too fast: waited only {diff.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task orders_should_be_fulfilled_fifo()
        {
            // line up the coffee lovers
            var order = new List<int>();
            using var cts = new CancellationTokenSource();

            // setup a limiter that allows 1 brew every 30ms
            var limiter = new RateLimiter<int>(async id =>
            {
                lock (order)
                {
                    order.Add(id);
                    Console.WriteLine($"served customer {id} at {DateTime.UtcNow:HH:mm:ss.fff}");
                }
            }, new[] { new RateLimit(1, TimeSpan.FromMilliseconds(30)) }, cts.Token);

            // order up
            await limiter.Perform(0);
            await limiter.Perform(1);
            await limiter.Perform(2);
            await Task.Delay(200);
            cts.Cancel();

            Assert.Equal(new List<int> { 0, 1, 2 }, order);
        }
    }
}
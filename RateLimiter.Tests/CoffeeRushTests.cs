using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RateLimiter.Core;

namespace RateLimiter.Tests
{
    public class CoffeeRushTest
    {
        [Fact]
        public async Task dynamic_number_of_customers_should_be_served_in_order()
        {
            // set how many customers are lining up for a hot brew
            int customerCount = 100;

            // blackboard to track who got their drink and when
            var log = new List<(int customer, DateTime servedAt)>();
            int servedCount = 0;
            var lockObj = new object();
            using var cts = new CancellationTokenSource();

            //read house rules from environment or use default
            int limitPerSecond = int.TryParse(Environment.GetEnvironmentVariable("LIMIT_PER_SECOND"), out var lps) ? lps : 20;
            int limitPerMinute = int.TryParse(Environment.GetEnvironmentVariable("LIMIT_PER_MINUTE"), out var lpm) ? lpm : 1000;
            Console.WriteLine($"barista limit: {limitPerSecond}/sec, {limitPerMinute}/min");

            //the barista is ready to pull shots under house rules
            var limiter = new RateLimiter<int>(async (id) =>
            {
                await Task.Delay(1); // a quick pour
                lock (lockObj)
                {
                    log.Add((id, DateTime.UtcNow));
                    servedCount++;
                    Console.WriteLine($"served customer {id} at {DateTime.UtcNow:HH:mm:ss.fff}");
                }
            },
            new[] {
                new RateLimit(limitPerSecond, TimeSpan.FromSeconds(1)),
                new RateLimit(limitPerMinute, TimeSpan.FromMinutes(1))
            }, cts.Token);

            var tasks = new List<Task>();
            for (int i = 0; i < customerCount; i++)
            {
                int id = i;
                tasks.Add(limiter.Perform(id));
            }

            await Task.WhenAll(tasks);
            cts.Cancel();

            Assert.Equal(customerCount, servedCount);
            for (int i = 1; i < log.Count; i++)
            {
                Assert.True(log[i - 1].servedAt <= log[i].servedAt, $"orders out of order: {log[i - 1].customer} before {log[i].customer}");
            }
        }
    }
}
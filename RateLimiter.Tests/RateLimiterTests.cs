using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RateLimiter.Core;


[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RateLimiter.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public async Task should_honor_rate_limit_delay()
        {
            // this is the chalkboard where the barista logs each espresso's timestamp
            var timestamps = new List<DateTime>();
            using var cts = new CancellationTokenSource();

            // our barista follows the house rules: 2 shots every 300ms
            var limiter = new RateLimiter<string>(async (order) =>
            {
                // just enough time for a quick brew
                lock (timestamps) timestamps.Add(DateTime.UtcNow);
            },
            new[] { new RateLimit(2, TimeSpan.FromMilliseconds(300)) }, cts.Token);

            // espresso orders start coming in
            await limiter.Perform("espresso 1");
            await limiter.Perform("espresso 2");
            await limiter.Perform("espresso 3");

            await Task.Delay(500); // give the barista time for that 3rd pour
            cts.Cancel(); // cafe lights off

            Assert.Equal(3, timestamps.Count);

            // we measure how long the barista waited before that 3rd cup
            var diff = timestamps[2] - timestamps[1];

            // allow a little slosh in the timing — the grinder might’ve jammed
            Assert.True(diff >= TimeSpan.FromMilliseconds(280),
                $"barista poured too fast: waited only {diff.TotalMilliseconds}ms");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter.Core
{
    public class RateLimiter<TArg, TResult>
    {
        private readonly Func<TArg, Task<TResult>> _function;
        private readonly List<RateLimit> _rateLimits;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RateLimiter(Func<TArg, Task<TResult>> function, IEnumerable<RateLimit> rateLimits)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
            _rateLimits = rateLimits?.ToList() ?? throw new ArgumentNullException(nameof(rateLimits));

            if (_rateLimits.Count == 0)
            {
                throw new ArgumentException("At least one rate limit must be provided", nameof(rateLimits));
            }
        }

        public async Task<TResult> PerformAsync(TArg argument)
        {
            await _semaphore.WaitAsync(); // Get access to the coffee shop, only one person at a time. Only one customer (thread) can talk to the barista at a time. Wait your turn.
            try
            {
                // Wait politely before ordering coffee
                //If you’ve had too many coffees recently, you wait before ordering. But you don’t freeze the entire shop — you wait asynchronously.
                TimeSpan timeToWait = GetMaxWaitTime();

                if (timeToWait > TimeSpan.Zero)
                {
                    await Task.Delay(timeToWait); 
                }

                // Log your new coffee time
                foreach (var rateLimit in _rateLimits)
                {
                    rateLimit.RecordExecution(); 
                }

                // Execute the function (Actually get the coffee)
                return await _function(argument);
            }
            finally
            {
                _semaphore.Release(); // Leave the coffee shop
            }
        }

        private TimeSpan GetMaxWaitTime()
        {
            TimeSpan maxWaitTime = TimeSpan.Zero;

            foreach (var rateLimit in _rateLimits)
            {
                TimeSpan waitTime = rateLimit.GetTimeToWait();
                if (waitTime > maxWaitTime)
                {
                    maxWaitTime = waitTime;
                }
            }

            return maxWaitTime;
        }
    }
}
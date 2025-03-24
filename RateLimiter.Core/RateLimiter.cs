using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

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
            await _semaphore.WaitAsync(); // get access to the coffee shop, only one person at a time. Only one customer (thread) can talk to the barista at a time. Wait your turn.
            try
            {
                // wait politely before ordering coffee
                // if you’ve had too many coffees recently, you wait before ordering but you don’t freeze the entire shop — you wait asynchronously
                TimeSpan timeToWait = GetMaxWaitTime();

                if (timeToWait > TimeSpan.Zero)
                {
                    await Task.Delay(timeToWait); 
                }

                // log your new coffee time
                foreach (var rateLimit in _rateLimits)
                {
                    rateLimit.RecordExecution(); 
                }

                // execute the function (Actually get the coffee)
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
                    maxWaitTime = waitTime; //if the wait time for this rule is longer than the longest wait time so far, update it. always use the longest wait time across all rules.
                }
            }

            return maxWaitTime; //if any rule says "Wait 5 minutes," even if others say "Wait 10 seconds," the customer waits 5 minutes.
        }
    }
}
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
            TimeSpan timeToWait;

            // ask the doorman how long you need to wait before entering the coffee shop
            await _semaphore.WaitAsync(); // Only one customer can talk to the doorman at a time
            try
            {
                timeToWait = GetMaxWaitTime(); //how long do you have to wait before you can have another coffee?
            }
            finally
            {
                _semaphore.Release(); //let others check their wait time
            }

            // wait outside the shop (if needed) — but don’t block others from asking about their wait time
            if (timeToWait > TimeSpan.Zero)
            {
                await Task.Delay(timeToWait); //wait politely in line without freezing the entire shop
            }

            // come back, tell the doorman you’re ready to order, and log your new coffee order
            await _semaphore.WaitAsync(); // enter the shop again to talk to the barista
            try
            {
                foreach (var rateLimit in _rateLimits)
                {
                    rateLimit.RecordExecution(); //record that you’ve had another coffee at this time
                }

                //actually get your coffee (running the 'get coffee' function)
                return await _function(argument);
            }
            finally
            {
                _semaphore.Release(); // leave the coffee shop so others can come in
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
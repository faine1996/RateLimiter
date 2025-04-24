using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RateLimiter.Core
{
    public class RateLimiter<TArg>
    {
        // this is the order counter where all customers queue up politely
        // this is our order counter with limited cups and space
        private readonly Channel<Request<TArg>> _queue = Channel.CreateBounded<Request<TArg>>(new BoundedChannelOptions(1000)
        {
            // if the cafe is full, new customers will have to wait
            FullMode = BoundedChannelFullMode.Wait,

            // one barista at the espresso machine
            SingleReader = true,

            // multiple customers placing orders at once
            SingleWriter = false
        });


        // the house rules - how many drinks per time window
        private readonly List<RateLimit> _limits;

        // the action to perform when a customer's turn comes up
        private readonly Func<TArg, Task> _action;

        private readonly CancellationTokenSource _internalCts = new();
        private readonly Task _backgroundWorker;

        public RateLimiter(Func<TArg, Task> action, RateLimit[] limits, CancellationToken token = default)
        {
            _action = action;
            _limits = limits.ToList();

            // link our shutdown token with any external timer
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_internalCts.Token, token);

            // start the barista shift
            _backgroundWorker = Task.Run(() => ProcessQueueAsync(linkedCts.Token));
        }

        public Task Perform(TArg arg)
        {
            var request = new Request<TArg>(arg);
            _queue.Writer.TryWrite(request); // customer joins the line
            return request.TaskCompletion.Task;
        }

        // clean shutdown when closing the cafe
        public async Task StopAsync()
        {
            _queue.Writer.Complete(); // no more orders accepted
            _internalCts.Cancel();
            await _backgroundWorker;
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            try
            {
                await foreach (var request in _queue.Reader.ReadAllAsync(token))
                {
                    while (!token.IsCancellationRequested)
                    {
                        var now = DateTime.UtcNow;
                        bool canProceed = _limits.All(limit => limit.CanProceed(now));

                        if (canProceed)
                        {
                            foreach (var limit in _limits)
                            {
                                limit.Record(now); // mark the shot as pulled on each rule board
                            }

                            try
                            {
                                // serve the drink here, inline and in order
                                await _action(request.Arg);
                                request.TaskCompletion.SetResult();
                            }
                            catch (Exception ex)
                            {
                                request.TaskCompletion.SetException(ex);
                            }

                            break;
                        }
                        else
                        {
                            // figure out when the next drink is allowed
                            var nextTimes = _limits
                                .Select(l => l.NextAvailable(now))
                                .Where(t => t > now)
                                .ToList();

                            var earliest = nextTimes.Any() ? nextTimes.Min() : now.AddMilliseconds(50);
                            var delay = earliest - now;

                            // barista takes a short break before checking again
                            if (delay > TimeSpan.Zero)
                            {
                                await Task.Delay(delay, token);
                            }

                            continue;
                        }


                        // barista checks the watch and waits before next shot
                        await Task.Delay(50, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // the barista got the signal to close down quietly
            }
        }

    }
}

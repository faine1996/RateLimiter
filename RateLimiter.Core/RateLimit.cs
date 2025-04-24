using System;
using System.Collections.Generic;
using System.Linq;

namespace RateLimiter.Core
{
    public class RateLimit
    {
        private readonly int _maxOperations;
        private readonly TimeSpan _timeWindow;
        private readonly Queue<DateTime> _executionTimes = new Queue<DateTime>();

        public RateLimit(int maxOperations, TimeSpan timeWindow)
        {
            _maxOperations = maxOperations > 0 ? maxOperations : throw new ArgumentException("Maximum operations must be > 0", nameof(maxOperations));
            _timeWindow = timeWindow > TimeSpan.Zero ? timeWindow : throw new ArgumentException("Time window must be > 0", nameof(timeWindow));
        }

        // gets the time to wait before the next operation can be performed. time to wait until your are allowed your next coffee
        public TimeSpan GetTimeToWait()
        {
            lock (_executionTimes)
            {
                // remove expired coffees ie people that have waited long enough so their times are not in the queue anymore
                RemoveExpiredTimestamps();

                // if you haven’t had too many coffees yet you can have another
                if (_executionTimes.Count < _maxOperations)
                {
                    Console.WriteLine($"Allowed: {_executionTimes.Count}/{_maxOperations} coffees in the last {_timeWindow.TotalSeconds}s");
                    return TimeSpan.Zero;
                }

                // you’ve hit your coffee per time limit lets figure out how long you have to wait
                DateTime oldestTimestamp = _executionTimes.Peek();
                DateTime earliestTimeForNextOperation = oldestTimestamp.Add(_timeWindow);
                TimeSpan timeToWait = earliestTimeForNextOperation - DateTime.UtcNow;

                Console.WriteLine($"Waiting: {_executionTimes.Count}/{_maxOperations} coffees in the last {_timeWindow.TotalSeconds}s → wait {Math.Max(timeToWait.TotalMilliseconds, 0):F0}ms");

                return timeToWait > TimeSpan.Zero ? timeToWait : TimeSpan.Zero;
            }
        }



        // records a new coffee you just had
        public void RecordExecution()
        {
            lock (_executionTimes)
            {
                RemoveExpiredTimestamps();//Clean out the old coffee times
                _executionTimes.Enqueue(DateTime.UtcNow); //Then log that I just had one now
            }
        }

        // a helper function that cleans up your any coffees you had that fall outside the given time window
        private void RemoveExpiredTimestamps()
        {
            DateTime cutoffTime = DateTime.UtcNow.Subtract(_timeWindow);
            while (_executionTimes.Count > 0 && _executionTimes.Peek() < cutoffTime)
            {
                _executionTimes.Dequeue(); //dequeue() removes the element first in line as this is a queue like a line at a coffee shop
            }
        }
    }
}
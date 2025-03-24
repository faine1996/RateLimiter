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
                // remove coffees that happened outside of the current time limit
                RemoveExpiredTimestamps();

                // if we haven't reached the limit, no need to wait. if you havent had your maximum amount of coffees then you can have more
                if (_executionTimes.Count < _maxOperations)
                {
                    return TimeSpan.Zero; //If we are below the limit of maximum number of coffees no need to wait we can have another one yay
                }

                // I’ve already had the max number of coffees. when will the oldest one no longer count so I can have another?
                DateTime oldestTimestamp = _executionTimes.Peek();
                DateTime earliestTimeForNextOperation = oldestTimestamp.Add(_timeWindow);
                TimeSpan timeToWait = earliestTimeForNextOperation - DateTime.UtcNow;

                return timeToWait > TimeSpan.Zero ? timeToWait : TimeSpan.Zero;
                // the difference here is that you dont remove the oldest coffee you had just check when you can have the next its the helper function that removes the greater than timeframe coffees
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
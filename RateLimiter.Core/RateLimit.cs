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
            _maxOperations = maxOperations > 0 ? maxOperations : throw new ArgumentException("Maximum operations must be > 0", nameof(maxOperations)); //nameof is just a nice way of using a variable when you want a string so you dont have to update the string and only the variable when you want to change it
            _timeWindow = timeWindow > TimeSpan.Zero ? timeWindow : throw new ArgumentException("Time window must be > 0", nameof(timeWindow));
        }

        // Gets the time to wait before the next operation can be performed
        public TimeSpan GetTimeToWait()
        {
            lock (_executionTimes)
            {
                // Remove outdated timestamps
                RemoveExpiredTimestamps();

                // If we haven't reached the limit, no need to wait
                if (_executionTimes.Count < _maxOperations)
                {
                    return TimeSpan.Zero; //If we are below the limit of maximum number of coffees no need to wait we can have another one yay
                }

                // I’ve already had the max number of coffees. When will the oldest one no longer count so I can have another?
                DateTime oldestTimestamp = _executionTimes.Peek();
                DateTime earliestTimeForNextOperation = oldestTimestamp.Add(_timeWindow);
                TimeSpan timeToWait = earliestTimeForNextOperation - DateTime.UtcNow;

                return timeToWait > TimeSpan.Zero ? timeToWait : TimeSpan.Zero;
                // Difference here is that you dont remove the oldest coffee you had just check when you can have the next its the helper function that removes the greater than timeframe coffees
            }
        }

        // Records a new operation execution
        public void RecordExecution()
        {
            lock (_executionTimes)
            {
                RemoveExpiredTimestamps();//Clean out the old coffee times
                _executionTimes.Enqueue(DateTime.UtcNow); //Then log that I just had one now
            }
        }

        // Helper method to remove timestamps outside the time window
        private void RemoveExpiredTimestamps()
        {
            DateTime cutoffTime = DateTime.UtcNow.Subtract(_timeWindow);
            while (_executionTimes.Count > 0 && _executionTimes.Peek() < cutoffTime)
            {
                _executionTimes.Dequeue(); //Dequeue() removes the old coffee timestamps that are no longer within the window you're checking against.
            }
        }
    }
}
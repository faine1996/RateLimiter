using System;
using System.Collections.Concurrent;

namespace RateLimiter.Core
{
    public class RateLimit
    {
        //how many drinks we serve in one session
        private readonly int _maxRequests;

        //how long that session lasts
        private readonly TimeSpan _timeWindow;

        //when each drink was served
        private readonly ConcurrentQueue<DateTime> _timestamps = new();

        public RateLimit(int maxRequests, TimeSpan timeWindow)
        {
            _maxRequests = maxRequests;
            _timeWindow = timeWindow;
        }

        //check if the barista is allowed to serve now
        public bool CanProceed(DateTime now)
        {
            lock (_timestamps)
            {
                //clean up any drinks that are past their window
                while (_timestamps.TryPeek(out var ts) && ts <= now - _timeWindow)
                {
                    _timestamps.TryDequeue(out _);
                }

                return _timestamps.Count < _maxRequests;
            }
        }

        //log that a new drink has been served
        public void Record(DateTime now)
        {
            _timestamps.Enqueue(now);

            lock (_timestamps)
            {
                while (_timestamps.TryPeek(out var ts) && ts <= now - _timeWindow)
                {
                    _timestamps.TryDequeue(out _);
                }
            }
        }

        //find out when the next drink can be served
        public DateTime NextAvailable(DateTime now)
        {
            lock (_timestamps)
            {
                if (_timestamps.Count < _maxRequests)
                    return now;

                if (_timestamps.TryPeek(out var oldest))
                {
                    return oldest + _timeWindow;
                }

                //fallback if the queue is empty somehow
                return now + TimeSpan.FromMilliseconds(50);
            }
        }
    }
}

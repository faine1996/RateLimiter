
# A thread-safe, async rate limiter in c# using the sliding window algorithm — ready to handle high-concurrency and custom limits.

## Project overview

this rate limiter:

- accepts a user-defined function (like an api call)  
- wraps it with one or more sliding window rate limits  
- delays execution when needed to respect all defined limits  
- is fully async, thread-safe, and dockerized  

## features

- supports multiple concurrent rate limits (e.g. 100/sec and 1000/min)  
- preserves strict fifo (first-in-first-out) request ordering  
- optimized for performance: the limiter doesn’t block while executing user actions  
- docker-compatible for consistent test execution and deployment  

---

## run tests with docker (recommended)

**requirements**: docker desktop (windows/mac) or docker engine (linux)

```bash
git clone https://github.com/faine1996/RateLimiter.git
cd RateLimiter
```
# build the container
```bash
docker build -t ratelimiter-demo .
```
# run with default limits (20/sec, 1000/min)
```bash
docker run --rm -e LIMIT_PER_SECOND=20 -e LIMIT_PER_MINUTE=1000 ratelimiter-demo
```

why sliding window?
sliding windows give more accurate rate enforcement compared to fixed windows — especially for bursty or high-throughput systems.
they count actions in real-time rather than batching by whole seconds or minutes.

customization
you can control the rate limits via env variables:

```bash

docker run -e LIMIT_PER_SECOND=10 -e LIMIT_PER_MINUTE=500 ratelimiter-demo
```

future ideas

observability metrics (queue size, wait times)

backpressure support (reject or throttle under extreme load)

dynamic config via api
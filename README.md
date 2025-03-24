# RateLimiter (C# + Docker)

A custom, thread-safe rate limiter implemented in C# using the **sliding window** algorithm. It supports **multiple simultaneous rate limits** (e.g., 10/sec, 100/min, 1000/day) and guarantees all limits are honored — even when accessed from multiple threads.

---

## 📦 Project Overview

This rate limiter:
- Accepts a user-provided function (e.g., an API call).
- Wraps it with one or more sliding window rate limits.
- Delays execution when needed to ensure **no limit is exceeded**.
- Is **fully async**, **thread-safe**, and **Dockerized**.

---

## 🚀 Getting Started

### 🐳 Run with Docker (Recommended)

> ✅ Requires Docker Desktop (Windows/macOS) or Docker Engine (Linux)

```bash
git clone https://github.com/faine1996/RateLimiter.git
cd RateLimiter

docker build -t ratelimiter-demo .

docker run --rm ratelimiter-demo

dotnet run --project RateLimiter.Demo

Why Sliding Window?
This implementation uses the sliding window algorithm instead of a fixed (absolute) window.

Sliding Window — Pros:
Enforces rate limits more accurately over time

Prevents bursting right before/after a time boundary

Ensures fairness between fast and slow clients

Used by many real-world APIs (e.g. GitHub, Stripe)

Sliding Window — Cons:
Requires storing a queue of recent timestamps

Slightly more complex to implement and maintain

Can use more memory if the time window is large

Compared to Fixed Window:
Fixed window resets limits at exact intervals (e.g. every 10 seconds or every minute):

Easier to implement

But allows bursts at window edges (e.g., 10 calls at 11:59:59, 10 more at 12:00:00)

That’s why sliding window was chosen — it provides smoother and more predictable throttling.
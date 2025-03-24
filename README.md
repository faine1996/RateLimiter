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

## Getting Started

### 🐳 Run with Docker (Recommended)

> ✅ Requires Docker Desktop (Windows/macOS) or Docker Engine (Linux)

```bash
git clone https://github.com/faine1996/RateLimiter.git
cd RateLimiter

docker build -t ratelimiter-demo .

docker run --rm ratelimiter-demo

dotnet run --project RateLimiter.Demo


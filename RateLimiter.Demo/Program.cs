using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RateLimiter.Core;

namespace RateLimiter.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("RateLimiter Demo");

            // in this coffee shop, each customer can only have:
            // - 100 coffees per second
            // - 200 coffees per minute

            var rateLimits = new List<RateLimit>
            {
                new RateLimit(100, TimeSpan.FromSeconds(1)),
                new RateLimit(200, TimeSpan.FromMinutes(1)),
            };


            // this is the barista: they take an order (message), spend 100ms making the drink, and return a friendly message
            Func<string, Task<string>> apiCall = async message =>
            {
                await Task.Delay(100); // simulate coffee preparation time
                Console.WriteLine($"API call executed: {message} at {DateTime.Now:HH:mm:ss.fff}");
                return $"Response for: {message}";
            };

            // the RateLimiter method is our doorman. He ensures no customer breaks the coffee shop rules.
            var rateLimiter = new Core.RateLimiter<string, string>(apiCall, rateLimits);

            Console.WriteLine("Making rapid calls to test rate limiting...");

            var tasks = new List<Task>();
            int totalRequests = 100;

            // 100 customers rush in at once to stress test the RateLimiter.
            // and waits outside if they’ve had too many coffees recently.
            for (int i = 0; i < totalRequests; i++)
            {
                int callNumber = i + 1;
                tasks.Add(Task.Run(async () =>
                {
                    var start = DateTime.UtcNow;
                    Console.WriteLine($"{start:HH:mm:ss.fff} - Starting Call {callNumber}");

                    string result = await rateLimiter.PerformAsync($"Call {callNumber}");

                    var end = DateTime.UtcNow;
                    Console.WriteLine($"{end:HH:mm:ss.fff} - Received: {result} (Took {(end - start).TotalMilliseconds} ms)");
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Demo completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}

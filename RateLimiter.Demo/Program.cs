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

            //In this shop, each person can only have:10 coffees per second,100 coffees per minute,1000 per day.
            var rateLimits = new List<RateLimit>
            {
                new RateLimit(10, TimeSpan.FromSeconds(1)),
                new RateLimit(100, TimeSpan.FromMinutes(1)),
                new RateLimit(1000, TimeSpan.FromDays(1))
            };

            // This is a barista: they take an order (message), spend 100ms making the drink, then say “Here’s the coffee!"
            Func<string, Task<string>> apiCall = async message =>
            {
                await Task.Delay(100); // Simulate work
                Console.WriteLine($"API call executed: {message} at {DateTime.Now:HH:mm:ss.fff}");
                return $"Response for: {message}";
            };


            // Rate-limiting doorman at the barista’s counter. No customer is allowed to order unless they respect the rules. If they’ve had too many drinks recently, the doorman tells them to wait before ordering again.
            var rateLimiter = new Core.RateLimiter<string, string>(apiCall, rateLimits);

            // Test with rapid calls
            Console.WriteLine("Making rapid calls to test rate limiting...");
            var tasks = new List<Task>();
            // 20 customers all at once. Doorman checks if they are under the max coffees they allowed and if not makes them wait the specified time
            for (int i = 0; i < 20; i++)     
            {
                int callNumber = i + 1;
                tasks.Add(Task.Run(async () =>
                {
                    string result = await rateLimiter.PerformAsync($"Call {callNumber}");
                    Console.WriteLine($"Received: {result}");
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Demo completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
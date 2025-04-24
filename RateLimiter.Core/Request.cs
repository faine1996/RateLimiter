using System.Threading.Tasks;

namespace RateLimiter.Core
{
    // this is a coffee order - holds the customer's request and their wait ticket
    public class Request<TArg>
    {
        //the drink order (e.g. an api call argument)
        public TArg Arg { get; }

        //the ticket the customer will wait on until their order is ready
        public TaskCompletionSource TaskCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Request(TArg arg)
        {
            Arg = arg;
        }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleProject
{
    public class SimpleServiceServer : SimpleServiceServerBase
    {
        public override Task<RandomNumberResponse> GetRandomNumber(
            RandonNumberRequest request, 
            CancellationToken cancellationToken)
        {
            var rng = new Random();
            var result = new RandomNumberResponse();
            int min = request.MinValue;
            int max = request.MaxValue;
            if (min > max)
                (min, max) = (max, min);
            for (int i = 0; i<request.Count; ++i)
            {
                if (min == max)
                    result.Number.Add(min);
                else result.Number.Add(rng.Next(min, max));
            }
            return Task.FromResult(result);
        }
    }
}

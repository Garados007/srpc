using sRPC.Test.Proto;
using System;
using System.Threading.Tasks;

namespace sRPC.Test.SimpleService
{
    public class Server : SimpleServiceServerBase
    {
        public override Task<SqrtResponse> Sqrt(SqrtRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            return Task.FromResult(new SqrtResponse
            {
                Value = request.Value < 0 ? double.NaN : Math.Sqrt(request.Value),
            });
        }
    }
}

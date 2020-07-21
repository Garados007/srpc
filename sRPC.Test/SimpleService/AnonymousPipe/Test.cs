using Microsoft.VisualStudio.TestTools.UnitTesting;
using sRPC.Pipes;
using sRPC.Test.Proto;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace sRPC.Test.SimpleService.AnonymousPipe
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public async Task AnonymousPipe_FetchNumbersTest()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Inconclusive("Cannot test anonymous pipes because this is a Windows feature");

            using var server = new AnonymouseApiServer<Server>();
            server.GetPipeHandles(out string inputPipe, out string outputPipe);

            using var client = new AnonymousApiClient<SimpleServiceClient>(outputPipe, inputPipe);

            server.DisposeLocalCopysOfPipeHandle();

            var testNumbers = new[]
            {
                1.0,
                Math.PI,
                0.0,
                -10.0
            };

            foreach (var num in testNumbers)
            {
                var check = num < 0 ? double.NaN : Math.Sqrt(num);
                var response = await client.Api.Sqrt(new SqrtRequest
                {
                    Value = num
                });
                Assert.AreEqual(check, response.Value);
            }
        }
    }
}

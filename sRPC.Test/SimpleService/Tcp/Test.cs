using Microsoft.VisualStudio.TestTools.UnitTesting;
using sRPC.TCP;
using sRPC.Test.Proto;
using System;
using System.Net;
using System.Threading.Tasks;

namespace sRPC.Test.SimpleService.Tcp
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public async Task Tcp_FetchNumbersTest()
        {
            using var server = new TcpApiServer<Server>(new IPEndPoint(IPAddress.Loopback, 0));

            using var client = new TcpApiClient<SimpleServiceClient>(server.EndPoint);
            await client.WaitConnect;

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
                var response = await client.Api.Sqrt(value: num);
                Assert.AreEqual(check, response.Value);
            }
        }
    }
}

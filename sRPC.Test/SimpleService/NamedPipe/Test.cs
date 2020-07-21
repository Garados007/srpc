﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using sRPC.Pipes;
using sRPC.Test.Proto;
using System;
using System.Threading.Tasks;

namespace sRPC.Test.SimpleService.NamedPipe
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public async Task FetchNumbersTest()
        {
            var name = Guid.NewGuid().ToString();

            using var server = new NamedApiServer<Server>(name);

            using var client = new NamedApiClient<SimpleServiceClient>(".", name);
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
                var response = await client.Api.Sqrt(new SqrtRequest
                {
                    Value = num
                });
                Assert.AreEqual(check, response.Value);
            }
        }
    }
}

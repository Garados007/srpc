﻿using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sRPC.Test.Proto;
using sRPC.Utils;
using System;
using System.Threading.Tasks;

namespace sRPC.Test.SimpleService.Streams
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public async Task Streams_FetchNumbersTest()
        {
            using var m1 = new BlockingStream();
            using var m2 = new BlockingStream();

            using var server = new ApiServer<Server>(m1, m2);
            server.Start();

            using var client = new ApiClient<SimpleServiceClient>(m2, m1);
            client.Start();

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


        [TestMethod]
        public async Task Streams_Bidi_FetchNumbersTest()
        {
            using var m1 = new BlockingStream();
            using var m2 = new BlockingStream();

            using var server = new ApiServer<SimpleServiceClient, Server>(m1, m2);
            server.Start();

            using var client = new ApiClient<SimpleServiceClient, Server>(m2, m1);
            client.Start();

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
                var response = await client.RequestApi.Sqrt(value: num);
                Assert.AreEqual(check, response.Value);
            }

            foreach (var num in testNumbers)
            {
                var check = num < 0 ? double.NaN : Math.Sqrt(num);
                var response = await server.RequestApi.Sqrt(value: num);
                Assert.AreEqual(check, response.Value);
            }
        }

        [TestMethod]
        public async Task Streams_CancelRequest()
        {
            using var m1 = new BlockingStream();
            using var m2 = new BlockingStream();

            using var server = new ApiServer<Server>(m1, m2);
            server.Start();

            using var client = new ApiClient<SimpleServiceClient>(m2, m1);
            client.Start();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await client.Api.Indefinite(TimeSpan.FromMilliseconds(100));
            });
        }
    }
}

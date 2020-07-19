using sRPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await SimpleService();
        }

        private static async Task SimpleService()
        {
            //setup basic "network" streams (you can use network streams of course)
            using var m1 = new BlockingStream();
            using var m2 = new BlockingStream();

            //setup server
            using var server = new ApiServer<SimpleServiceServer>(m1, m2);
            server.Start(); //this will start the polling

            //setup client
            using var client = new ApiClient<SimpleServiceClient>(m2, m1);
            client.Start(); //this will start the polling

            //fetch some numbers
            var numbers = (await client.Api.GetRandomNumber(new RandonNumberRequest()
            {
                Count = 5,
                MinValue = 0,
                MaxValue = 100,
            })).Number.ToArray();

            //output the numbers
            Console.WriteLine($"Your random numbers are: {string.Join(", ", numbers)}");
        }
    }
}

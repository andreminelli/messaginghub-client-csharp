﻿using System;
using System.Threading.Tasks;

namespace Takenet.MessagingHub.Client.BasicSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();
        }

        static async Task Init()
        {
            var client = new MessagingHubClient()
                            .UsingAccount("andreminelli", "123456")
                            .AddMessageReceiver(new PlainTextMessageReceiver(), forMimeType: MediaTypes.PlainText);
            await client.StartAsync();

            Console.WriteLine("Press any key to stop");
            await WaitKeyAsync();
            await client.StopAsync();
        }

        static Task<char> WaitKeyAsync()
        {
            var tcs = new TaskCompletionSource<char>();
            Task.Run(() =>
            {
                var key = Console.ReadKey(false);
                tcs.SetResult(key.KeyChar);
            });
            return tcs.Task;
        }
    }
}

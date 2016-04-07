﻿using System;
using System.Threading.Tasks;
using Lime.Protocol;
using Takenet.MessagingHub.Client.Deprecated.Receivers;

namespace Takenet.MessagingHub.Client.Playground
{
    /// <summary>
    /// Example of a notification receiver
    /// </summary>
    public class PrintNotificationReceiver : NotificationReceiverBase
    {
        public override Task ReceiveAsync(Notification notification)
        {
            Console.WriteLine("Notification of {0} event received. Reason: {1}", notification.Event, notification.Reason);
            return Task.FromResult(0);
        }
    }
}

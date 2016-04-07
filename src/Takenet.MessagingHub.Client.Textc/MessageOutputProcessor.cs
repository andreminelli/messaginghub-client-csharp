﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Takenet.MessagingHub.Client.Deprecated;
using Takenet.Textc;
using Takenet.Textc.Processors;

namespace Takenet.MessagingHub.Client.Textc
{
    public sealed class MessageOutputProcessor : IOutputProcessor
    {
        private readonly Func<IEnvelopeSender> _envelopeSenderFactory;

        public MessageOutputProcessor(Func<IEnvelopeSender> envelopeSenderFactory)
        {
            if (envelopeSenderFactory == null) throw new ArgumentNullException(nameof(envelopeSenderFactory));
            _envelopeSenderFactory = envelopeSenderFactory;
        }

        public Task ProcessOutputAsync(object output, IRequestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var to = context.GetMessagePp() ?? context.GetMessageFrom();
            if (to == null)  throw new ArgumentException("Could not determine the message sender", nameof(context));
                        
            var content = output as Document;
            if (content != null)
            {
                return _envelopeSenderFactory().SendMessageAsync(content, to);
            }
            return _envelopeSenderFactory().SendMessageAsync(output.ToString(), to);
        }
    }
}

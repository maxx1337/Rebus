﻿using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline.Send
{
    /// <summary>
    /// Outgoing step that sets the <see cref="Headers.CorrelationId"/> header of the outgoing message if it has not already been set.
    /// The value used is one of the following (in prioritized order):
    /// 1) The correlation ID of the message currently being handled,
    /// 2) The message ID of the message currently being handled,
    /// 3) The message's own message ID
    /// </summary>
    public class FlowCorrelationIdStep : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var outgoingMessage = context.Load<Message>();

            var transactionContext = context.Load<ITransactionContext>();
            var incomingStepContext = transactionContext.Items.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);

            if (!outgoingMessage.Headers.ContainsKey(Headers.CorrelationId))
            {
                var correlationId = GetCorrelationIdToAssign(incomingStepContext, outgoingMessage);

                outgoingMessage.Headers[Headers.CorrelationId] = correlationId;
            }

            await next();
        }

        string GetCorrelationIdToAssign(IncomingStepContext incomingStepContext, Message outgoingMessage)
        {
            // if we're handling an incoming message right now, let either current correlation ID or the message ID flow
            if (incomingStepContext != null)
            {
                var incomingMessage = incomingStepContext.Load<Message>();

                var correlationId = incomingMessage.Headers.GetValueOrNull(Headers.CorrelationId)
                                    ?? incomingMessage.Headers.GetValue(Headers.MessageId);

                return correlationId;
            }
            
            // otherwise, use the current message ID as the correlation ID
            return outgoingMessage.Headers.GetValue(Headers.MessageId);
        }
    }
}
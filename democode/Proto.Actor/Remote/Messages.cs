﻿// -----------------------------------------------------------------------
//   <copyright file="Messages.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;

namespace Proto.Remote
{
    public sealed class EndpointTerminatedEvent
    {
        public string Address { get; set; }
    }

    public class RemoteTerminate
    {
        public RemoteTerminate(PID watcher, PID watchee)
        {
            Watcher = watcher;
            Watchee = watchee;
        }

        public PID Watcher { get; }
        public PID Watchee { get; }
    }

    public class RemoteWatch
    {
        public RemoteWatch(PID watcher, PID watchee)
        {
            Watcher = watcher;
            Watchee = watchee;
        }

        public PID Watcher { get; }
        public PID Watchee { get; }
    }

    public class RemoteUnwatch
    {
        public RemoteUnwatch(PID watcher, PID watchee)
        {
            Watcher = watcher;
            Watchee = watchee;
        }

        public PID Watcher { get; }
        public PID Watchee { get; }
    }

    public class RemoteDeliver
    {
        public RemoteDeliver(object message, PID target, PID sender, int serializerId)
        {
            Message = message;
            Target = target;
            Sender = sender;
            SerializerId = serializerId;
        }

        public object Message { get; }
        public PID Target { get; }
        public PID Sender { get; }

        public int SerializerId { get; }
    }

    public class JsonMessage
    {
        //NOTE: typename should not be checked against available typenames on send
        //as the message might only exist on the receiveing side
        public JsonMessage(string typeName, string json)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            Json = json ?? throw new ArgumentNullException(nameof(json));
        }

        public string Json { get; set; }
        public string TypeName { get; set; }
    }
}
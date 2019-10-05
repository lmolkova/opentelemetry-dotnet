// <copyright file="Tracing.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;

namespace OpenTelemetry.Trace
{
    using OpenTelemetry.Trace.Configuration;

    /// <summary>
    /// Class that manages a global instance of the <see cref="Tracer"/>.
    /// </summary>
    public static class Tracing
    {
        public static event EventHandler<GlobalInitEventArgs> GlobalInit;

        public static TracerFactory TracerFactory { get; private set; }

        public static void Init(TracerBuilder builder)
        {
            // if already init - throw
            globalTracerBuilder = builder;

            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is rais
            var eventHandler = GlobalInit;

            // Event will be null if there are no subscribers
            if (eventHandler != null)
            {
                var eventArgs = new GlobalInitEventArgs(builder);
                eventHandler(null, eventArgs);
            }
        }

        private static TracerBuilder globalTracerBuilder;

        public class GlobalInitEventArgs : EventArgs
        {
            public GlobalInitEventArgs(TracerBuilder globalTracerBuilder)
            {
                this.GlobalTracerBuilder = globalTracerBuilder;
            }

            public TracerBuilder GlobalTracerBuilder { get; private set; }
        }
    }
}

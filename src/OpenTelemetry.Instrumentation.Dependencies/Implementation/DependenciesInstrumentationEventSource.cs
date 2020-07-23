﻿// <copyright file="DependenciesInstrumentationEventSource.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;

namespace OpenTelemetry.Instrumentation.Dependencies.Implementation
{
    /// <summary>
    /// EventSource events emitted from the project.
    /// </summary>
    [EventSource(Name = "OpenTelemetry-Instrumentation-Dependencies")]
    internal class DependenciesInstrumentationEventSource : EventSource
    {
        public static DependenciesInstrumentationEventSource Log = new DependenciesInstrumentationEventSource();

        [NonEvent]
        public void UnknownErrorProcessingEvent(string handlerName, string eventName, Exception ex)
        {
            if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
            {
                this.UnknownErrorProcessingEvent(handlerName, eventName, ToInvariantString(ex));
            }
        }

        [NonEvent]
        public void FailedProcessResult(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
            {
                this.FailedProcessResult(ToInvariantString(ex));
            }
        }

        [NonEvent]
        public void ExceptionInitializingInstrumentation(string instrumentationType, Exception ex)
        {
            if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
            {
                this.ExceptionInitializingInstrumentation(instrumentationType, ToInvariantString(ex));
            }
        }

        [Event(4, Message = "Current Activity is NULL the '{0}' callback. Span will not be recorded.", Level = EventLevel.Warning)]
        public void NullActivity(string eventName)
        {
            this.WriteEvent(4, eventName);
        }

        [Event(5, Message = "Payload is NULL in event '{1}' from handler '{0}', span will not be recorded.", Level = EventLevel.Warning)]
        public void NullPayload(string handlerName, string eventName)
        {
            this.WriteEvent(5, handlerName, eventName);
        }

        [Event(6, Message = "Payload is invalid in event '{1}' from handler '{0}', span will not be recorded.", Level = EventLevel.Warning)]
        public void InvalidPayload(string handlerName, string eventName)
        {
            this.WriteEvent(6, handlerName, eventName);
        }

        /// <summary>
        /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
        /// appropriate for diagnostics tracing.
        /// </summary>
        private static string ToInvariantString(Exception exception)
        {
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                return exception.ToString();
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }

        [Event(1, Message = "Unknown error processing event '{1}' from handler '{0}', Exception: {2}", Level = EventLevel.Error)]
        private void UnknownErrorProcessingEvent(string handlerName, string eventName, string ex)
        {
            this.WriteEvent(1, handlerName, eventName, ex);
        }

        [Event(2, Message = "Failed to process result: '{0}'", Level = EventLevel.Error)]
        private void FailedProcessResult(string ex)
        {
            this.WriteEvent(2, ex);
        }

        [Event(3, Message = "Error initializing instrumentation type {0}. Exception : {1}", Level = EventLevel.Error)]
        private void ExceptionInitializingInstrumentation(string instrumentationType, string ex)
        {
            this.WriteEvent(3, instrumentationType, ex);
        }
    }
}
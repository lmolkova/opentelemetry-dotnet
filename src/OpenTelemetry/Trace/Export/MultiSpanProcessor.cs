// <copyright file="MultiSpanProcessor.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace.Export
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Proxies calls to a multiple SpanProcessor.
    /// <remarks>It does not implement SpanProcessor intentionally to avoid unnecessary virtual calls.</remarks>
    /// </summary>
    internal class MultiSpanProcessor
    {
        private readonly IEnumerable<SpanProcessor> processors;

        public MultiSpanProcessor(IEnumerable<SpanProcessor> processors)
        {
            this.processors = processors;
        }

        public void OnStart(Span span)
        {
            foreach (var proc in this.processors)
            {
                proc.OnStart(span);
            }
        }

        public void OnEnd(Span span)
        {
            foreach (var proc in this.processors)
            {
                proc.OnEnd(span);
            }
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(this.processors.Select(p => p.ShutdownAsync(cancellationToken)));
        }
    }
}

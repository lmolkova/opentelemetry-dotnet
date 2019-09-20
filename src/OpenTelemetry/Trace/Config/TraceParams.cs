// <copyright file="TraceParams.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace.Config
{
    using System;
    using OpenTelemetry.Trace.Sampler;

    /// <inheritdoc/>
    public sealed class TraceParams : ITraceParams
    {
        /// <summary>
        /// Default trace parameters.
        /// </summary>
        public static readonly ITraceParams Default = new TraceParams(Samplers.AlwaysSample);

        internal TraceParams(ISampler sampler)
        {
            this.Sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }

        /// <inheritdoc/>
        public ISampler Sampler { get; }

        /// <inheritdoc/>
        public TraceParamsBuilder ToBuilder()
        {
            return new TraceParamsBuilder(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(TraceParams)
                + "{"
                + nameof(this.Sampler) + "=" + this.Sampler
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TraceParams that)
            {
                return this.Sampler.Equals(that.Sampler);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= this.Sampler.GetHashCode();
            return h;
        }
    }
}

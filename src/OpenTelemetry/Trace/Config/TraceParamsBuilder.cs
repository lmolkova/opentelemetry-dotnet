// <copyright file="TraceParamsBuilder.cs" company="OpenTelemetry Authors">
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

    /// <summary>
    /// Trace parameters builder.
    /// </summary>
    public sealed class TraceParamsBuilder
    {
        private ISampler sampler;

        internal TraceParamsBuilder(TraceParams source)
        {
            this.sampler = source.Sampler;
        }

        /// <summary>
        /// Sets sempler to use.
        /// </summary>
        /// <param name="sampler">Sampler to use.</param>
        /// <returns>Builder to chain operations.</returns>
        public TraceParamsBuilder SetSampler(ISampler sampler)
        {
            this.sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            return this;
        }

        /// <summary>
        /// Builds trace parameters from provided arguments.
        /// </summary>
        /// <returns>Builder to chain operations.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If maximum values are not set or set to less than one.</exception>
        public TraceParams Build()
        {
            if (this.sampler == null)
            {
                throw new ArgumentNullException(nameof(sampler));
            }

            return new TraceParams(this.sampler);
        }
    }
}

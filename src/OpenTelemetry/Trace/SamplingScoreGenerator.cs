// <copyright file="SamplingScoreGenerator.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace
{
    public abstract class SamplingScoreGenerator
    {
        public abstract double GenerateScore(in SamplingParameters parameters);
    }

    public class TraceIdRatioGenerator : SamplingScoreGenerator
    {
        public override double GenerateScore(in SamplingParameters samplingParameters)
        {
            Span<byte> traceIdBytes = stackalloc byte[16];
            samplingParameters.TraceId.CopyTo(traceIdBytes);

            return Math.Abs(this.GetLowerLong(traceIdBytes)) / (float)long.MaxValue;
        }

        private long GetLowerLong(ReadOnlySpan<byte> bytes)
        {
            long result = 0;
            for (var i = 0; i < 8; i++)
            {
                result <<= 8;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                result |= bytes[i] & 0xff;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
            }

            return result;
        }
    }

    public class RandomGenerator : SamplingScoreGenerator
    {
        public override double GenerateScore(in SamplingParameters parameters)
        {
            return new Random().NextDouble();
        }
    }
}

// <copyright file="ExternalScoreSampler.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.Trace
{

    public class ExternalScoreSampler : Sampler
    {
        private const string ScoreFlagName = "sampling.score";
        private static readonly int ScoreFlagLength = "sampling.score".Length;
        private static readonly IEnumerable<KeyValuePair<string, object>> EmptyAttributes = Enumerable.Empty<KeyValuePair<string, object>>();
        private readonly double probability;
        private readonly SamplingScoreGenerator scoreGenerator;

        public ExternalScoreSampler(double probability)
        {
            this.probability = probability;
            this.scoreGenerator = new RandomGenerator();
        }

        public ExternalScoreSampler(double probability, SamplingScoreGenerator scoreGenerator)
        {
            this.scoreGenerator = scoreGenerator;
            this.probability = probability;
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            // todo: optimize for in-proc parent

            string tracestate = samplingParameters.ParentContext.TraceState;
            if (!this.TryGetScore(tracestate, out var score))
            {
                // if sampling.score is NOT in the tracestate, calculate a score
                score = (float)this.scoreGenerator.GenerateScore(in samplingParameters);

                // prepend it to the tracestate
                tracestate = tracestate != null ?
                    string.Concat(ScoreFlagName, '=', score, ',', tracestate) :
                    string.Concat(ScoreFlagName, '=', score);
            }

            var result = score <= this.probability;
            return new SamplingResult(
                decision: result ? SamplingDecision.RecordAndSampled : SamplingDecision.NotRecord,
                attributes: result ? new[] { new KeyValuePair<string, object>(ScoreFlagName, score) } : EmptyAttributes, // TODO merge with attributes in samplingParameters
                tracestate: tracestate);
        }

        private bool TryGetScore(string tracestate, out float upstreamScore)
        {
            if (!string.IsNullOrEmpty(tracestate))
            {
                var start = tracestate.IndexOf(ScoreFlagName);
                if (start >= 0)
                {
                    var end = tracestate.IndexOf(',', start);
                    if (end < 0)
                    {
                        end = tracestate.Length;
                    }

                    start += ScoreFlagLength + 1; // sampling.score=
                    if (float.TryParse(tracestate.Substring(start, end - start), out upstreamScore))
                    {
                        return true;
                    }
                }
            }

            upstreamScore = 0;
            return false;
        }
    }
}

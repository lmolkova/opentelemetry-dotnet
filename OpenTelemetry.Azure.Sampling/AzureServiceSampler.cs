using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Azure.Sampling
{
    public class AzureServiceSampler : Sampler
    {
        private readonly Sampler sampler1P;
        private readonly Sampler sampler3P;
        private const string Only3PAttribute = "Only3P"; // this attribute will be populated on 3P spans, but never sent anywhere (since derived event will not populate it on the forked event)
        public AzureServiceSampler(Sampler sampler1P, Sampler sampler3P)
        {
            this.sampler1P = sampler1P;
            this.sampler3P = sampler3P;
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            var result1P = this.sampler1P.ShouldSample(samplingParameters);
            var result3P = this.sampler3P.ShouldSample(samplingParameters);

            // if 1P OR 3P  is in, sample it in (and merge attributes)
            if (this.Recorded(result1P.Decision) || this.Recorded(result3P.Decision))
            {
                return new SamplingResult(SamplingDecision.RecordAndSampled, this.MergeAttributes(result1P, result3P));
            }

            // there could be an edge cases when you'd need attributes merged on sampled-out spans,
            // but it's unconventional and it could be skipped for now for perf reasons
            return new SamplingResult(SamplingDecision.NotRecord);
        }

        private bool Recorded(SamplingDecision decision)
        {
            return decision == SamplingDecision.Record || decision == SamplingDecision.RecordAndSampled;
        }

        private IEnumerable<KeyValuePair<string, object>> MergeAttributes(SamplingResult result1P, SamplingResult result3P)
        {
            // if 1P is not sampled in, add Only3P attribute to span, we'll use it on agent to
            // forward this span to 3P consumption only
            if (!this.Recorded(result1P.Decision))
            {
                var attr = new List<KeyValuePair<string, object>>(result3P.Attributes)
                {
                    new KeyValuePair<string, object>(Only3PAttribute, true)
                };
                return attr;
            }

            if (result1P.Attributes == null || !result1P.Attributes.Any())
            {
                return result3P.Attributes;
            }

            if (result3P.Attributes == null || !result3P.Attributes.Any())
            {
                return result1P.Attributes;
            }

            var merge = new List<KeyValuePair<string, object>>();
            merge.AddRange(result1P.Attributes);
            merge.AddRange(result3P.Attributes);
            return merge;
        }
    }
}

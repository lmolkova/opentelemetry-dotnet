using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Azure.Sampling
{
    [Flags]
    public enum PublicBoundary
    {
        Incoming = 1,
        Outgoing = 2,
    }

    /// <summary>
    /// Sampler implements parent-only strategy: samples in spans following client decision
    /// </summary>
    public class ThirdPartyParentOnlySampler : Sampler
    {
        private const string UserFlagName = "tf@az";
        private static readonly int UserFlagLength = "tf@az".Length;

        private readonly Func<string> getResourceIdCallback;
        private readonly Func<string, bool> samplingCallback;
        private readonly PublicBoundary boundaries;
        private readonly bool propagateContext;

        public ThirdPartyParentOnlySampler(Func<string, bool> samplingCallback, Func<string> getResourceIdCallback, PublicBoundary boundaries, bool propagateContext = true)
        {
            this.getResourceIdCallback = getResourceIdCallback;

            // sampling callback will be different for rate-based sampling: Func<string, double, bool> samplingCallback

            this.samplingCallback = samplingCallback;
            this.boundaries = boundaries;
            this.propagateContext = propagateContext;
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            // if it's server span, and it's on the public endpoint
            if ((this.boundaries & PublicBoundary.Incoming) == PublicBoundary.Incoming &&
                this.IsServerSpan(in samplingParameters))
            {
                string resourceId = this.getResourceIdCallback?.Invoke();

                // follow parent decision + respect sampling configuration settings
                if (!string.IsNullOrEmpty(resourceId) && 
                    samplingParameters.ParentContext.TraceFlags == ActivityTraceFlags.Recorded &&
                    this.samplingCallback(resourceId))             // samplingCallback(resoureId, GetScore(traceId)) <= this.rate for rate-based
                {
                    // if there are outgoing requests, we need to follow client sampling decision on outgoing boundary
                    // internal sampling decision may be different, but we need to propagate client decision all the way to outgoing boundary
                    if (this.propagateContext)
                    {
                        var childTracestate = string.Concat(UserFlagName, '=', "01", ',',
                            samplingParameters.ParentContext.TraceState);

                        // todo: childTracestate set on result
                        // hacky workaround for https://github.com/open-telemetry/opentelemetry-specification/issues/856
                        if (Activity.Current != null)
                        {
                            Activity.Current.TraceStateString = childTracestate;
                        }
                    }

                    // resourceId attributed will tell agent that this event needs to be exported to 3P
                    return new SamplingResult(SamplingDecision.RecordAndSampled, new []{new KeyValuePair<string, object>("resourceId", resourceId)});
                }
            }

            // if this is a outgoing call and to external endpoint
            if ((this.boundaries & PublicBoundary.Outgoing) == PublicBoundary.Outgoing && // if outgoing boundary is making public calls
                this.IsClientOrInternalSpan(in samplingParameters) && // and it's a client or internal span
                PublicCall.IsPublicCall()) // and it's a call to external endpoint on behalf of user
            {
                string resourceId = this.getResourceIdCallback?.Invoke();

                // and if it's sampled in (context is propagated in the tracestate)
                if (this.TryGetUserFlag(samplingParameters.ParentContext.TraceState, out var userFlag) &&
                    userFlag.Length == 2 &&
                    userFlag[1] == '1')
                {
                    if (samplingParameters.Kind != ActivityKind.Internal)
                    {
                        // remove tf@az from tracestate if it's a client span -
                        // don't delete it on internal spans -  this will break sampling for nested client spans
                        var childTracestate = this.SanitizeTraceState(samplingParameters.ParentContext.TraceState);

                        // todo: childTracestate set on result
                        // hacky workaround for https://github.com/open-telemetry/opentelemetry-specification/issues/856
                        if (Activity.Current != null)
                        {
                            Activity.Current.TraceStateString = childTracestate;
                        }
                    }

                    return new SamplingResult(SamplingDecision.RecordAndSampled, new[] { new KeyValuePair<string, object>("resourceId", resourceId) });
                }
            }

            return new SamplingResult(SamplingDecision.NotRecord);
        }

        private bool IsServerSpan(in SamplingParameters samplingParameters)
        {
            return samplingParameters.Kind == ActivityKind.Server || samplingParameters.Kind == ActivityKind.Consumer;
        }

        private bool IsClientOrInternalSpan(in SamplingParameters samplingParameters)
        {
            return samplingParameters.Kind == ActivityKind.Client || samplingParameters.Kind == ActivityKind.Producer || samplingParameters.Kind == ActivityKind.Internal;
        }

        private string SanitizeTraceState(string tracestate)
        {
            // todo: cleanup all *@az keys

            return string.Empty;
        }

        private bool TryGetUserFlag(string tracestate, out ReadOnlySpan<char> userFlag)
        {
            userFlag = default;
            if (!string.IsNullOrEmpty(tracestate))
            {
                var start = tracestate.IndexOf(UserFlagName);
                if (start >= 0)
                {
                    var end = tracestate.IndexOf(',', start);
                    if (end < 0)
                    {
                        end = tracestate.Length;
                    }

                    start += UserFlagLength + 1; // tf@az=
                    userFlag = tracestate.AsSpan(start, end - start);
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

// <copyright file="NoopTracerTests.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Resources;

namespace OpenTelemetry.Tests.Impl.Trace
{
    using System;
    using OpenTelemetry.Context.Propagation;
    using OpenTelemetry.Trace;
    using Xunit;

    public class NoopTracerTests
    {
        [Fact]
        public void NoopTracer_CurrentSpan()
        {
            Assert.Same(BlankSpan.Instance, ProxyTracer.Instance.CurrentSpan);
        }

        [Fact]
        public void NoopTracer_WithSpan()
        {
            var noopScope = ProxyTracer.Instance.WithSpan(BlankSpan.Instance);
            Assert.NotNull(noopScope);
            // does not throw
            noopScope.Dispose();
        }

        [Fact]
        public void NoopTracer_SpanBuilder()
        {
            Assert.IsType<NoopSpanBuilder>(ProxyTracer.Instance.SpanBuilder("foo"));
        }

        [Fact]
        public void NoopTracer_Formats()
        {
            Assert.NotNull(ProxyTracer.Instance.TextFormat);
            Assert.NotNull(ProxyTracer.Instance.BinaryFormat);
            Assert.IsAssignableFrom<ITextFormat>(ProxyTracer.Instance.TextFormat);
            Assert.IsAssignableFrom<IBinaryFormat>(ProxyTracer.Instance.BinaryFormat);
        }
    }
}


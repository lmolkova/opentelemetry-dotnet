﻿// <copyright file="TracerFactorySdkTest.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Trace.Test
{
    using OpenTelemetry.Trace.Configuration;
    using System.Linq;
    using Xunit;

    public class TracerFactorySdkTest
    {
        private readonly TracerFactory tracerFactory = new TracerBuilder();
        
        [Fact]
        public void GetTracer_NoName_NoVersion()
        {
            var tracer = (Tracer)tracerFactory.GetTracer("");
            Assert.DoesNotContain(tracer.LibraryResource.Labels, kvp => kvp.Key == "name");
            Assert.DoesNotContain(tracer.LibraryResource.Labels, kvp => kvp.Key == "version");
        }

        [Fact]
        public void GetTracer_NoName_Version()
        {
            var tracer = (Tracer)tracerFactory.GetTracer(null, "semver:1.0.0");
            Assert.DoesNotContain(tracer.LibraryResource.Labels, kvp => kvp.Key == "name");
            Assert.DoesNotContain(tracer.LibraryResource.Labels, kvp => kvp.Key == "version");
        }
        
        [Fact]
        public void GetTracer_Name_NoVersion()
        {
            var tracer = (Tracer)tracerFactory.GetTracer("foo");
            Assert.Equal("foo", tracer.LibraryResource.Labels.Single(kvp => kvp.Key == "name").Value);
            Assert.DoesNotContain(tracer.LibraryResource.Labels, kvp => kvp.Key == "version");
        }
        
        [Fact]
        public void GetTracer_Name_Version()
        {
            var tracer = (Tracer)tracerFactory.GetTracer("foo", "semver:1.2.3");
            Assert.Equal("foo", tracer.LibraryResource.Labels.Single(kvp => kvp.Key == "name").Value);
            Assert.Equal("semver:1.2.3", tracer.LibraryResource.Labels.Single(kvp => kvp.Key == "version").Value);
        }
        
        [Fact]
        public void FactoryReturnsSameTracerForGivenNameAndVersion()
        {
            var tracer1 = tracerFactory.GetTracer("foo", "semver:1.2.3");
            var tracer2 = tracerFactory.GetTracer("foo");
            var tracer3 = tracerFactory.GetTracer("foo", "semver:2.3.4");
            var tracer4 = tracerFactory.GetTracer("bar", "semver:1.2.3");
            var tracer5 = tracerFactory.GetTracer("foo", "semver:1.2.3");
            var tracer6 = tracerFactory.GetTracer("");
            var tracer7 = tracerFactory.GetTracer(null);
            var tracer8 = tracerFactory.GetTracer(null, "semver:1.2.3");
            
            Assert.NotEqual(tracer1, tracer2);
            Assert.NotEqual(tracer1, tracer3);
            Assert.NotEqual(tracer1, tracer4);
            Assert.Equal(tracer1, tracer5);
            Assert.NotEqual(tracer5, tracer6);
            Assert.Equal(tracer6, tracer7);
            Assert.Equal(tracer7, tracer8);
        }
    }
}

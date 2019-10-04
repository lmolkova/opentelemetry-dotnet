﻿// <copyright file="TracerBuilderExtensions.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Exporter.Zipkin
{
    using System;
    using OpenTelemetry.Trace.Configuration;

    public static class TracerBuilderExtensions
    {
        public static TracerBuilder AddZipkin(this TracerBuilder builder, ZipkinTraceExporterOptions options)
        {
            return builder.AddExporter(new ZipkinTraceExporter(options));
        }

        public static TracerBuilder AddZipkin(this TracerBuilder builder, Action<ZipkinTraceExporterOptions> configureOptions)
        {
            var options = new ZipkinTraceExporterOptions();
            configureOptions(options);
            return builder.AddExporter(new ZipkinTraceExporter(options));
        }
    }
}

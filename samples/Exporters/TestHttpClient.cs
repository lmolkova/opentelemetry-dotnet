// <copyright file="TestHttpClient.cs" company="OpenTelemetry Authors">
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

namespace Samples
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using OpenTelemetry.Collector.Dependencies;
    using OpenTelemetry.Exporter.Zipkin;
    using OpenTelemetry.Trace.Configuration;
    using OpenTelemetry.Trace.Export;
    using OpenTelemetry.Trace.Sampler;

    internal class TestHttpClient
    {
        internal static object Run()
        {
            Console.WriteLine("Hello World!");

            var exporter = new ZipkinTraceExporter(
                new ZipkinTraceExporterOptions()
                {
                    Endpoint = new Uri("https://zipkin.azurewebsites.net/api/v2/spans"),
                    ServiceName = typeof(Program).Assembly.GetName().Name,
                });

<<<<<<< HEAD
<<<<<<< HEAD
            var tracerFactory = new TracerFactorySdk(new BatchingSpanProcessor(exporter));
            var tracer = tracerFactory.GetTracer(nameof(HttpClientCollector));
            using (new HttpClientCollector(new HttpClientCollectorOptions(), tracer))
=======
            var tracerBuilder = new TracerBuilder()
                .AddSpanExporter(exporter)
                .AddSpanProcessor(e => new BatchingSpanProcessor(e))
                .AddCollector(t => new HttpClientCollector(new DependenciesCollectorOptions(), t));

            var tracer = tracerBuilder.GetTracer(string.Empty);
<<<<<<< HEAD
            using (new DependenciesCollector(new DependenciesCollectorOptions(), tracerBuilder))
>>>>>>> b8e378d... trash
=======
>>>>>>> 6a8c8a0... better
=======
            using (var tracerBuilder = new TracerBuilder())
>>>>>>> 6864611... closer
            {
                var tracer = tracerBuilder.AddExporter(exporter)
                    .AddProcessor(e => new BatchingSpanProcessor(e))
                    .AddCollector(t => new HttpClientCollector(new HttpClientCollectionOptions(), t))
                    .Build();

                using (tracer.WithSpan(tracer.SpanBuilder("incoming request").SetSampler(Samplers.AlwaysSample)
                    .StartSpan()))
                {
                    using (var client = new HttpClient())
                    {
                        client.GetStringAsync("http://bing.com").GetAwaiter().GetResult();
                    }
                }

                Console.ReadLine();

                exporter.ShutdownAsync(CancellationToken.None).GetAwaiter().GetResult();
                return null;
            }
        }
    }
}

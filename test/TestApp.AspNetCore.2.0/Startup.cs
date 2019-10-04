﻿// <copyright file="Startup.cs" company="OpenTelemetry Authors">
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Collector.AspNetCore;
using OpenTelemetry.Collector.Dependencies;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;
using OpenTelemetry.Trace.Sampler;
using System.Net.Http;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace.Configuration;

namespace TestApp.AspNetCore._2._0
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<HttpClient>();

            services.AddSingleton<ISampler>(Samplers.AlwaysSample);
            services.TryAddSingleton<RequestsCollectorOptions>(new RequestsCollectorOptions());
            services.AddSingleton<RequestsCollector>();
            services.TryAddSingleton<DependenciesCollectorOptions>(new DependenciesCollectorOptions());
            services.AddSingleton<DependenciesCollector>();
            services.AddSingleton<CallbackMiddleware.CallbackMiddlewareImpl>(new CallbackMiddleware.CallbackMiddlewareImpl());
            services.AddSingleton<ZipkinTraceExporterOptions>(new ZipkinTraceExporterOptions { ServiceName = "tracing-to-zipkin-service" });
            services.AddSingleton<SpanExporter, ZipkinTraceExporter>();
            services.AddSingleton<SpanProcessor, BatchingSpanProcessor>();
            services.AddSingleton<TracerConfiguration>();
            services.AddSingleton<ITracer, Tracer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<CallbackMiddleware>();
            app.UseMvc();
            var collector = app.ApplicationServices.GetService<RequestsCollector>();
            var depCollector = app.ApplicationServices.GetService<DependenciesCollector>();
        }
    }
}

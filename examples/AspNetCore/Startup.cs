// <copyright file="Startup.cs" company="OpenTelemetry Authors">
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Examples.AspNetCore.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Azure.Sampling;
using OpenTelemetry.Trace;

namespace Examples.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Switch between Zipkin/Jaeger by setting UseExporter in appsettings.json.
            var exporter = this.Configuration.GetValue<string>("UseExporter").ToLowerInvariant();
            switch (exporter)
            {
                case "jaeger":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddJaegerExporter(jaegerOptions =>
                        {
                            jaegerOptions.ServiceName = this.Configuration.GetValue<string>("Jaeger:ServiceName");
                            jaegerOptions.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
                            jaegerOptions.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
                        }));
                    break;
                case "zipkin":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddZipkinExporter(zipkinOptions =>
                        {
                            zipkinOptions.ServiceName = this.Configuration.GetValue<string>("Zipkin:ServiceName");
                            zipkinOptions.Endpoint = new Uri(this.Configuration.GetValue<string>("Zipkin:Endpoint"));
                        }));
                    break;
                default:
                    services.AddHttpContextAccessor();
                    services.AddSingleton<ResourceIdGetter>();
                    services.AddSingleton<ParentOnlySamplingSettingsProvider>();

                    services.AddOpenTelemetryTracing((provider, builder) =>
                    {
                        var resourceIdGetter = provider.GetService<ResourceIdGetter>();
                        var samplingSettingsProvider = provider.GetService<ParentOnlySamplingSettingsProvider>();
                        builder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddSource("test")
                            .SetSampler(new AzureServiceSampler(
                                new AlwaysOnSampler(),
                                new ThirdPartyParentOnlySampler(samplingSettingsProvider.IsSampled,
                                    resourceIdGetter.GetResourceId, PublicBoundary.Outgoing | PublicBoundary.Incoming)))
                            .AddZipkinExporter(zipkinOptions =>
                            {
                                zipkinOptions.ServiceName = this.Configuration.GetValue<string>("Zipkin:ServiceName");
                                zipkinOptions.Endpoint =
                                    new Uri(this.Configuration.GetValue<string>("Zipkin:Endpoint"));
                            });
                    });
                    break;
            }
        }

        private class ResourceIdGetter
        {
            private readonly IHttpContextAccessor httpContextAccessor;

            public ResourceIdGetter(IHttpContextAccessor contextAccessor)
            {
                this.httpContextAccessor = contextAccessor;
            }

            public string GetResourceId()
            {
                var ctx = this.httpContextAccessor.HttpContext;
                if (ctx == null)
                {
                    return string.Empty;
                }

                return this.GetResourceId(ctx);
            }

            private string GetResourceId(HttpContext context)
            {
                // use HttpContext to calculate resource id
                // this code runs before any middleware, no way to get any callback before that
                return "/SUBSCRIPTIONS/123/RESOURCEGROUPS/MY-RG/PROVIDERS/MICROSOFT.NAMESPACE/TYPE/MY-RESOURCE";
            }
        }

        public class ParentOnlySamplingSettingsProvider
        {
            private readonly ConcurrentDictionary<string, bool> resourcesWithTracingEnabled = new ConcurrentDictionary<string, bool>();

            public bool IsSampled(string resourceId)
            {

                if (this.resourcesWithTracingEnabled.TryGetValue(resourceId, out var sampleIn))
                {
                    return sampleIn;
                }

                return this.resourcesWithTracingEnabled.GetOrAdd(resourceId, this.IsTracingEnabledForResource);
            }

            private bool IsTracingEnabledForResource(string resourceId)
            {
                return true; // do lazy read of resourceId settings from central/local storage here
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

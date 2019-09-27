using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Config;
using OpenTelemetry.Trace.Export;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // samples
            // Basic configuration

            // create and configure tracer
            var zipkinExporter = new ZipkinTraceExporter(new ZipkinTraceExporterOptions());
            var tracer = new Tracer(new SimpleSpanProcessor(zipkinExporter), TraceConfig.Default);

            // there will be method that makes it default tracer for auto-collectors
            // DefaultTracer.Init(tracer)

            {
                // create the most basic span
                var span = tracer
                    .SpanBuilder("basic span") // this will change
                    .StartSpan();

                span.End();
            }

            {
                // create span with children
                // explicitly assign parent
                var parentSpan = tracer
                    .SpanBuilder("parent") // this will change
                    .StartSpan();

                var childSpan = tracer
                    .SpanBuilder("child") // this will change
                    .SetParent(parentSpan)
                    .StartSpan();

                // end in any order
                parentSpan.End();
                childSpan.End();
            }

            {
                // create span with children, assign parent implicitly
                var parentSpan = tracer
                    .SpanBuilder("parent") // this will change
                    .StartSpan();
                using (tracer.WithSpan(parentSpan))
                {
                    var childSpan = tracer
                        .SpanBuilder("child") // this will change
                        .StartSpan();

                    childSpan.End();
                }
            }

            {
                // create span with attributes
                var span = tracer
                    .SpanBuilder("span with attributes ") // this will change
                    .SetSpanKind(SpanKind.Client)
                    .StartSpan();

                span.SetAttribute("db.type", "redis");
                span.SetAttribute("db.instance", "localhost:6379[0]");
                span.SetAttribute("db.statement", "SET");
                span.End();
            }

            {
                // create span with links
                SpanContext link1 = SpanContext.Blank;//ExtractContext(eventHubMessage1);
                SpanContext link2 = SpanContext.Blank;//ExtractContext(eventHubMessage2);

                var span = tracer
                    .SpanBuilder("span with links") // this will change
                    .SetSpanKind(SpanKind.Server)
                    .AddLink(link1)
                    .AddLink(link2)
                    .StartSpan();

                span.End();
            }

            {
                // add events to running span
                var span = tracer
                    .SpanBuilder("incoming HTTP request") // this will change
                    .SetSpanKind(SpanKind.Server)
                    .StartSpan();

                using (tracer.WithSpan(span))
                {
                    tracer.CurrentSpan.AddEvent("something important happened");
                }

                span.End();
            }

            {
                // in auto-collector
                void StartActivity()
                {
                    var span = tracer
                        .SpanBuilder("GET api/values") // get name from Activity props/tags, incoming request
                        .SetCreateChild(false) // needs change: tells to use current activity without creating a child one
                        .StartSpan();

                    // extract other things from Activity and set them on span (tags to attributes)
                    // ...

                    tracer.WithSpan(span); // needs change: we drop scope here as we cannot propagate it
                }

                void StopActivity()
                {
                    var span = tracer.CurrentSpan;
                    span.End();
                }

                // auto-collector code
                var httpInActivity = new Activity("Microsoft.AspNetCore.HttpIn").Start();
                // StartActivity();
                // StopActivity();
                // httpInActivity.Stop();
            }

            {
                // extract context from incoming request
                HttpRequest incomingRequest;
                var context = tracer.TextFormat.Extract(incomingRequest.Headers, (headers, name) => headers[name]);

                var incomingSpan = tracer
                    .SpanBuilder("incoming http request") // this will change
                    .SetSpanKind(SpanKind.Server)
                    .SetParent(context)
                    .StartSpan();

                var outgoingSpan = tracer
                    .SpanBuilder("outgoing http request") // this will change
                    .SetSpanKind(SpanKind.Client)
                    .StartSpan();

                var outgoingRequest = new HttpRequestMessage(HttpMethod.Get, "http://microsoft.com");
                tracer.TextFormat.Inject(
                    outgoingSpan.Context,
                    outgoingRequest.Headers,
                    (headers, name, value) => headers.Add(name, value));

                outgoingSpan.End();
                incomingSpan.End();
            }

            {
                // custom sampler and exporter
                var sampler = new MySampler();
                var exporter = new MyExporter();
                var tracer = new Tracer(new SimpleSpanProcessor(exporter), new TraceConfig(sampler));
            }
            // TODO
            // create spans
            //  - +from user
            //  - +from auto-collector
            //  - +with links
            //  - +with events
            //  - +with attributes
            //  - +with scope
            //  - +get current span

            // propagation APIs
            //   - +inject
            //   - +extract
            //   - -configure propagator: advanced

            // sampling API
            //   - +how it's called internally, used in lib instrumentation
            //   - +custom sampler

            // configuration
            //  - +create tracer
            //  - +configure exporter
            //  - +set sampler
            //  - DI
            //  - + set default tracer: TBD

            // exporting
            //  - custom exporter

            // user app with auto-collectors
            // 
        }
    }

    class MySampler : ISampler
    {
        public string Description { get; } = "my custom sampler";

        public Decision ShouldSample(SpanContext parentContext, ActivityTraceId traceId, ActivitySpanId spanId, string name,
            IEnumerable<ILink> links)
        {
            bool sampledIn;
            if (parentContext != null && parentContext.IsValid)
            {
                sampledIn = (parentContext.TraceOptions & ActivityTraceFlags.Recorded) != 0;
            }
            else
            {
                sampledIn = Stopwatch.GetTimestamp() % 2 == 0;
            }

            return new Decision(sampledIn);
        }
    }

    class MyExporter : SpanExporter
    {
        public override Task<ExportResult> ExportAsync(IEnumerable<Span> batch, CancellationToken cancellationToken)
        {
            foreach (var span in batch)
            {
                Console.WriteLine($"[{span.StartTimestamp:o}] {span.Name} {span.Context.TraceId.ToHexString()} {span.Context.SpanId.ToHexString()}");
            }

            return Task.FromResult(ExportResult.Success);
        }

        public override Task ShutdownAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

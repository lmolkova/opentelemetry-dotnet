using System;
using System.Net.Http;
using System.Threading.Tasks;
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

            // TODO
            // create spans
            //  - +from user
            //  - from auto-collector
            //  - +with links
            //  - +with events
            //  - +with attributes
            //  - +with scope
            //  - +get current span

            // propagation APIs
            //   - inject
            //   - extract
            //   - configure propagator: advanced

            // sampling API
            //   - how it's called internally, used in lib instrumentation
            //   - custom sampler

            // configuration
            //  - +create tracer
            //  - +configure exporter
            //  - set sampler
            //  - DI
            //  - set default tracer: TBD

            // exporting
            //  - custom exporter

            // user app with auto-collectors
            // 
        }
    }
}

// <copyright file="WeatherForecastController.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Examples.AspNetCore.Models;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Azure.Sampling;

namespace Examples.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        };

        private static readonly ActivitySource source = new ActivitySource("test");
        private static HttpClient httpClient = new HttpClient();

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var res = httpClient.GetStringAsync("http://google.com").Result;

            using (PublicCall.BeginScope())
            {
                var res2 = httpClient.GetStringAsync("http://microsoft.com").Result;
            }

            using (PublicCall.BeginScope())
            {
                using (var internalOperation = source.StartActivity("internal", ActivityKind.Internal))
                {
                    var res3 = httpClient.GetStringAsync("https://www.bing.com/search?q=123").Result;
                }
            }

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
            })
            .ToArray();
        }
    }
}

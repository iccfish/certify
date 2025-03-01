﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Certify.Models.API;
using Certify.Server.API.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Certify.Service.Api.Tests
{
    [TestClass]
    public class APITestBase
    {
        internal static HttpClient _clientWithAnonymousAccess;
        internal static HttpClient _clientWithAuthorizedAccess;

        internal static TestServer _server;

        internal static System.Text.Json.JsonSerializerOptions _defaultJsonSerializerOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        internal static string _apiBaseUri = "/api/v1";

        internal string _refreshToken;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {

            // setup public API service and backend service worker API

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = services.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<SystemController>();

            _server = new TestServer(
                new WebHostBuilder()
                 .ConfigureAppConfiguration((context, builder) =>
                 {
                     builder
                     .AddJsonFile("appsettings.api.public.test.json");
                 })
                .UseStartup<Server.API.Startup>()
                );

            _clientWithAnonymousAccess = _server.CreateClient();
            _clientWithAuthorizedAccess = _server.CreateClient();

            Worker.Program.CreateHostBuilder(new string[] { }).Build().StartAsync();
        }

        public async Task PerformAuth()
        {
            if (!_clientWithAuthorizedAccess.DefaultRequestHeaders.Any(h => h.Key == "Authorization"))
            {
                var login = new { username = "test", password = "test" };

                var payload = new StringContent(System.Text.Json.JsonSerializer.Serialize(login), System.Text.UnicodeEncoding.UTF8, "application/json");

                var response = await _clientWithAuthorizedAccess.PostAsync(_apiBaseUri + "/auth/login", payload);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var authResponse = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(responseString, _defaultJsonSerializerOptions);

                    _refreshToken = authResponse.RefreshToken;

                    _clientWithAuthorizedAccess.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
                }
            }
        }
    }
}

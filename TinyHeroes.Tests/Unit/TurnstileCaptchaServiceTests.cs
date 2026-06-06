using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TinyHeroes.Api.Services;

namespace TinyHeroes.Tests.Unit;

public class TurnstileCaptchaServiceTests
{
    private TurnstileCaptchaService BuildSut(HttpMessageHandler handler, string secretKey = "test-secret")
    {
        var http = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Turnstile:SecretKey"] = secretKey })
            .Build();
        return new TurnstileCaptchaService(http, config, NullLogger<TurnstileCaptchaService>.Instance);
    }

    [Fact]
    public async Task ValidateAsync_WhenSiteverifyReturnsSuccess_ReturnsTrue()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"success":true}""");
        var sut = BuildSut(handler);

        var result = await sut.ValidateAsync("valid-token");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenSiteverifyReturnsFailure_ReturnsFalse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"success":false}""");
        var sut = BuildSut(handler);

        var result = await sut.ValidateAsync("bad-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WhenSiteverifyReturns500_ReturnsFalse()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var sut = BuildSut(handler);

        var result = await sut.ValidateAsync("any-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WhenTokenIsEmpty_ReturnsFalseWithoutHttpCall()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"success":true}""");
        var sut = BuildSut(handler);

        var result = await sut.ValidateAsync("");

        result.Should().BeFalse();
        handler.CallCount.Should().Be(0);
    }

    private class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}

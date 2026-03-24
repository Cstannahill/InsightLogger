using FluentAssertions;
using InsightLogger.Application.Logging;

namespace InsightLogger.ApplicationTests.Logging;

public sealed class LogRedactorTests
{
    [Fact]
    public void Redact_ShouldMask_CommonSensitiveValues()
    {
        const string value = "Failure at C:\\repo\\app\\Program.cs while calling https://api.example.com/v1/chat?api_key=secret123 for admin@example.com with token=abc123";

        var redacted = LogRedactor.Redact(value);

        redacted.Should().NotContain("C:\\repo\\app\\Program.cs");
        redacted.Should().NotContain("https://api.example.com");
        redacted.Should().NotContain("admin@example.com");
        redacted.Should().NotContain("secret123");
        redacted.Should().Contain("<redacted:path>");
        redacted.Should().Contain("<redacted:url>");
        redacted.Should().Contain("<redacted:email>");
        redacted.Should().Contain("token=<redacted>");
    }

    [Fact]
    public void Redact_ShouldTruncate_LongValues()
    {
        var value = new string('a', 400);

        var redacted = LogRedactor.Redact(value, maxLength: 32);

        redacted.Should().HaveLength(33);
        redacted.Should().EndWith("…");
    }
}

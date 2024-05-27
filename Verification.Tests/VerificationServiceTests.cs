using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VerificationProvider.Services;
using VerificationProvider.Models;
using VerificationProvider.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Verification.Tests
{
    public class VerificationServiceTests
    {
        private readonly VerificationService _verificationService;
        private readonly ILogger<VerificationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public VerificationServiceTests()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDatabase"))
                .BuildServiceProvider();

            _logger = services.GetRequiredService<ILogger<VerificationService>>();
            _serviceProvider = services;
            _verificationService = new VerificationService(_logger, _serviceProvider);
        }

        [Fact]
        public void GenerateCode_ReturnsFiveDigitCode()
        {
            // Act
            var code = _verificationService.GenerateCode();

            // Assert
            Assert.NotNull(code);
            Assert.Equal(5, code.Length);
            Assert.True(int.TryParse(code, out _));
        }

        [Fact]
        public void GenerateEmailRequest_ValidRequest_ReturnsEmailRequest()
        {
            // Arrange
            var request = new VerificationRequest { Email = "test@example.com" };
            var code = "12345";

            // Act
            var result = _verificationService.GenerateEmailRequest(request, code);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.To);
            Assert.Contains(code, result.Subject);
            Assert.Contains(code, result.HtmlBody);
            Assert.Contains(code, result.PlainText);
        }

        [Fact]
        public void GenerateServiceBusEmailRequest_ValidEmailRequest_ReturnsPayload()
        {
            // Arrange
            var emailRequest = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Test",
                HtmlBody = "<p>Test</p>",
                PlainText = "Test"
            };

            // Act
            var result = _verificationService.GenerateServiceBusEmailRequest(emailRequest);

            // Assert
            Assert.NotNull(result);
            var payload = JsonConvert.DeserializeObject<EmailRequest>(result);
            Assert.Equal("test@example.com", payload.To);
            Assert.Equal("Test", payload.Subject);
            Assert.Equal("<p>Test</p>", payload.HtmlBody);
            Assert.Equal("Test", payload.PlainText);
        }
    }
}

using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VerificationProvider.Services;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VerificationProvider.Data.Entites;

namespace Verification.Tests
{
    public class ValidateVerificationCodeServiceTests
    {
        private readonly ValidateVerificationCodeService _validateVerificationCodeService;
        private readonly DataContext _context;
        private readonly ILogger<ValidateVerificationCodeService> _logger;

        public ValidateVerificationCodeServiceTests()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDatabase"))
                .BuildServiceProvider();

            _logger = services.GetRequiredService<ILogger<ValidateVerificationCodeService>>();
            _context = services.GetRequiredService<DataContext>();
            _validateVerificationCodeService = new ValidateVerificationCodeService(_logger, _context);
        }

        [Fact]
        public async Task UnpackValidateRequestAsync_ValidRequest_ReturnsValidateRequest()
        {
            // Arrange
            var validateRequest = new ValidateRequest { Email = "test@example.com", Code = "12345" };
            var json = JsonConvert.SerializeObject(validateRequest);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var context = new DefaultHttpContext();
            context.Request.Body = stream;

            // Act
            var result = await _validateVerificationCodeService.UnpackValidateRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(validateRequest.Email, result.Email);
            Assert.Equal(validateRequest.Code, result.Code);
        }

        [Fact]
        public async Task UnpackValidateRequestAsync_InvalidRequest_ReturnsNull()
        {
            // Arrange
            var json = "Invalid JSON";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var context = new DefaultHttpContext();
            context.Request.Body = stream;

            // Act
            var result = await _validateVerificationCodeService.UnpackValidateRequestAsync(context.Request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateCodeAsync_ValidCode_ReturnsTrue()
        {
            // Arrange
            var request = new VerificationRequestEntity
            {
                Email = "test@example.com",
                Code = "12345",
                ExpiryDate = DateTime.Now.AddMinutes(10)
            };
            _context.VerificationRequests.Add(request);
            await _context.SaveChangesAsync();

            var validateRequest = new ValidateRequest { Email = request.Email, Code = request.Code };

            // Act
            var result = await _validateVerificationCodeService.ValidateCodeAsync(validateRequest);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == request.Email && x.Code == request.Code));
        }

        [Fact]
        public async Task ValidateCodeAsync_InvalidCode_ReturnsFalse()
        {
            // Arrange
            var validateRequest = new ValidateRequest { Email = "test@example.com", Code = "wrongcode" };

            // Act
            var result = await _validateVerificationCodeService.ValidateCodeAsync(validateRequest);

            // Assert
            Assert.False(result);
        }
    }
}

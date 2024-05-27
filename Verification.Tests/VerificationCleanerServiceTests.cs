using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VerificationProvider.Services;
using VerificationProvider.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerificationProvider.Data.Entites;

namespace Verification.Tests
{
    public class VerificationCleanerServiceTests
    {
        private readonly VerificationCleanerService _verificationCleanerService;
        private readonly DataContext _context;
        private readonly ILogger<VerificationCleanerService> _logger;

        public VerificationCleanerServiceTests()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddDbContext<DataContext>(options => options.UseInMemoryDatabase("TestDatabase"))
                .BuildServiceProvider();

            _logger = services.GetRequiredService<ILogger<VerificationCleanerService>>();
            _context = services.GetRequiredService<DataContext>();
            _verificationCleanerService = new VerificationCleanerService(_logger, _context);
        }

        [Fact]
        public async Task RemoveExpiredRecordsAsync_RemovesExpiredRecords()
        {
            // Arrange
            var expiredRequest = new VerificationRequestEntity
            {
                Email = "expired@example.com",
                Code = "12345",
                ExpiryDate = DateTime.Now.AddMinutes(-1) // Expired
            };

            var validRequest = new VerificationRequestEntity
            {
                Email = "valid@example.com",
                Code = "67890",
                ExpiryDate = DateTime.Now.AddMinutes(10) // Not expired
            };

            _context.VerificationRequests.AddRange(expiredRequest, validRequest);
            await _context.SaveChangesAsync();

            // Act
            await _verificationCleanerService.RemoveExpiredRecordsAsync();

            // Assert
            var requests = await _context.VerificationRequests.ToListAsync();
            Assert.Single(requests);
            Assert.Equal("valid@example.com", requests[0].Email);
        }
    }
}

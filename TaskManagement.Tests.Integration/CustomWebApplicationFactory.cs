using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using TaskManagement.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace TaskManagement.Tests.Integration
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override settings for testing
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"JwtSettings:Secret", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789"},
                    {"JwtSettings:Issuer", "TestIssuer"},
                    {"JwtSettings:Audience", "TestAudience"},
                    {"JwtSettings:ExpirationInMinutes", "60"}
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext related services
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextDescriptor != null)
                    services.Remove(dbContextDescriptor);

                var dbContextOptionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextOptionsDescriptor != null)
                    services.Remove(dbContextOptionsDescriptor);

                // Also remove DbContextOptions (non-generic)
                var dbContextOptionsDescriptor2 = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions));
                if (dbContextOptionsDescriptor2 != null)
                    services.Remove(dbContextOptionsDescriptor2);

                // Add DbContext using in-memory database for testing
                services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}")
                           .EnableSensitiveDataLogging()
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });

            builder.UseEnvironment("Testing");
            
            // Suppress logs during testing for cleaner output
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Error);
            });
        }
    }
}

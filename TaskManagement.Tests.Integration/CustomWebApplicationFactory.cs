using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using TaskManagement.Infrastructure.Data;

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
                // Remove the existing DbContext configuration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Remove ApplicationDbContext
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Add DbContext using in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}");
                });
            });

            builder.UseEnvironment("Testing");
        }
    }
}

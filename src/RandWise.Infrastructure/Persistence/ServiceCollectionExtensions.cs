using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RandWise.Application.Auth;
using RandWise.Application.Budgeting;
using RandWise.Application.Common;
using RandWise.Application.Dashboard;
using RandWise.Application.FinancialProfile;
using RandWise.Application.Security;
using RandWise.Application.Transactions;
using RandWise.Application.WhatsApp;
using RandWise.Infrastructure.Budgeting;
using RandWise.Infrastructure.Dashboard;
using RandWise.Infrastructure.FinancialProfiles;
using RandWise.Infrastructure.Identity;
using RandWise.Infrastructure.Security;
using RandWise.Infrastructure.Transactions;
using RandWise.Infrastructure.WhatsApp;

namespace RandWise.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRandWisePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RandWisePersistenceOptions
        {
            ConnectionString = configuration[$"{RandWisePersistenceOptions.SectionName}:ConnectionString"]
                ?? new RandWisePersistenceOptions().ConnectionString
        };

        services.AddSingleton<SqliteWalConnectionInterceptor>();
        services.AddDbContext<RandWiseDbContext>((serviceProvider, dbContextOptions) =>
        {
            dbContextOptions
                .UseSqlite(options.ConnectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<SqliteWalConnectionInterceptor>());
        });

        services.AddIdentityCore<RandWiseIdentityUser>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;
                identityOptions.Password.RequiredLength = 8;
                identityOptions.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<RandWiseDbContext>();

        services.Configure<JwtTokenOptions>(configuration.GetSection(JwtTokenOptions.SectionName));
        services.Configure<SensitiveDataOptions>(configuration.GetSection(SensitiveDataOptions.SectionName));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddScoped<IRandWiseAuthService, RandWiseAuthService>();
        services.AddScoped<IFinancialProfileService, EfFinancialProfileService>();
        services.AddScoped<ITransactionService, EfTransactionService>();
        services.AddScoped<IBudgetPeriodService, EfBudgetPeriodService>();
        services.AddScoped<ICategoryService, EfCategoryService>();
        services.AddScoped<ICategoryBudgetService, EfCategoryBudgetService>();
        services.AddScoped<IRecurringTransactionService, EfRecurringTransactionService>();
        services.AddScoped<ISafeToSpendService, EfSafeToSpendService>();
        services.AddScoped<IDashboardService, EfDashboardService>();
        services.AddScoped<IWhatsAppContactService, EfWhatsAppContactService>();
        services.AddScoped<IWhatsAppWebhookService, EfWhatsAppWebhookService>();
        services.AddScoped<IWhatsAppMessageProcessor, EfWhatsAppMessageProcessor>();
        services.AddScoped<IDeterministicWhatsAppParser, DeterministicWhatsAppParser>();
        services.AddScoped<IWhatsAppOutboundClient, NoOpWhatsAppOutboundClient>();
        services.AddScoped<IWhatsAppWebhookVerifier, WhatsAppWebhookVerifier>();
        services.AddScoped<ISensitiveDataProtector, AesSensitiveDataProtector>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
        services.AddSingleton<IAccessTokenIssuer, ConfiguredAccessTokenIssuer>();
        services.AddSingleton<IRefreshTokenGenerator, CryptographicRefreshTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();

        return services;
    }
}

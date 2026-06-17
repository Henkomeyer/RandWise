using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RandWise.Contracts.Health;
using RandWise.Api.Endpoints;
using RandWise.Infrastructure.Persistence;
using RandWise.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

const string DevelopmentCorsPolicy = "DevelopmentCors";

builder.Services.AddOpenApi();
builder.Services.AddRandWisePersistence(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevelopmentCorsPolicy, policy =>
        policy
            .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtTokenOptions.SectionName)
            .Get<JwtTokenOptions>() ?? new JwtTokenOptions();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RandWiseDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors(DevelopmentCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");

api.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");

api.MapAuthEndpoints();
api.MapFinancialProfileEndpoints();
api.MapTransactionEndpoints();
api.MapCategoryEndpoints();
api.MapCategoryRuleEndpoints();
api.MapBudgetPeriodEndpoints();
api.MapCategoryBudgetRootEndpoints();
api.MapRecurringTransactionEndpoints();
api.MapDashboardEndpoints();
api.MapWhatsAppEndpoints();

app.Run();

public partial class Program;

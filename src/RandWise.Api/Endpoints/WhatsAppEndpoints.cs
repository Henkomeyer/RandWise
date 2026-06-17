using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RandWise.Application.WhatsApp;
using RandWise.Contracts.WhatsApp;
using AppException = RandWise.Application.Common.ApplicationException;

namespace RandWise.Api.Endpoints;

public static class WhatsAppEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapWhatsAppEndpoints(this RouteGroupBuilder api)
    {
        var whatsapp = api.MapGroup("/whatsapp")
            .RequireAuthorization()
            .WithTags("WhatsApp");

        whatsapp.MapGet("/status", async (
                ClaimsPrincipal user,
                IWhatsAppContactService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.GetStatusAsync(user.GetRequiredUserId(), cancellationToken)))
            .WithName("GetWhatsAppStatus");

        whatsapp.MapPost("/link", async (
                LinkWhatsAppRequest request,
                ClaimsPrincipal user,
                IWhatsAppContactService service,
                CancellationToken cancellationToken) =>
            await RunAsync(() => service.LinkAsync(user.GetRequiredUserId(), request, cancellationToken)))
            .WithName("LinkWhatsAppContact");

        whatsapp.MapPost("/unlink", async (
                ClaimsPrincipal user,
                IWhatsAppContactService service,
                CancellationToken cancellationToken) =>
            await RunAsync(async () =>
            {
                await service.UnlinkAsync(user.GetRequiredUserId(), cancellationToken);
                return Results.NoContent();
            }))
            .WithName("UnlinkWhatsAppContact");

        var webhooks = api.MapGroup("/webhooks/whatsapp")
            .WithTags("WhatsApp webhooks");

        webhooks.MapGet("/", (
                string? hub_mode,
                string? hub_verify_token,
                string? hub_challenge,
                IWhatsAppWebhookVerifier verifier) =>
            verifier.VerifyChallenge(hub_mode ?? string.Empty, hub_verify_token ?? string.Empty)
                ? Results.Text(hub_challenge ?? string.Empty)
                : Results.Unauthorized())
            .WithName("VerifyWhatsAppWebhook");

        webhooks.MapPost("/", async (
                HttpRequest httpRequest,
                IWhatsAppWebhookVerifier verifier,
                IWhatsAppWebhookService service,
                CancellationToken cancellationToken) =>
            {
                using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8);
                var rawBody = await reader.ReadToEndAsync(cancellationToken);
                var signature = httpRequest.Headers["X-Hub-Signature-256"].FirstOrDefault();

                if (!verifier.VerifyPayload(rawBody, signature))
                {
                    return Results.Unauthorized();
                }

                var request = JsonSerializer.Deserialize<WhatsAppWebhookRequest>(rawBody, JsonOptions);
                if (request is null)
                {
                    return Results.BadRequest();
                }

                var payloadHash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant()}";
                return await RunAsync(() => service.IngestAsync(request, payloadHash, cancellationToken));
            })
            .WithName("ReceiveWhatsAppWebhook");

        return api;
    }

    private static async Task<IResult> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return Results.Ok(await operation());
        }
        catch (AppException exception)
        {
            return exception.ToProblem();
        }
    }

    private static async Task<IResult> RunAsync(Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (AppException exception)
        {
            return exception.ToProblem();
        }
    }
}

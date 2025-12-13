using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

/// <summary>
/// Mock push notification provider for development/testing purposes.
/// In production, replace with providers like Firebase, Twilio Verify, or platform-specific implementations.
/// </summary>
public class MockPushNotificationProvider(ILogger<MockPushNotificationProvider> logger) : IPushNotificationProvider
{
    /// <inheritdoc />
    public string ProviderName => "Mock Push Provider";

    /// <inheritdoc />
    public async Task<bool> SendPushNotificationAsync(
        string pushToken,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Mock push notification sent to token {PushToken}: {Title} - {Body}",
            MaskToken(pushToken), title, body);

        logger.LogDebug(
            "Push notification data: {@Data}", data);

        // Simulate network delay
        await Task.Delay(100, cancellationToken);

        // Simulate success (in real implementation, check provider response)
        return true;
    }

    /// <inheritdoc />
    public bool ValidatePushToken(string pushToken, string platform)
    {
        if (string.IsNullOrWhiteSpace(pushToken))
            return false;

        return platform?.ToLowerInvariant() switch
        {
            "ios" => ValidateApnsToken(pushToken),
            "android" => ValidateFcmToken(pushToken),
            _ => false
        };
    }

    private static bool ValidateApnsToken(string token)
    {
        // APNS tokens are 64 hex characters (32 bytes)
        return token.Length == 64 && IsHexString(token);
    }

    private static bool ValidateFcmToken(string token)
    {
        // FCM tokens are typically longer and contain alphanumeric characters
        return token.Length >= 140 && token.Length <= 200;
    }

    private static bool IsHexString(string input)
    {
        return input.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    private static string MaskToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length < 8)
            return "***";

        return $"{token[..4]}***{token[^4..]}";
    }
}
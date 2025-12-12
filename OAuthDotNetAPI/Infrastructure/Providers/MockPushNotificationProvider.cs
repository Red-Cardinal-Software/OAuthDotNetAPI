using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Providers;

/// <summary>
/// Mock push notification provider for development/testing purposes.
/// In production, replace with providers like Firebase, Twilio Verify, or platform-specific implementations.
/// </summary>
public class MockPushNotificationProvider : IPushNotificationProvider
{
    private readonly ILogger<MockPushNotificationProvider> _logger;

    public MockPushNotificationProvider(ILogger<MockPushNotificationProvider> logger)
    {
        _logger = logger;
    }

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
        _logger.LogInformation(
            "Mock push notification sent to token {PushToken}: {Title} - {Body}",
            MaskToken(pushToken), title, body);

        _logger.LogDebug(
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

/// <summary>
/// Firebase Cloud Messaging push notification provider.
/// Replace the mock provider with this for production Firebase integration.
/// </summary>
public class FirebasePushNotificationProvider : IPushNotificationProvider
{
    private readonly ILogger<FirebasePushNotificationProvider> _logger;
    // private readonly FirebaseMessaging _messaging;

    public FirebasePushNotificationProvider(ILogger<FirebasePushNotificationProvider> logger)
    {
        _logger = logger;
        // Initialize Firebase messaging client
    }

    /// <inheritdoc />
    public string ProviderName => "Firebase Cloud Messaging";

    /// <inheritdoc />
    public async Task<bool> SendPushNotificationAsync(
        string pushToken,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Example Firebase implementation:
            /*
            var message = new Message
            {
                Token = pushToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await _messaging.SendAsync(message, cancellationToken);
            _logger.LogInformation("Firebase message sent: {MessageId}", response);
            return true;
            */

            _logger.LogWarning("Firebase provider not fully implemented");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Firebase push notification");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidatePushToken(string pushToken, string platform)
    {
        // Firebase tokens work for both iOS and Android
        return !string.IsNullOrWhiteSpace(pushToken) && 
               pushToken.Length >= 140 && 
               pushToken.Length <= 200;
    }
}
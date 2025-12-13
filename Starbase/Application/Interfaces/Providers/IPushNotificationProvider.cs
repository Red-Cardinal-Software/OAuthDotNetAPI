namespace Application.Interfaces.Providers;

/// <summary>
/// Interface for push notification providers (Twilio, Firebase, etc.).
/// </summary>
public interface IPushNotificationProvider
{
    /// <summary>
    /// Sends a push notification for MFA.
    /// </summary>
    /// <param name="pushToken">The device's push token.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body.</param>
    /// <param name="data">Additional data payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sent successfully.</returns>
    Task<bool> SendPushNotificationAsync(
        string pushToken,
        string title,
        string body,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a push token format.
    /// </summary>
    /// <param name="pushToken">The token to validate.</param>
    /// <param name="platform">The platform type.</param>
    /// <returns>True if valid.</returns>
    bool ValidatePushToken(string pushToken, string platform);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }
}
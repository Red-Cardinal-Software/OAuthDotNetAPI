namespace Application.Logging;

/// <summary>
/// Represents a no-op implementation of the <see cref="IDisposable"/> pattern.
/// </summary>
/// <remarks>
/// This class is used as a singleton to represent an empty or null
/// scope for logging or other contextual operations where no actual
/// resources or state are managed.
/// </remarks>
public sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    private NullScope() { }

    public void Dispose() { }
}

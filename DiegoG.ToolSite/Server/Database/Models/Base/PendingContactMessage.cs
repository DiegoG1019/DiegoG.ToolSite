namespace DiegoG.ToolSite.Server.Database.Models.Base;

/// <summary>
/// Represents a message that still has not been communicated to an administrator
/// </summary>
public class PendingContactMessage : IKeyed<PendingContactMessage>, IDispatchable
{
    /// <summary>
    /// The Id of the message
    /// </summary>
    public required Id<PendingContactMessage> Id { get; init; }

    /// <summary>
    /// The message itself
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The address at which a response can be communicated
    /// </summary>
    public required string? ResponseAddress { get; init; }

    /// <summary>
    /// The medium through which a response can be communicated
    /// </summary>
    public required string? ResponseMedium { get; init; }

    /// <inheritdoc/>
    public DateTimeOffset? ClaimedAt { get; set; }

    /// <inheritdoc/>
    public ServerInfo? ClaimedBy { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? DispatchedAt { get; set; }
}

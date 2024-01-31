namespace RemoteForge;

/// <summary>
/// The remote forge implementation. This is used as a way to describe the
/// connection and provide a common mechanism to generate the transport object
/// based on the connection properties.
/// </summary>
public interface IRemoteForge
{
    /// <summary>
    /// The Forge identifier used with Register-RemoteForge. This should be
    /// unique as it's used by the Uri scheme to determine what transport to
    /// use.
    /// </summary>
    protected internal static abstract string ForgeName { get; }

    /// <summary>
    /// The description of the transport to use with the forge registration.
    /// It should describe the transport as it is displayed with the
    /// registration info.
    /// </summary>
    protected internal static virtual string? ForgeDescription => null;

    /// <summary>
    /// Gets a string that describes the transport. It is used to identify the
    /// transport when displaying an error.
    /// </summary>
    /// <returns>The transport string.</returns>
    string GetTransportString() => this.GetType().Name;

    /// <summary>
    /// Called to create the transport instance that does the actual remote
    /// communication.
    /// </summary>
    /// <returns>The IRemoteForgeTransport implementation.</returns>
    IRemoteForgeTransport CreateTransport();

    /// <summary>
    /// Called when creating the forge information instance with the URI
    /// provided by the user.
    /// </summary>
    /// <param name="info">The string containing the connection info.</param>
    /// <returns>The IRemoteForge instance for the string provided.</returns>
    protected internal static abstract IRemoteForge Create(string info);
}

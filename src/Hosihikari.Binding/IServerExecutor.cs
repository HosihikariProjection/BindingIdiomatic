namespace Hosihikari.Binding;

/// <summary>Runs work on the carrier's server thread.</summary>
/// <remarks>
/// The carrier supplies the mechanism — LeviLamina's server-thread executor, Endstone's
/// scheduler. This layer only decides what runs and in which order; it never creates a thread
/// of its own.
/// </remarks>
public interface IServerExecutor
{
    /// <summary>Queues <paramref name="work"/> to run on the server thread at its next safe point.</summary>
    /// <param name="work">The work to run. It must not block for long; the server thread runs it.</param>
    void Post(Action work);
}

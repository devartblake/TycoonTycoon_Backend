using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Tests.Helpers;

/// Simple in-memory session store for unit tests.
internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly Dictionary<Guid, SecureSession> _sessions = new();

    public Task SaveAsync(SecureSession session, CancellationToken ct)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct)
        => Task.FromResult(_sessions.GetValueOrDefault(sessionId));

    public Task DeleteAsync(Guid sessionId, CancellationToken ct)
    {
        _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }

    public SecureSession? this[Guid id] => _sessions.GetValueOrDefault(id);
}

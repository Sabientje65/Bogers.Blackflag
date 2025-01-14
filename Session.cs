﻿using System.Collections.Concurrent;

namespace Bogers.Blackflag;

/// <summary>
/// Persistent key-value store, used to bind settings etc. to a specific key
///
/// Sessions are stored in memory by default and will thus be wiped on application restart!
/// </summary>
public class Session
{
    private static readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();
    private readonly IDictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Current active session, only one session can be 'active' at a time!
    /// </summary>
    public static Session? ActiveSession { get; private set; }
    
    private Session() { }
    
    /// <summary>
    /// Session key, can be used for future requests
    /// </summary>
    public string Key { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Timestamp the session was created
    /// </summary>
    public DateTime Created { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp the session was last retrieved
    /// </summary>
    public DateTime LastRetrieval { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns true when the session has no values set yet
    /// </summary>
    public bool IsEmpty => !_values.Any();
    
    /// <summary>
    /// Read the value associated with the given key
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>Value for the given key</returns>
    public string ReadStringValue(string key) => _values.TryGetValue(key, out var value) ? value : String.Empty;

    /// <summary>
    /// Write a value to the given key
    /// </summary>
    /// <param name="key">Key to write value to</param>
    /// <param name="value">Value</param>
    public void WriteStringValue(string key, string value) => _values[key] = value;

    /// <summary>
    /// Alias for <see cref="ReadStringValue"/> and <see cref="WriteStringValue"/> respectively
    /// </summary>
    public string this[string key]
    {
        get => ReadStringValue(key);
        set => _values[key] = value;
    }

    /// <summary>
    /// Make the current session the 'active' session
    /// </summary>
    public Session MakeActive() => ActiveSession = this;

    /// <summary>
    /// Clear the current session
    /// </summary>
    public void Clear() => _values.Clear();
    
    // public string this[string key] => ReadStringValue(key);
    
    /// <summary>
    /// Create a new session with a random key and returns it
    /// </summary>
    /// <returns>Create a new session</returns>
    public static Session New()
    {
        var session = new Session();
        return session;
    }

    /// <summary>
    /// Expunges all expired sessions
    /// </summary>
    public static void ExpungeExpired()
    {
        // todo :P 
        throw new NotImplementedException();
    }

    /// <summary>
    /// Locate the session with the given key, returns NULL when no session was found
    /// </summary>
    /// <param name="key">Session key</param>
    /// <returns>Session or NULL</returns>
    public static Session? FromKey(string key) => _sessions.TryGetValue(key, out var session) ? session : null;
}
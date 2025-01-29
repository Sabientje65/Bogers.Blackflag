namespace Bogers.Blackflag;

/// <summary>
/// Typed wrapper around a twitcasting session
/// </summary>
public class TwitCastingDownloadSession
{
    private readonly Session _session;
    
    public TwitCastingDownloadSession(Session session)
    {
        _session = session;
    }

    /// <summary>
    /// Hostname to use for subsequent requests, should use the following scheme {videoid?}.twitcasting.tv, eg. dl193250.twitcasting.tv
    /// </summary>
    public string Host
    {
        get => _session["twitcasting.videoid"];
        set => _session["twitcasting.videoid"] = value;
    }

    /// <summary>
    /// UserAgent header to use for subsequent requests
    /// </summary>
    public string UserAgent
    {
        get => _session["twitcasting.useragent"];
        set => _session["twitcasting.useragent"] = value;
    }

    /// <summary>
    /// Assumed to be twitcastings userid, extracted from `did` cookie
    /// </summary>
    public string UserId
    {
        get => _session["twitcasting.userid"];
        set => _session["twitcasting.userid"] = value;
    }
    
    /// <summary>
    /// Assumed to be a displayname/userfriendly id, extracted from `fftc_id` cookie
    /// </summary>
    public string UserFriendlyId
    {
        get => _session["twitcasting.userfriendlyid"];
        set => _session["twitcasting.userfriendlyid"] = value;
    }
    
    /// <summary>
    /// Assumed to be a session id of some kind, extracted from `tc_ss` cookie
    /// </summary>
    public string SessionId
    {
        get => _session["twitcasting.sessionid"];
        set => _session["twitcasting.sessionid"] = value;
    }
}
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;

namespace Bogers.Blackflag;

public class TwitCasting
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwitCastingDownloadSession _session;
    
    public TwitCasting(
        IHttpClientFactory httpClientFactory,
        Session session
    )
    {
        _httpClientFactory = httpClientFactory;
        _session = new TwitCastingDownloadSession(session);
    }

    /// <summary>
    /// Proxies the request to TwitCasting authenticating using the current session
    /// </summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    public async Task ProxyToTwitCasting(HttpRequest request, HttpResponse response)
    {
        using var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Host = _session.Host;
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _session.UserAgent);
        http.DefaultRequestHeaders.Add("Cookie", $"keep=1; hl=en; did={_session.UesrId}; fftc_id={_session.UserFriendlyId}; tc_ss={_session.SessionId}");
   
        
        var scheme = request.Scheme ?? string.Empty;
        var host = _session.Host;
        var pathBase = request.PathBase.Value ?? string.Empty;
        var path = request.Path.Value ?? string.Empty;
        var queryString = request.QueryString.Value ?? string.Empty;
        
        // taken from request.GetDisplayUrl()
        var url = new StringBuilder()
            .Append(scheme)
            .Append(Uri.SchemeDelimiter)
            .Append(host)
            .Append(pathBase)
            .Append(path)
            .Append(queryString)
            .ToString();
        
        // assume just GET requests for now
        var res = await http.GetAsync(url);
        await res.Content.CopyToAsync(response.Body);
        await response.Body.FlushAsync();
    }
}

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
    public string UesrId
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
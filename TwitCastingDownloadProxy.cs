using System.Text;

namespace Bogers.Blackflag;

public class TwitCastingDownloadProxy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwitCastingDownloadSession _session;
    
    public TwitCastingDownloadProxy(
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
        http.DefaultRequestHeaders.Add("Cookie", $"keep=1; hl=en; did={_session.UserId}; fftc_id={_session.UserFriendlyId}; tc_ss={_session.SessionId}");
   
        
        var scheme = "https";
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
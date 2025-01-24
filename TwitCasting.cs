using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

namespace Bogers.Blackflag;

public class TwitCastingAuthenticator
{
    public static async Task Test()
    {
        await new TwitCastingAuthenticator().Authenticate();
    }
    
    // when running 
    public async Task Authenticate()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var firefox = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });

        var page = await firefox.NewPageAsync();
        await CachedAuthentication(page);
        await page.GotoAsync("https://twitcasting.tv");
        // var myCookies = (await page.Context.CookiesAsync(["https://twitcasting.tv"]));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var myId = await ReadCookie(page, "tc_id");
        var myTickets = await page.GotoAsync($"https://twitcasting.tv/{myId}/shopmytickets");
        
        // find all tickets
        var ticketsLocator = page
            .Locator(".tw-shop-ticket-card2");

        var ticketNames = await ticketsLocator.Locator(".tw-shop-ticket-card2-title")
            .AllInnerTextsAsync();
        
        // fake selection -> first available ticket
        await ticketsLocator.GetByText(ticketNames[0]).ClickAsync(); // navigate to event page
        await page.GetByText("Go to archive page").ClickAsync(); // navigate to archives page
        
        // text may be cutoff, assume first 10 characters are suffice for identifying our archive
        await page.GetByText(ticketNames[0][..Math.Min(ticketNames[0].Length, 10)]).ClickAsync(); // navigate to event archive
        
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<string> ReadCookie(IPage page, string cookieName) => (await page.Context.CookiesAsync(["https://twitcasting.tv"]))
        !.SingleOrDefault(c => c.Name == cookieName)
        !.Value;

    private async Task CachedAuthentication(IPage page)
    {
        var initialCookies = JsonSerializer.Deserialize<BrowserContextCookiesResult[]>(
            await File.ReadAllTextAsync("E:/src/Bogers.BlackFlag/.data/cookies.json")    
        )!;

        var twitCastingCookies = initialCookies
            .Where(x => x.Domain == ".twitcasting.tv");
        // page.Context.CookiesAsync(initialCookies)

        await page.Context.AddCookiesAsync(twitCastingCookies.Select(c => new Cookie
        {
            Domain = c.Domain,
            HttpOnly = c.HttpOnly,
            Name = c.Name,
            Path = c.Path,
            SameSite = c.SameSite,
            Secure = c.Secure,
            // Url = c.,
            Value = c.Value
        }));
        
    }

    private async Task FreshAuthentication(IPage page)
    {
        var credentials = JsonSerializer.Deserialize<JsonObject>(
            await File.ReadAllTextAsync("E:/src/Bogers.BlackFlag/.data/cookies.json")
        );
        
        await page.GotoAsync("https://twitcasting.tv/indexloginwindow.php");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await page.ClickAsync(".tw-casaccount-button[aria-label=\"Twitter\"]"); // twitter login button
        
        // twitter oauth page
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await page.HoverAsync("#allow");
        
        // for some reason needs to be clicked twice?
        await page.ClickAsync("#allow");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await page.ClickAsync("#allow"); // needs to be clicked twice for some reason

        // force focus
        await page.ClickAsync("[autocomplete=\"username\"]");
        
        // twitter signin page, fill username
        await page.Locator("[autocomplete=\"username\"]").FillAsync(credentials["email"].ToString());
        await page.GetByText("Next").ClickAsync();

        // await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(750, 2500)));
        
        // only when verification actually appears!
        await page.Locator("[data-testid=\"ocfEnterTextTextInput\"]").FillAsync(credentials["phonenumber"].ToString());
        await page.GetByText("Next").ClickAsync();

        // fake human reaction speed
        // await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(750, 2500)));
        
        await page.ClickAsync("[name=\"password\"]");
        
        // wait for login form to load
        await page.Locator("[name=\"password\"]").WaitForAsync();
        await page.Locator("[name=\"password\"]").FillAsync(credentials["password"].ToString());
        
        // fake human reaction speed
        // await Task.Delay(TimeSpan.FromMilliseconds( Random.Shared.Next(750, 2500) ));
        await page.GetByText("Log in").FocusAsync();
        // fake human reaction speed
        // await Task.Delay(TimeSpan.FromMilliseconds( Random.Shared.Next(10, 50) ));
        
        await page.GetByText("Log in").ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForURLAsync(url => url.Contains("twitcasting.tv", StringComparison.OrdinalIgnoreCase));

        var cookies = await page.Context.CookiesAsync();
        var x = cookies;
    }
}

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
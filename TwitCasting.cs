using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright;

namespace Bogers.Blackflag;

public class TwitCasting : IAsyncDisposable
{
    private IPlaywright Playwright { get; init; }
    private IBrowser Browser { get; init; }
    private IPage TwitCastingPage { get; init; }

    private readonly string _storagePath = "E:/src/Bogers.BlackFlag/.data/cookies.json";
    private readonly string _credentialsPath = "E:/src/Bogers.BlackFlag/.data/credentials.json";
    
    public static async Task<TwitCasting> Create()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var firefox = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await firefox.NewPageAsync();
        await page.GotoAsync("https://twitcasting.tv");

        return new TwitCasting
        {
            Playwright = playwright,
            Browser = firefox,
            TwitCastingPage = page
        };
    }

    /// <summary>
    /// Populate the given session as TwitCasting session, making the session eligible for twitcasting downloads
    /// </summary>
    public async Task PopulateDownloadSession(
        Session session,
        string playlistFile
    )
    {
        var twitCastingSession = new TwitCastingDownloadSession(session)
        {
            UserFriendlyId = await ReadCookie("tc_id"),
            UserId = await ReadCookie("did"),
            SessionId = await ReadCookie("tc_ss"),
            UserAgent = await TwitCastingPage.EvaluateAsync<string>("navigator.userAgent"),
            Host = new Uri(playlistFile).Host
        };
    }
    
    /// <summary>
    /// Authenticate the current TwitCasting instance by signing in to TwitCasting
    /// </summary>
    public async Task Authenticate()
    {
        if (File.Exists(_storagePath)) await AuthenticateFromSavedCookies();
        else await AuthenticateViaTwitter();
    }

    /// <summary>
    /// Get a collection of all tickets belonging to current user
    /// </summary>
    /// <returns></returns>
    public async Task<string[]> GetMyTickets()
    {
        var myId = await ReadCookie("tc_id");
        await TwitCastingPage.GotoAsync($"https://twitcasting.tv/{myId}/shopmytickets");
        
        // find all tickets
        var ticketsLocator = TwitCastingPage.Locator(".tw-shop-ticket-card2");

        var ticketNames = await ticketsLocator.Locator(".tw-shop-ticket-card2-title")
            .AllInnerTextsAsync();

        return ticketNames.ToArray();
    }

    /// <summary>
    /// Get the m3u8 file associated with the given ticket
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetTicketPlaylistFile(string ticketName)
    {
        var myId = await ReadCookie("tc_id");
        await TwitCastingPage.GotoAsync($"https://twitcasting.tv/{myId}/shopmytickets");
        
        // find all tickets
        var ticketsLocator = TwitCastingPage.Locator(".tw-shop-ticket-card2");

        await ticketsLocator.GetByText(ticketName).ClickAsync(); // navigate to event page
        await TwitCastingPage.GetByText("Go to archive page").ClickAsync(); // navigate to archives page
        
        // text may be cutoff, assume first 10 characters are suffice for identifying our archive
        await TwitCastingPage.GetByText(ticketName[..Math.Min(ticketName.Length, 10)]).ClickAsync(); // navigate to event archive

        var playlistFile = await TwitCastingPage.RunAndWaitForResponseAsync(
            async () => await TwitCastingPage.ClickAsync(".vjs-big-play-button"),
            response => response.Url.Contains("master.m3u8")
        );

        return playlistFile.Url;
    }

    /// <summary>
    /// Authenticate from cookies saved to disk
    /// </summary>
    private async Task AuthenticateFromSavedCookies()
    {
        var initialCookies = JsonSerializer.Deserialize<BrowserContextCookiesResult[]>(
            await File.ReadAllTextAsync(_storagePath)    
        )!;

        var twitCastingCookies = initialCookies
            .Where(x => x.Domain == ".twitcasting.tv");
        
        await TwitCastingPage.Context.AddCookiesAsync(twitCastingCookies.Select(c => new Cookie
        {
            Domain = c.Domain,
            HttpOnly = c.HttpOnly,
            Name = c.Name,
            Path = c.Path,
            SameSite = c.SameSite,
            Secure = c.Secure,
            Value = c.Value
        }));
    }
    
    /// <summary>
    /// Signin via twitter oauth flow
    ///
    /// Requires twitter credentials to be configured
    /// </summary>
    private async Task AuthenticateViaTwitter()
    {
        var credentials = JsonSerializer.Deserialize<JsonObject>(
            await File.ReadAllTextAsync(_credentialsPath)
        );
        
        await TwitCastingPage.GotoAsync("https://twitcasting.tv/indexloginwindow.php");
        await TwitCastingPage.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await TwitCastingPage.ClickAsync(".tw-casaccount-button[aria-label=\"Twitter\"]"); // twitter login button
        
        // twitter oauth page
        await TwitCastingPage.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await TwitCastingPage.HoverAsync("#allow");
        
        // for some reason needs to be clicked twice?
        await TwitCastingPage.ClickAsync("#allow");
        await TwitCastingPage.WaitForLoadStateAsync(LoadState.NetworkIdle); // wait for requests to finish
        await TwitCastingPage.ClickAsync("#allow"); // needs to be clicked twice for some reason
        
        // twitter signin page, fill username
        await TwitCastingPage.Locator("[autocomplete=\"username\"]").FillAsync(credentials["email"].ToString());
        await TwitCastingPage.GetByText("Next").ClickAsync();

        // only when verification actually appears!
        await TwitCastingPage.Locator("[data-testid=\"ocfEnterTextTextInput\"]").FillAsync(credentials["phonenumber"].ToString());
        await TwitCastingPage.GetByText("Next").ClickAsync();
        
        // wait for login form to load
        await TwitCastingPage.Locator("[name=\"password\"]").WaitForAsync();
        await TwitCastingPage.Locator("[name=\"password\"]").FillAsync(credentials["password"].ToString());
        
        await TwitCastingPage.GetByText("Log in").ClickAsync();

        await TwitCastingPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TwitCastingPage.WaitForURLAsync(url => url.Contains("twitcasting.tv", StringComparison.OrdinalIgnoreCase));

        
        // todo: save cookies to disk
        // var cookies = await page.Context.CookiesAsync();
    }
    
    private async Task<string> ReadCookie(string cookieName) => (await TwitCastingPage.Context.CookiesAsync(["https://twitcasting.tv"]))
        !.SingleOrDefault(c => c.Name == cookieName)
        !.Value;
    
    public async ValueTask DisposeAsync()
    {
        if (Playwright is IAsyncDisposable playwrightAsyncDisposable) await playwrightAsyncDisposable.DisposeAsync();
        else Playwright.Dispose();
        
        
        await Browser.DisposeAsync();
    }
}

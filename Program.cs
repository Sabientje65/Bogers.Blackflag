using Bogers.Blackflag;

// await TwitCastingAuthenticator.Test();
//
// return;

// meant for internal use only!
// todo: ffmpeg generator endpoint -> ffmpeg -i "MY_URL" -c copy MY_OUTPUT.mkv

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddHttpClient()
    .AddScoped<Session>(_ => Session.ForPort(80)) // todo: pick dynamic ports to listen to -> bind sessions to
    .AddScoped<TwitCastingDownloadProxy>();

var app = builder.Build();

app.Use(async (HttpContext context, Func<Task> _) =>
{
    // assume twitcasting for now
    var downloadProxy = context.RequestServices.GetRequiredService<TwitCastingDownloadProxy>();
    await downloadProxy.ProxyToTwitCasting(context.Request, context.Response);
});

await PrintDownloadLink();

app.Run();


// todo: make dynamic, expose interface for selecting a download link
async Task PrintDownloadLink()
{
    await using var twitCasting = await TwitCasting.Create();
    await twitCasting.Authenticate();
    
    var myTickets = await twitCasting.GetMyTickets();
    var playlistFile = await twitCasting.GetTicketPlaylistFile(myTickets[0]);
 
    // also populate default session
    await twitCasting.PopulateDownloadSession(
        Session.ForPort(80),
        playlistFile
    );
    
    Console.WriteLine($"ffmpeg generator endpoint -> ffmpeg -i \"http://localhost:5000{new Uri(playlistFile).PathAndQuery}\" -c copy MY_OUTPUT.mkv");
}

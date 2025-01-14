using Bogers.Blackflag;
using Microsoft.AspNetCore.Mvc;

// meant for internal use only!
// todo: ffmpeg generator endpoint -> ffmpeg -i "MY_URL" -c copy MY_OUTPUT.mkv

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddSingleton<Session>(_ => Session.ActiveSession ?? Session.New().MakeActive())
    .AddHttpClient()
    .AddScoped<TwitCasting>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var session = context.RequestServices.GetRequiredService<Session>();
    
    if (
        !context.Request.Path.Equals("/configure") &&
        session.IsEmpty
    )
    {
        context.Response.Redirect("/configure");
        return;
    }

    if (
        context.Request.Path.Equals("/configure")
    )
    {
        await next();
        return;
    }
    
    // assume twitcasting for now
    var twitCasting = context.RequestServices.GetRequiredService<TwitCasting>();
    await twitCasting.ProxyToTwitCasting(context.Request, context.Response);
});

// ensure an active session is always present
// app.Use(async (_, next) =>
// {
//     if (Session.ActiveSession == null) Session.New().MakeActive();
//     await next();
// });

app.MapPost("/configure", async (HttpRequest request, [FromServices] Session session) =>
{
    var form = await request.ReadFormAsync();
    var twitCastingSession = new TwitCastingDownloadSession(session);

    if (form.TryGetValue("sessionId", out var sessionId) && !String.IsNullOrEmpty("sessionId")) twitCastingSession.SessionId = sessionId;
    if (form.TryGetValue("host", out var host) && !String.IsNullOrEmpty("host")) twitCastingSession.Host = host;
    if (form.TryGetValue("userId", out var userId) && !String.IsNullOrEmpty("userId")) twitCastingSession.UesrId = userId;
    if (form.TryGetValue("userFriendlyId", out var userFriendlyId) && !String.IsNullOrEmpty("userFriendlyId")) twitCastingSession.UserFriendlyId = userFriendlyId;
    twitCastingSession.UserAgent = request.Headers.UserAgent.ToString();
});

app.MapGet("/configure", ([FromServices] Session session) =>
{
    string FormField (string key) => $"""
        <div>
            <label for="{key}">{key}</label>
            <input id="{key}" name="{key}" value="{session[key]}" required />
        </div>
    """;
    
    return TypedResults.Text($"""
    <!DOCTYPE HTML>
    <html>
      <head>
          <title>Login</title>
      </head>
      <body>
          <form method="post" action="/configure">
              {FormField("host")}
              {FormField("userId")}
              {FormField("userFriendlyId")}
              {FormField("sessionId")}
              
              <div>
                  <input type="submit" />
              </div>
          </form>
      </body>
    </html>
    """, "text/html");
});

app.Run();


// static class Modes
// {
//     public static string CurrentMode = Twitcasting;
//     
//     /// <summary>
//     /// Twitcasting mode, all future non-mode requests will be proxied to Twitcasting
//     /// </summary>
//     public const string Twitcasting = "Twitcasting";
// }
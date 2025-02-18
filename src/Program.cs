using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", (HttpContext httpContext) =>
{
    var res = "";

    if (!string.IsNullOrEmpty(httpContext.Request.Headers["X-Forwarded-For"]))
    {
        var q = httpContext.Request.Headers["X-Forwarded-For"].ToString();

        if (q is not null)
        {
            if (q.StartsWith("["))
            {
                var ips = JsonSerializer.Deserialize<string[]>(q);

                if (ips is not null && ips.Length > 0)
                    res = ips.First();
            }
            else
                res = q;
        }
    }
    else
        res = httpContext.Connection?.RemoteIpAddress?.ToString();

    return Results.Text(res);
});

app.Run();

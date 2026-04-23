using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors();

const string DemoUsername = "test";
const string DemoApiKey = "demo-api-key-123";

var sessions = new ConcurrentDictionary<string, string>();
var csrfCounters = new ConcurrentDictionary<string, int>();
var replayCounters = new ConcurrentDictionary<string, int>();

app.MapGet("/", () => Results.Ok(new
{
    message = "Authentication demo API",
    endpoints = new[]
    {
        "POST /login-session",
        "POST /login-jwt",
        "GET /protected-session",
        "GET /protected-jwt",
        "GET /protected-basic",
        "GET /protected-apikey",
        "GET /simulate-csrf-state-change",
        "POST /simulate-token-theft",
        "POST /simulate-replay",
        "GET /oauth-mock/authorize?username=test",
        "POST /oauth-mock/token"
    }
}));

app.MapPost("/login-session", (LoginRequest request, HttpResponse response) =>
{
    if (!IsValidUser(request.Username, request.Password))
    {
        return Results.Unauthorized();
    }

    var sessionId = Guid.NewGuid().ToString("N");
    sessions[sessionId] = request.Username;

    response.Cookies.Append("demo_session_id", sessionId, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = false,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddHours(1)
    });

    return Results.Ok(new
    {
        message = "Session cookie created",
        note = "Cookie HttpOnly oldugu icin JavaScript tarafindan okunamaz; CSRF riski ise cookie'nin otomatik gonderilmesinden kaynaklanir."
    });
});

app.MapPost("/login-jwt", (LoginRequest request) =>
{
    if (!IsValidUser(request.Username, request.Password))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        token = CreateJwt(request.Username),
        tokenType = "Bearer",
        expiresInSeconds = 3600,
        note = "Web demosu bu token'i localStorage'a koyarak XSS etkisini gosterir."
    });
});

app.MapGet("/protected-session", (HttpRequest request) =>
{
    if (!TryGetSessionUser(request, sessions, out var username))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        message = "Session protected endpoint success",
        user = username,
        authModel = "Session cookie"
    });
});

app.MapGet("/protected-jwt", (HttpRequest request) =>
{
    if (!TryGetBearerToken(request, out var token))
    {
        return Results.Unauthorized();
    }

    var jwt = ValidateJwt(token);
    if (!jwt.IsValid)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        message = "JWT protected endpoint success",
        user = jwt.Username,
        authModel = "Bearer JWT"
    });
});

app.MapGet("/protected-basic", (HttpRequest request) =>
{
    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return Results.Unauthorized();
    }

    var value = authHeader.ToString();
    if (!value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    try
    {
        var encoded = value["Basic ".Length..].Trim();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var parts = decoded.Split(':', 2);

        if (parts.Length == 2 && IsValidUser(parts[0], parts[1]))
        {
            return Results.Ok(new
            {
                message = "Basic auth protected endpoint success",
                user = parts[0],
                authModel = "Basic Authentication"
            });
        }
    }
    catch (FormatException)
    {
        return Results.Unauthorized();
    }

    return Results.Unauthorized();
});

app.MapGet("/protected-apikey", (HttpRequest request) =>
{
    if (request.Headers.TryGetValue("x-api-key", out var apiKey) && apiKey == DemoApiKey)
    {
        return Results.Ok(new
        {
            message = "API key protected endpoint success",
            authModel = "API Key",
            note = "CLI gibi machine-to-machine senaryolarinda basit bir ornek."
        });
    }

    return Results.Unauthorized();
});

app.MapGet("/simulate-csrf-state-change", (HttpRequest request) =>
{
    if (!TryGetSessionUser(request, sessions, out var username))
    {
        return Results.Unauthorized();
    }

    var count = csrfCounters.AddOrUpdate(username, 1, (_, current) => current + 1);

    return Results.Ok(new
    {
        warning = "CSRF demo: Session cookie otomatik gonderildigi icin state degisen GET istegi calisti.",
        user = username,
        unsafeCounter = count
    });
});

app.MapPost("/simulate-token-theft", (TokenRequest request) =>
{
    var jwt = ValidateJwt(request.Token);
    if (!jwt.IsValid)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        warning = "Token theft demo: Token'i bilen kisi ayni kullanici gibi istek atabilir.",
        acceptedAsUser = jwt.Username
    });
});

app.MapPost("/simulate-replay", (HttpRequest request) =>
{
    if (!TryGetBearerToken(request, out var token))
    {
        return Results.Unauthorized();
    }

    var jwt = ValidateJwt(token);
    if (!jwt.IsValid)
    {
        return Results.Unauthorized();
    }

    var requestId = request.Headers.TryGetValue("x-demo-request-id", out var header)
        ? header.ToString()
        : "missing-request-id";

    var key = $"{jwt.Username}:{requestId}";
    var count = replayCounters.AddOrUpdate(key, 1, (_, current) => current + 1);

    return Results.Ok(new
    {
        warning = "Replay demo: Ayni token ve request id tekrar kullanildi.",
        user = jwt.Username,
        requestId,
        seenCount = count,
        replayed = count > 1
    });
});

app.MapGet("/oauth-mock/authorize", (string username) =>
{
    if (username != DemoUsername)
    {
        return Results.BadRequest(new { error = "Unknown demo user" });
    }

    return Results.Ok(new
    {
        code = "mock-code-test",
        note = "Gercek OAuth provider yoktur. Bu kodu POST /oauth-mock/token endpointine gonderin."
    });
});

app.MapPost("/oauth-mock/token", (OAuthTokenRequest request) =>
{
    if (request.Code != "mock-code-test")
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        accessToken = CreateJwt(DemoUsername),
        tokenType = "Bearer",
        note = "Mock OAuth benzeri akisin token adimi."
    });
});

app.Run();

static bool IsValidUser(string username, string password)
{
    return username == "test" && password == "test123";
}

static bool TryGetSessionUser(
    HttpRequest request,
    ConcurrentDictionary<string, string> sessions,
    out string username)
{
    username = "";

    if (!request.Cookies.TryGetValue("demo_session_id", out var sessionId))
    {
        return false;
    }

    return sessions.TryGetValue(sessionId, out username!);
}

static bool TryGetBearerToken(HttpRequest request, out string token)
{
    token = "";

    if (!request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        return false;
    }

    var value = authHeader.ToString();
    if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    token = value["Bearer ".Length..].Trim();
    return token.Length > 0;
}

static string CreateJwt(string username)
{
    var header = new { alg = "HS256", typ = "JWT" };
    var payload = new
    {
        sub = username,
        name = username,
        exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
    };

    var headerJson = JsonSerializer.Serialize(header);
    var payloadJson = JsonSerializer.Serialize(payload);
    var unsignedToken = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson))}";
    var signature = Sign(unsignedToken);

    return $"{unsignedToken}.{signature}";
}

static JwtValidationResult ValidateJwt(string token)
{
    var parts = token.Split('.');
    if (parts.Length != 3)
    {
        return new JwtValidationResult(false, null);
    }

    var unsignedToken = $"{parts[0]}.{parts[1]}";
    var expectedSignature = Sign(unsignedToken);

    var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
    var actualBytes = Encoding.UTF8.GetBytes(parts[2]);

    if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
    {
        return new JwtValidationResult(false, null);
    }

    try
    {
        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;
        var exp = root.GetProperty("exp").GetInt64();

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
        {
            return new JwtValidationResult(false, null);
        }

        var username = root.GetProperty("sub").GetString();
        return new JwtValidationResult(username is not null, username);
    }
    catch (JsonException)
    {
        return new JwtValidationResult(false, null);
    }
    catch (FormatException)
    {
        return new JwtValidationResult(false, null);
    }
    catch (KeyNotFoundException)
    {
        return new JwtValidationResult(false, null);
    }
}

static string Sign(string value)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("demo-secret-key-for-hs256-signature-only"));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    return Base64UrlEncode(hash);
}

static string Base64UrlEncode(byte[] bytes)
{
    return Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

static byte[] Base64UrlDecode(string value)
{
    var padded = value.Replace('-', '+').Replace('_', '/');
    padded += (padded.Length % 4) switch
    {
        2 => "==",
        3 => "=",
        _ => ""
    };

    return Convert.FromBase64String(padded);
}

record LoginRequest(string Username, string Password);
record TokenRequest(string Token);
record OAuthTokenRequest(string Code);
record JwtValidationResult(bool IsValid, string? Username);

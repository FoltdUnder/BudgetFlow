using BudgetFlow.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace BudgetFlow.Api.Common;

public sealed class RefreshTokenCookieManager
{
    public const string CookieName = "refreshToken";

    private readonly JwtOptions _jwtOptions;

    public RefreshTokenCookieManager(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public void AppendRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append(CookieName, refreshToken, CreateCookieOptions(httpContext.Request.IsHttps));
    }

    public void DeleteRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(CookieName, CreateCookieOptions(httpContext.Request.IsHttps));
    }

    public bool TryGetRefreshToken(HttpRequest request, out string refreshToken)
    {
        if (request.Cookies.TryGetValue(CookieName, out var cookieValue)
            && !string.IsNullOrWhiteSpace(cookieValue))
        {
            refreshToken = cookieValue;
            return true;
        }

        refreshToken = string.Empty;
        return false;
    }

    private CookieOptions CreateCookieOptions(bool isHttps)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = isHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };
    }
}

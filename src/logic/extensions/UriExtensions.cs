namespace core.extensions;

using System.Web;

public static class UriExtensions
{
    private static string ExternalLinkAnchorTagFormat = $"<a href=\"{{1}}\" target=\"_blank\">{{0}}</a>";

    public static string ToEncodedUrlStringOrNull(this Uri uri)
    {
        if (uri == null)
        {
            return null;
        }

        return uri.AbsoluteUri;
    }

    public static bool IsHttpsProtocol(this Uri uri) => uri.Scheme == Uri.UriSchemeHttps;

    public static bool IsHttpProtocol(this Uri uri) => uri.Scheme == Uri.UriSchemeHttp;

    public static bool IsGitProtocol(this Uri uri) => uri.Scheme == "git";

    public static bool IsDomainWithHttpsSupport(this Uri uri) =>
        IsGitHubUri(uri) ||
        IsGitHubPagerUri(uri) ||
        IsInvocativeUri(uri) ||
        IsVeinGalleryUri(uri);

    public static bool IsGitHubUri(this Uri uri) =>
        string.Equals(uri.Host, "www.github.com", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsGitHubPagerUri(this Uri uri) =>
        uri.Authority.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase) ||
        uri.Authority.EndsWith(".github.io", StringComparison.OrdinalIgnoreCase);

    private static bool IsInvocativeUri(this Uri uri) => uri.IsInDomain("invocative.studio");

    private static bool IsVeinGalleryUri(this Uri uri) =>
        uri.IsInDomain("vein.gallery") ||
        uri.IsInDomain("dev.vein.gallery");

    private static bool IsInDomain(this Uri uri, string domain) =>
        uri.Authority.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Authority, domain, StringComparison.OrdinalIgnoreCase);

    public static Uri ToHttps(this Uri uri)
    {
        var uriBuilder = new UriBuilder(uri);
        uriBuilder.Scheme = Uri.UriSchemeHttps;
        uriBuilder.Port = -1;

        return uriBuilder.Uri;
    }

    public static string AppendQueryStringToRelativeUri(string relativeUrl, IReadOnlyCollection<KeyValuePair<string, string>> queryStringCollection)
    {
        var tempUri = new Uri("http://vein.gallery/");
        var builder = new UriBuilder(new Uri(tempUri, relativeUrl));
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (var pair in queryStringCollection)
        {
            query[pair.Key] = pair.Value;
        }

        builder.Query = query.ToString();
        return builder.Uri.PathAndQuery;
    }

    public static Uri AppendPathToUri(this Uri uri, string pathToAppend, string queryString = null)
    {
        var builder = new UriBuilder(uri);
        builder.Path = builder.Path.TrimEnd('/') + "/" + pathToAppend.TrimStart('/');
        if (!string.IsNullOrEmpty(queryString))
        {
            builder.Query = queryString;
        }
        return builder.Uri;
    }

    public static string GetExternalUrlAnchorTag(string data, string link) => string.Format(ExternalLinkAnchorTagFormat, data, link);
}

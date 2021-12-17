namespace core.services;

using core.extensions;
using System.Text.RegularExpressions;

public class ImageDomainValidator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMinutes(1);
    private static readonly Regex GithubBadgeUrlRegEx = new Regex("^(https|http):\\/\\/github\\.com\\/[^/]+\\/[^/]+(\\/actions)?\\/workflows\\/.*badge\\.svg", RegexOptions.IgnoreCase, RegexTimeout);
    
    public bool TryPrepareImageUrlForRendering(string uriString, out string readyUriString)
    {
        if (uriString == null)
        {
            throw new ArgumentNullException(nameof(uriString));
        }

        Uri returnUri = null;
        readyUriString = null;

        if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            if (uri.IsHttpProtocol() && IsTrustedImageDomain(uri))
                returnUri = uri.ToHttps();
            else if (uri.IsHttpsProtocol() && IsTrustedImageDomain(uri))
                returnUri = uri;
        }

        if (returnUri != null)
        {
            readyUriString = returnUri.AbsoluteUri;
            return true;
        }

        return false;
    }

    private bool IsTrustedImageDomain(Uri uri) =>
        TrustedImageDomains.Contains(uri.Host) ||
        IsGitHubBadge(uri);


    private static List<string> TrustedImageDomains = new List<string>()
    {
        "api.bintray.com",
        "api.codacy.com",
        "app.codacy.com",
        "api.codeclimate.com",
        "api.dependabot.com",
        "api.travis-ci.com",
        "api.travis-ci.org",
        "app.fossa.io",
        "app.fossa.com",
        "badge.fury.io",
        "badgen.net",
        "badges.gitter.im",
        "bettercodehub.com",
        "buildstats.info",
        "cdn.jsdelivr.net",
        "cdn.syncfusion.com",
        "ci.appveyor.com",
        "circleci.com",
        "codecov.io",
        "codefactor.io",
        "coveralls.io",
        "dev.azure.com",
        "gitlab.com",
        "img.shields.io",
        "i.imgur.com",
        "isitmaintained.com",
        "opencollective.com",
        "snyk.io",
        "sonarcloud.io",
        "raw.github.com",
        "raw.githubusercontent.com",
        "user-images.githubusercontent.com",
        "camo.githubusercontent.com"
    };

    private bool IsGitHubBadge(Uri uri)
    {
        try
        {
            return GithubBadgeUrlRegEx.IsMatch(uri.OriginalString);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}

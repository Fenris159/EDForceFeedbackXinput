using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Journals
{
    /// <summary>
    /// Checks the running version against the latest GitHub release.
    /// Uses the release tag (e.g. v1.0.0) from the GitHub API – zip names do not need to match.
    /// </summary>
    public static class VersionChecker
    {
        private const string ReleasesLatestUrl = "https://api.github.com/repos/Fenris159/EDForceFeedbackXinput/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/Fenris159/EDForceFeedbackXinput/releases/latest";

        /// <summary>
        /// Returns true if a newer version is available on GitHub. Returns false if current is up to date,
        /// or if the check fails (network error, rate limit, etc.) – caller should proceed normally.
        /// </summary>
        public static async Task<(bool IsOutdated, string LatestVersion, string ReleaseUrl)> CheckForUpdateAsync(string currentVersion)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "EDForceFeedback-Updater");
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var response = await client.GetAsync(ReleasesLatestUrl).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        return (false, null, ReleasesPageUrl);

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var tag = ParseTagName(json);
                    if (string.IsNullOrEmpty(tag))
                        return (false, null, ReleasesPageUrl);

                    var latest = NormalizeVersion(tag);
                    var current = NormalizeVersion(currentVersion);
                    var isOutdated = CompareVersions(current, latest) < 0;

                    return (isOutdated, latest ?? tag, ReleasesPageUrl);
                }
            }
            catch
            {
                return (false, null, ReleasesPageUrl);
            }
        }

        private static string ParseTagName(string json)
        {
            const string key = "\"tag_name\":\"";
            var start = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;
            start += key.Length;
            var end = json.IndexOf('"', start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }

        private static string NormalizeVersion(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0.0.0";
            v = v.TrimStart('v', 'V');
            var dash = v.IndexOf('-');
            if (dash >= 0) v = v.Substring(0, dash);
            return v;
        }

        private static int CompareVersions(string a, string b)
        {
            var pa = ParseVersion(a);
            var pb = ParseVersion(b);
            for (int i = 0; i < 3; i++)
            {
                var diff = pa[i] - pb[i];
                if (diff != 0) return Math.Sign(diff);
            }
            return 0;
        }

        private static int[] ParseVersion(string v)
        {
            var parts = (v ?? "0.0.0").Split('.');
            return new[]
            {
                parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0,
                parts.Length > 1 && int.TryParse(parts[1], out var i) ? i : 0,
                parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : 0
            };
        }
    }
}

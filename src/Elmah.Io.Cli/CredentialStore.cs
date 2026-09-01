using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elmah.Io.Cli
{
    static class CredentialStore
    {
        private static readonly string CredentialsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".elmahio");

        private static readonly string CredentialsFile =
            Path.Combine(CredentialsDir, "credentials.json");

        public const string ApiKeyEnvironmentVariable = "ELMAHIO_API_KEY";

        public static string? GetApiKey(string? provided = null)
        {
            if (!string.IsNullOrWhiteSpace(provided)) return provided;

            var fromEnvironment = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;

            try
            {
                if (!File.Exists(CredentialsFile)) return null;
                var json = File.ReadAllText(CredentialsFile);
                var obj = JObject.Parse(json);
                var stored = obj["apiKey"]?.ToString();
                return string.IsNullOrWhiteSpace(stored) ? null : stored;
            }
            catch
            {
                return null;
            }
        }

        public static void SaveApiKey(string apiKey)
        {
            Directory.CreateDirectory(CredentialsDir);
            var json = JsonConvert.SerializeObject(new { apiKey }, Formatting.Indented);
            File.WriteAllText(CredentialsFile, json);

            // Restrict file permissions to owner-only on Unix
            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(CredentialsFile,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                catch
                {
                    // Best-effort; non-fatal if filesystem doesn't support it
                }
            }
        }

        public static void Clear()
        {
            if (File.Exists(CredentialsFile))
                File.Delete(CredentialsFile);
        }

        public static string CredentialsPath => CredentialsFile;
    }
}

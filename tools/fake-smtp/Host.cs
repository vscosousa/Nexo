namespace Nexo.FakeSmtp;

// A named entry point (not top-level statements) so the type name never collides with the API's Program in tests.
internal static class Host
{
    private static Task Main(string[] args) => FakeSmtpApp.Build(args).RunAsync();
}

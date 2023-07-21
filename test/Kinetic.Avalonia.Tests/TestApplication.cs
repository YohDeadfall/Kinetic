using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;
using Kinetic.Data.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]

namespace Kinetic.Data.Tests;

public sealed class TestApplication : Application
{
    public TestApplication() => Styles.Add(new SimpleTheme());

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
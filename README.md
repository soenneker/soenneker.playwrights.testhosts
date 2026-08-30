[![](https://img.shields.io/nuget/v/soenneker.playwrights.testhosts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.playwrights.testhosts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.playwrights.testhosts/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.playwrights.testhosts/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.playwrights.testhosts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.playwrights.testhosts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.playwrights.testhosts/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.playwrights.testhosts/actions/workflows/codeql.yml)

# Soenneker.Playwrights.TestHosts

A reusable test fixture that locates an application project, starts it on an available loopback port, and provides Playwright browser sessions to the suite.

## Installation

```bash
dotnet add package Soenneker.Playwrights.TestHosts
```

## Define a host

Derive from `PlaywrightHostedTestHost` and identify the solution and application project. The project path must be relative to, and remain inside, the discovered solution directory.

```csharp
using System.IO;
using Soenneker.Playwrights.TestEnvironment.Options;
using Soenneker.Playwrights.TestHosts;

public sealed class AppPlaywrightHost : PlaywrightHostedTestHost
{
    protected override PlaywrightTestHostOptions CreateOptions()
    {
        return new PlaywrightTestHostOptions
        {
            SolutionFileName = "MyApp.slnx",
            ProjectRelativePath = Path.Combine("src", "MyApp", "MyApp.csproj"),
            ApplicationName = "MyApp",
            Restore = false,
            Build = true,
            BuildConfiguration = "Debug",
            ReuseBrowserContextAcrossSessions = false,
            ReusePageAcrossSessions = false
        };
    }
}
```

Override `ConfigureServices` when the test fixture needs additional dependencies:

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<TestData>();
}
```

## Use it in tests

Share the host for the test session, then create and dispose a browser session in each test:

```csharp
using Microsoft.Playwright;
using Soenneker.Playwrights.Session;

[ClassDataSource<AppPlaywrightHost>(Shared = SharedType.PerTestSession)]
public sealed class HomePageTests
{
    private readonly AppPlaywrightHost _host;

    public HomePageTests(AppPlaywrightHost host)
    {
        _host = host;
    }

    [Test]
    public async ValueTask Home_page_loads()
    {
        await using BrowserSession session = await _host.CreateSession();

        await session.Page.GotoAsync(_host.BaseUrl);
        await Assertions.Expect(session.Page.GetByRole(AriaRole.Heading))
                        .ToBeVisibleAsync();
    }
}
```

`BaseUrl` and `CreateSession` are available after the shared host initializes. Disposing the host closes Playwright and terminates the application process. Shared context/page behavior can be set in `CreateOptions` or overridden per `CreateSession` call with `PlaywrightSessionOptions`.

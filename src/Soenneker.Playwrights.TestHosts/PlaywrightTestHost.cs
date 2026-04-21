using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.Playwrights.Session;
using Soenneker.Playwrights.TestEnvironment.Abstract;
using Soenneker.Playwrights.TestEnvironment.Options;
using Soenneker.Playwrights.TestEnvironment.Registrars;
using Soenneker.TestHosts.Unit;
using Soenneker.Utils.File.Abstract;
using Soenneker.Utils.Test;

namespace Soenneker.Playwrights.TestHosts;

/// <summary>
/// A test fixture for testing with Playwright
/// </summary>
public class PlaywrightTestHost : UnitTestHost
{
    private string? _projectPath;

    private IPlaywrightTestEnvironment? _environment;
    private IFileUtil? _fileUtil;

    public string BaseUrl =>
        _environment?.BaseUrl ?? throw new InvalidOperationException("Fixture has not been initialized.");

    public override async Task InitializeAsync()
    {
        PlaywrightFixtureOptions options = CreateOptions();

        SetupIoC(Services, options);
        ConfigureServices(Services);

        await base.InitializeAsync().NoSync();

        _environment = ServicesProvider!.GetRequiredService<IPlaywrightTestEnvironment>();
        _fileUtil = ServicesProvider!.GetRequiredService<IFileUtil>();

        _projectPath = await ResolveProjectPath(options, CancellationToken.None).NoSync();

        await _environment.Initialize(_projectPath, CancellationToken.None).NoSync();
    }

    public ValueTask<BrowserSession> CreateSession(PlaywrightSessionOptions? sessionOptions = null, CancellationToken cancellationToken = default)
    {
        if (_environment is null)
            throw new InvalidOperationException("Fixture has not been initialized.");

        return _environment.CreateSession(sessionOptions, cancellationToken);
    }

    protected virtual PlaywrightFixtureOptions CreateOptions()
    {
        return new PlaywrightFixtureOptions
        {
            SolutionFileName = "Soenneker.Bradix.Suite.slnx",
            ProjectRelativePath = Path.Combine("test", "Soenneker.Bradix.Suite.Demo", "Soenneker.Bradix.Suite.Demo.csproj"),
            ApplicationName = "Bradix demo"
        };
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public static IServiceCollection SetupIoC(IServiceCollection services, PlaywrightFixtureOptions options)
    {
        IConfiguration configuration = TestUtil.BuildConfig();
        services.AddSingleton(configuration);
        services.AddSingleton(options);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();

            builder.AddSerilog(dispose: true);
        });

        services.AddPlaywrightTestEnvironmentAsSingleton();

        return services;
    }

    private async ValueTask<string> ResolveProjectPath(PlaywrightFixtureOptions options, CancellationToken cancellationToken)
    {
        string solutionRoot = await FindSolutionRoot(options.SolutionFileName, cancellationToken).NoSync();

        string projectPath = Path.Combine(solutionRoot, options.ProjectRelativePath);

        if (!await _fileUtil!.Exists(projectPath, cancellationToken).NoSync())
            throw new FileNotFoundException($"Could not locate the '{options.ApplicationName}' project.", projectPath);

        return projectPath;
    }

    private async ValueTask<string> FindSolutionRoot(string solutionFileName, CancellationToken cancellationToken)
    {
        string[] startingPoints =
        [
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        ];

        foreach (string startingPoint in startingPoints)
        {
            DirectoryInfo? current = new(startingPoint);

            while (current is not null)
            {
                string candidate = Path.Combine(current.FullName, solutionFileName);

                if (await _fileUtil!.Exists(candidate, cancellationToken).NoSync())
                    return current.FullName;

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException($"Could not locate the solution root containing '{solutionFileName}'.");
    }

    public override async ValueTask DisposeAsync()
    {
        if (_environment != null)
            await _environment.DisposeAsync().NoSync();

        await base.DisposeAsync().NoSync();
    }
}
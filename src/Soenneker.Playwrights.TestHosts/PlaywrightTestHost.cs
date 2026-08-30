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
/// Starts an application project and exposes Playwright browser sessions to a test suite.
/// </summary>
public class PlaywrightTestHost : UnitTestHost
{
    private string? _projectPath;

    private IPlaywrightTestEnvironment? _environment;
    private IFileUtil? _fileUtil;

    /// <summary>
    /// Gets the application base URL after the host has initialized.
    /// </summary>
    public string BaseUrl =>
        _environment?.BaseUrl ?? throw new InvalidOperationException("Fixture has not been initialized.");

    /// <summary>
    /// Builds the service provider, resolves the configured project, and starts the Playwright test environment.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task InitializeAsync()
    {
        PlaywrightTestHostOptions options = CreateOptions();

        SetupIoC(Services, options);
        ConfigureServices(Services);

        await base.InitializeAsync().NoSync();

        _environment = ServicesProvider!.GetRequiredService<IPlaywrightTestEnvironment>();
        _fileUtil = ServicesProvider!.GetRequiredService<IFileUtil>();

        try
        {
            _projectPath = await ResolveProjectPath(options, CancellationToken.None).NoSync();
            await _environment.Initialize(_projectPath, CancellationToken.None).NoSync();
        }
        catch
        {
            await _environment.DisposeAsync().NoSync();
            throw;
        }
    }

    /// <summary>
    /// Creates a browser session using the host defaults or the supplied reuse overrides.
    /// </summary>
    /// <param name="sessionOptions">Options controlling browser creation and the test session lifetime.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task whose result is the requested browser session.</returns>
    public ValueTask<BrowserSession> CreateSession(PlaywrightSessionOptions? sessionOptions = null, CancellationToken cancellationToken = default)
    {
        if (_environment is null)
            throw new InvalidOperationException("Fixture has not been initialized.");

        return _environment.CreateSession(sessionOptions, cancellationToken);
    }

    /// <summary>
    /// Creates the solution, application project, build, and session-reuse options for this host.
    /// </summary>
    /// <returns>The options used to locate and start the application.</returns>
    protected virtual PlaywrightTestHostOptions CreateOptions()
    {
        throw new InvalidOperationException($"{GetType().Name} must override {nameof(CreateOptions)} and identify its application project.");
    }

    /// <summary>
    /// Adds test-specific services before the host service provider is built.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Registers the services required by the application.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <param name="options">Options to configure for the Playwright Test Host.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection SetupIoC(IServiceCollection services, PlaywrightTestHostOptions options)
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

    private async ValueTask<string> ResolveProjectPath(PlaywrightTestHostOptions options, CancellationToken cancellationToken)
    {
        string solutionRoot = await FindSolutionRoot(options.SolutionFileName, cancellationToken).NoSync();

        string projectPath = Path.GetFullPath(Path.Combine(solutionRoot, options.ProjectRelativePath));
        string solutionRootPrefix = Path.EndsInDirectorySeparator(solutionRoot) ? solutionRoot : solutionRoot + Path.DirectorySeparatorChar;

        if (!projectPath.StartsWith(solutionRootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The project path must remain inside the solution root '{solutionRoot}'.");

        if (!await _fileUtil!.Exists(projectPath, cancellationToken).NoSync())
            throw new FileNotFoundException($"Could not locate the '{options.ApplicationName}' project.", projectPath);

        return projectPath;
    }

    private async ValueTask<string> FindSolutionRoot(string solutionFileName, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetFileName(solutionFileName), solutionFileName, StringComparison.Ordinal))
            throw new InvalidOperationException("SolutionFileName must be a file name, not a path.");

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
        try
        {
            if (_environment != null)
                await _environment.DisposeAsync().NoSync();
        }
        finally
        {
            await base.DisposeAsync().NoSync();
        }
    }
}

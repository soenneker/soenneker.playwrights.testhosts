[![](https://img.shields.io/nuget/v/soenneker.playwrights.testhosts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.playwrights.testhosts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.playwrights.testhosts/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.playwrights.testhosts/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.playwrights.testhosts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.playwrights.testhosts/)

# Soenneker.Playwrights.TestHosts

Represents the playwright hosted test host.

## Install

```bash
dotnet add package Soenneker.Playwrights.TestHosts
```

## What you get

- `PlaywrightHostedTestHost` — Represents the playwright hosted test host.
- `PlaywrightTestHost` — A test fixture for testing with Playwright.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `PlaywrightTestHost.BaseUrl` | Gets or sets base url. | Gets or sets base url. |
| `PlaywrightTestHost.InitializeAsync()` | Initializes async. | A task that represents the asynchronous operation. |
| `PlaywrightTestHost.SetupIoC(services, options)` | Registers the services required by the application. | The same service collection, so additional registrations can be chained. |
| `PlaywrightTestHost.DisposeAsync()` | Asynchronously releases resources used by the current instance. | A task that represents the asynchronous operation. |

## Practical notes

- Dispose instances you own when their scope ends so held resources can be released.

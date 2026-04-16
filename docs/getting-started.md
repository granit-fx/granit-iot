# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (for integration tests)
- Access to the [Granit GitHub Packages](https://github.com/orgs/granit-fx/packages) NuGet feed

## NuGet Authentication

Configure your local NuGet credentials for the Granit feed:

```bash
dotnet nuget update source granit-registry \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text \
  --configfile nuget.config
```

## Build

```bash
dotnet build Granit.IoT.slnx
```

## Test

```bash
dotnet test Granit.IoT.slnx
```

## Project Structure

See [architecture.md](architecture.md) for the package ring design and
key design decisions.

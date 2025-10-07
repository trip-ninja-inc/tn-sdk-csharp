# Trip Ninja .NET SDK

The Trip Ninja .NET SDK is a library for interacting with the Trip Ninja API using C#.
It provides an intuitive interface to prepare and process API requests.

> **Target Framework:** .NET 9.0 or greater

---

## Installation

The Trip Ninja SDK is available via [NuGet](https://www.nuget.org/).

Install via the .NET CLI:

```bash
dotnet add package TN.SDK
```

Or via the NuGet Package Manager Console:

```powershell
Install-Package TN.SDK
```

---

## Quick Example

```csharp
using Tn.SDK;

var tnClient = new TnApi("access_key", "refresh_token");
string requestData = "{}"; // JSON string representing request data

string compressedData = tnClient.PrepareDataForGenerateSolutions(requestData);

// The method compresses the request data and returns the compressed result
```

---

## Development

### Setup for Local Development

1. Clone the repository and navigate to the project directory.
2. Open the solution in your IDE (e.g., Visual Studio or Rider).
3. To build and test locally:

```bash
dotnet build
dotnet test
```

### Using the SDK Locally (Project Reference)

If you're contributing or testing changes locally in another project, reference the SDK project directly:

```xml
<ProjectReference Include="../TN.SDK/TN.SDK.csproj" />
```

This enables live updates as you modify the source

---

### Linting

Linting is done using the [Roslyn Analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)


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

## Quick Example

```csharp
using Tn.SDK;

var tnClient = new TnApi("access_key", "refresh_token");
string requestData = "{}"; // JSON string representing request data

string compressedData = tnClient.PrepareDataForGenerateSolutions(requestData);

// The method compresses the request data and returns the compressed result
```

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

### Linting and Formatting

Linting is done using the [Roslyn Analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)

Formatting is done using ```dotnet format```


### Setup Visual Studio Code
- Install [Visual Studio Code](https://code.visualstudio.com/)
- Install [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- In the walkthrough, select Set up your environment and select Install .NET SDK. This will open a window next to the walkthrough with a button to install the latest version of the .NET SDK. Select the Install button, which will trigger a download and an install of the .NET SDK. Follow the on-screen instructions to complete this process.
- Open the project folder
- Run the following commands to build and test the project:
```bash
dotnet build
dotnet test
```

[Full Documentation](https://code.visualstudio.com/docs/csharp/get-started)

### Setup Visual Studio
- Install [Visual Studio](https://visualstudio.microsoft.com/) (Community/Pro/Enterprise)
- Include the **“.NET desktop development”** workload
- Open the `.sln` file
- Build the solution (`Ctrl + Shift + B`)
- Use Test Explorer to run tests

[Full Documentation](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=vs-2022)

---



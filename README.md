# Trip Ninja .NET SDK

The Trip Ninja .NET SDK is a library for interacting with the Trip Ninja API using C#.
It provides an intuitive interface to prepare and process API requests.

> **Target Framework:** .NET 9.0 or greater

---

## Installation

The Trip Ninja SDK is available via [NuGet](https://www.nuget.org/packages/TripNinja.SDK).

Install via the .NET CLI:

```bash
dotnet add package TripNinja.SDK
```

Or via the NuGet Package Manager Console:

```powershell
NuGet\Install-Package TripNinja.SDK
```

## Quick Example

```csharp
using TN.SDK.Core;
using System.Text.Json;

// Client ID and Client Secret can be retrieved from the Admin Panel
var tnClient = new TnApi(
    "client_id", // Can be set via TN_SDK_CLIENT_ID env variable
    "client_secret" // Can be set via TN_SDK_CLIENT_SECRET env variable
);

var requestData = new
{
    trip_id = "",
    datasource_responses = new
    {
        _4b69b995e699534c1c644381b760c990795efff9 = new[]
        {
            new
            {
                pricing_solution_id = "e494c1f1d172380259a61b05990d61cbb68b35a9",
                total_price = 392.22,
                segment_source = "travelport",
                is_private_fare = false,
                refundable = false,
                segments = new[]
                {
                    new[]
                    {
                        new
                        {
                            departure_time = "2025-11-15T16:40:00.000-04:00",
                            departure_timestamp = 1763239200,
                            arrival_time = "2025-11-15T17:35:00.000-05:00",
                            arrival_timestamp = 1763246100,
                            flight_number = "667",
                            operating_carrier = "AC",
                            transportation_type = "flight",
                            fare_type = "PublicFare",
                            cabin_class = "E",
                            from_iata = "YHZ",
                            to_iata = "YUL"
                        },
                        new
                        {
                            departure_time = "2025-11-15T18:20:00.000-05:00",
                            departure_timestamp = 1763248800,
                            arrival_time = "2025-11-15T20:50:00.000-08:00",
                            arrival_timestamp = 1763268600,
                            flight_number = "311",
                            operating_carrier = "AC",
                            transportation_type = "flight",
                            fare_type = "PublicFare",
                            cabin_class = "E",
                            from_iata = "YUL",
                            to_iata = "YVR"
                        }
                    }
                },
                baggage = (object?)null
            }
        }
    }
};

// Serialise to JSON
string json = JsonSerializer.Serialize(requestData);

string compressedData = tnClient.PrepareDataForGenerateSolutions(json);

// The method compresses the request data and returns the compressed result
```

### Response
`compresssed data`
```

eJzFkU1v3CAQhv/LnNkIsI0NtypK1UMOVdVUalcrNGDsIuEPAY5Urfa/V+y2STdK02NvMMw8eufhCDn6VfseFACBHjOmZYvW6ejSuszJJVBH0LUR0kjZOCFlU9WWWVHXVcdMK6iVkrayccMwSFD7I6zRWz+POi1hy36ZL3hXy9qygfWs5VVHeSNRMEMbKWkvmDVGdKZqUAKBvGQMumAcqEryG84JJDdObs76kg8U5IiPLqxLzEDAp9L/iNnpAaMDNWBIjkB0wzb3aMJz6Rcogdrvj9C7FWPeotPZTwXLKW92jO1Y85kJVVNF6Q2ldEdrRWlxdDWQMk4rKNaKileSU0oAYwkSXuO1qmqeeOUE1+1/0mrBCm0Ifvye9bxNxkVQIEQLBJbVRcxFsi3z55d3t0VdxDkVJ3g2n3+sJcMFAgSKm9/Fj5sJ3r4vtghYNH7WNmBKoOCutMZl0h4zgoKvH76dv+Xp/nAPJ/IPeZ3i9OWyf5dXd93b8jhVzTOve1ue6MRr8irG/oO8h/treV8+welwIGBwHHF0oOYthNPhdPoJwVsdOA==
```

### Unsuccessful Response
```
error CS1503: Argument 1: cannot convert from 'any_type' to 'string'
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



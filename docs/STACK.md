# Project Stack

- Language: C# with nullable reference types enabled.
- Runtime/SDK target: .NET 10.0.
- Web framework: ASP.NET Core with OData controllers.
- Data providers: Entity Framework Core, Cosmos DB SDK, MongoDB, LiteDB, and in-memory repositories.
- API documentation: OpenAPI, Swashbuckle, and NSwag integration projects.
- Serialization: System.Text.Json with Datasync-specific converters.
- Tests: xUnit v3 with AwesomeAssertions and NSubstitute.
- Build shape: `Datasync.Toolkit.sln`, central package versions in `Directory.Packages.props`, shared project settings in `src/Directory.Build.props` and `tests/Directory.Build.props`.

## Considerations

- Samples require platform-specific SDKs and should be validated through the documented GitHub Actions sample workflow instead of local workload installation.
- Unit tests live under `tests/CommunityToolkit.Datasync.*.Test`.
- Generated outputs under `bin/` and `obj/` are ignored and should not be edited.

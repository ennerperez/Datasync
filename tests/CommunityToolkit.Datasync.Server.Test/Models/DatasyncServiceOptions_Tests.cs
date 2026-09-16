// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.Test.Json;

[ExcludeFromCodeCoverage]
public class DatasyncServiceOptions_Tests
{
    [Fact]
    public void JsonSerializerOptions_Works()
    {
        JsonSerializerOptions options = new DatasyncServiceOptions().JsonSerializerOptions;
        options.Should().NotBeNull();
        options.Converters.Should().NotBeNullOrEmpty();
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void TableDataProperties_Defaults_Work()
    {
        DatasyncServiceOptions options = new();

        options.TableDataProperties.IdPropertyName.Should().Be("Id");
        options.TableDataProperties.UpdatedAtPropertyName.Should().Be("UpdatedAt");
        options.TableDataProperties.VersionPropertyName.Should().Be("Version");
        options.TableDataProperties.DeletedPropertyName.Should().Be("Deleted");
    }

    [Fact]
    public void TableDataProperties_CustomMap_Works()
    {
        DatasyncServiceOptions options = new();

        options.TableDataProperties.Map(id: "Key", updatedAt: "ChangedOn", version: "Token", deleted: "Removed");

        options.TableDataProperties.IdPropertyName.Should().Be("Key");
        options.TableDataProperties.UpdatedAtPropertyName.Should().Be("ChangedOn");
        options.TableDataProperties.VersionPropertyName.Should().Be("Token");
        options.TableDataProperties.DeletedPropertyName.Should().Be("Removed");
    }
}
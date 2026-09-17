// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client.Serialization;

namespace CommunityToolkit.Datasync.Client.Test.Serialization;

[ExcludeFromCodeCoverage]
public class EntityMetadataPropertyMap_Tests
{
    [Fact]
    public void GetEntityMetadata_UsesCustomPropertyNames()
    {
        EntityMetadataPropertyMap map = new();
        map.Map(
            id: nameof(CustomMetadataEntity.Uid),
            updatedAt: nameof(CustomMetadataEntity.ModifiedOn),
            version: nameof(CustomMetadataEntity.ETag),
            deleted: nameof(CustomMetadataEntity.IsRemoved));
        CustomMetadataEntity entity = new()
        {
            Uid = "movie-1",
            ModifiedOn = DateTimeOffset.UtcNow,
            ETag = "version-1",
            IsRemoved = true
        };

        EntityMetadata metadata = map.GetAccessor<CustomMetadataEntity>().GetEntityMetadata(entity);

        metadata.Id.Should().Be(entity.Uid);
        metadata.UpdatedAt.Should().Be(entity.ModifiedOn);
        metadata.Version.Should().Be(entity.ETag);
        metadata.Deleted.Should().BeTrue();
    }

    private class CustomMetadataEntity
    {
        public string Uid { get; set; }

        public DateTimeOffset? ModifiedOn { get; set; }

        public string ETag { get; set; }

        public bool IsRemoved { get; set; }
    }
}

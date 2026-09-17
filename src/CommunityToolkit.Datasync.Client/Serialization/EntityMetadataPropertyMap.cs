// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Datasync.Client.Serialization;

/// <summary>
/// Configures the CLR property names used for Datasync entity metadata.
/// </summary>
public sealed class EntityMetadataPropertyMap
{
    private readonly ConcurrentDictionary<Type, EntityMetadataAccessor> accessors = [];
    private static readonly Regex EntityIdentity = new("^[a-zA-Z0-9][a-zA-Z0-9_.|:-]{0,126}$", RegexOptions.Compiled);
    private string idPropertyName = nameof(EntityMetadata.Id);
    private string deletedPropertyName = nameof(EntityMetadata.Deleted);
    private string updatedAtPropertyName = nameof(EntityMetadata.UpdatedAt);
    private string versionPropertyName = nameof(EntityMetadata.Version);

    /// <summary>
    /// The CLR property name used for the entity identifier.
    /// </summary>
    public string IdPropertyName
    {
        get => this.idPropertyName;
        set => SetPropertyName(ref this.idPropertyName, value);
    }

    /// <summary>
    /// The CLR property name used for the soft-delete flag.
    /// </summary>
    public string DeletedPropertyName
    {
        get => this.deletedPropertyName;
        set => SetPropertyName(ref this.deletedPropertyName, value);
    }

    /// <summary>
    /// The CLR property name used for the last-updated timestamp.
    /// </summary>
    public string UpdatedAtPropertyName
    {
        get => this.updatedAtPropertyName;
        set => SetPropertyName(ref this.updatedAtPropertyName, value);
    }

    /// <summary>
    /// The CLR property name used for the concurrency token.
    /// </summary>
    public string VersionPropertyName
    {
        get => this.versionPropertyName;
        set => SetPropertyName(ref this.versionPropertyName, value);
    }

    /// <summary>
    /// Updates all system property names.
    /// </summary>
    /// <param name="id">The CLR property name used for the entity identifier.</param>
    /// <param name="updatedAt">The CLR property name used for the last-updated timestamp.</param>
    /// <param name="version">The CLR property name used for the concurrency token.</param>
    /// <param name="deleted">The CLR property name used for the soft-delete flag.</param>
    /// <returns>The current instance for chained configuration.</returns>
    public EntityMetadataPropertyMap Map(string id = nameof(EntityMetadata.Id), string updatedAt = nameof(EntityMetadata.UpdatedAt), string version = nameof(EntityMetadata.Version), string deleted = nameof(EntityMetadata.Deleted))
    {
        IdPropertyName = id;
        UpdatedAtPropertyName = updatedAt;
        VersionPropertyName = version;
        DeletedPropertyName = deleted;
        return this;
    }

    /// <summary>
    /// Gets a typed metadata accessor for the provided entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A typed metadata accessor.</returns>
    public EntityMetadataAccessor<TEntity> GetAccessor<TEntity>() where TEntity : class
        => (EntityMetadataAccessor<TEntity>)this.accessors.GetOrAdd(typeof(TEntity), _ => new EntityMetadataAccessor<TEntity>(this));

    /// <summary>
    /// Gets a metadata accessor for the provided entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>A metadata accessor.</returns>
    public EntityMetadataAccessor GetAccessor(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return this.accessors.GetOrAdd(entityType, type => (EntityMetadataAccessor)Activator.CreateInstance(typeof(EntityMetadataAccessor<>).MakeGenericType(type), this)!);
    }

    /// <summary>
    /// Returns true if the provided value is a valid entity ID.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="allowNull">If <c>true</c>, null is a valid value for the entity ID.</param>
    /// <returns><c>true</c> if the entity ID is valid; <c>false</c> otherwise.</returns>
    internal static bool EntityIdIsValid(string? value, bool allowNull = false)
    {
        if (value is null && allowNull)
        {
            return true;
        }

        return value is not null && EntityIdentity.IsMatch(value);
    }

    private void SetPropertyName(ref string field, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        field = value;
        this.accessors.Clear();
    }
}

/// <summary>
/// Provides reflection-based access to Datasync entity metadata properties.
/// </summary>
public abstract class EntityMetadataAccessor
{
    /// <summary>
    /// Creates a new metadata accessor.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="propertyMap">The property map to use.</param>
    protected EntityMetadataAccessor(Type entityType, EntityMetadataPropertyMap propertyMap)
    {
        EntityType = entityType;
        IdPropertyInfo = GetProperty(entityType, propertyMap.IdPropertyName, true, typeof(string))!;
        DeletedPropertyInfo = GetProperty(entityType, propertyMap.DeletedPropertyName, false, typeof(bool));
        UpdatedAtPropertyInfo = GetProperty(entityType, propertyMap.UpdatedAtPropertyName, false, typeof(DateTimeOffset));
        VersionPropertyInfo = GetProperty(entityType, propertyMap.VersionPropertyName, false, typeof(string), typeof(byte[]));
    }

    /// <summary>
    /// The entity type handled by this accessor.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The property used for the entity identifier.
    /// </summary>
    public PropertyInfo IdPropertyInfo { get; }

    /// <summary>
    /// The property used for the soft-delete flag.
    /// </summary>
    public PropertyInfo? DeletedPropertyInfo { get; }

    /// <summary>
    /// The property used for the last-updated timestamp.
    /// </summary>
    public PropertyInfo? UpdatedAtPropertyInfo { get; }

    /// <summary>
    /// The property used for the concurrency token.
    /// </summary>
    public PropertyInfo? VersionPropertyInfo { get; }

    /// <summary>
    /// Retrieves the datasync metadata for the given entity.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The metadata for the entity.</returns>
    public EntityMetadata GetEntityMetadata(object entity)
    {
        EntityMetadata metadata = new() { Id = (string?)IdPropertyInfo.GetValue(entity) };

        if (UpdatedAtPropertyInfo is not null)
        {
            metadata.UpdatedAt = (DateTimeOffset?)UpdatedAtPropertyInfo.GetValue(entity);
        }

        if (DeletedPropertyInfo is not null)
        {
            metadata.Deleted = (bool)(DeletedPropertyInfo.GetValue(entity) ?? false);
        }

        if (VersionPropertyInfo is not null)
        {
            if (VersionPropertyInfo.PropertyType == typeof(string))
            {
                metadata.Version = (string?)VersionPropertyInfo.GetValue(entity);
            }

            if (VersionPropertyInfo.PropertyType == typeof(byte[]))
            {
                byte[]? bVersion = (byte[]?)VersionPropertyInfo.GetValue(entity);
                if (bVersion != null)
                {
                    metadata.Version = Convert.ToBase64String(bVersion);
                }
            }
        }

        return metadata;
    }

    private static PropertyInfo? GetProperty(Type entityType, string name, bool required, params Type[] validTypes)
    {
        PropertyInfo? property = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(p => p.CanRead && p.CanWrite && p.Name.Equals(name, StringComparison.Ordinal));

        if (property is null)
        {
            if (required)
            {
                throw new DatasyncException($"Entity type '{entityType.Name}' does not have a '{name}' property.");
            }

            return null;
        }

        if (!HasType(property.PropertyType, validTypes))
        {
            string validTypeNames = string.Join(" or ", validTypes.Select(t => t == typeof(byte[]) ? "byte array" : $"'{t.Name}'"));
            throw new DatasyncException($"Property type '{entityType.Name}'.{name} must be {validTypeNames}.");
        }

        return property;
    }

    private static bool HasType(Type type, Type[] validTypes)
    {
        foreach (Type validType in validTypes)
        {
            if (type == validType)
            {
                return true;
            }

            if (validType.IsValueType && type == typeof(Nullable<>).MakeGenericType(validType))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Provides typed access to Datasync entity metadata properties.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class EntityMetadataAccessor<TEntity> : EntityMetadataAccessor where TEntity : class
{
    /// <summary>
    /// Creates a typed metadata accessor.
    /// </summary>
    /// <param name="propertyMap">The property map to use.</param>
    public EntityMetadataAccessor(EntityMetadataPropertyMap propertyMap) : base(typeof(TEntity), propertyMap)
    {
    }

    /// <summary>
    /// Retrieves the datasync metadata for the given entity.
    /// </summary>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The metadata for the entity.</returns>
    public EntityMetadata GetEntityMetadata(TEntity entity)
        => GetEntityMetadata((object)entity);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CommunityToolkit.Datasync.Server;

/// <summary>
/// Configures the CLR property names used for Datasync system metadata.
/// </summary>
public sealed class TableDataPropertyMap
{
    private readonly ConcurrentDictionary<Type, TableDataAccessor> accessors = [];
    private string idPropertyName = nameof(ITableData.Id);
    private string deletedPropertyName = nameof(ITableData.Deleted);
    private string updatedAtPropertyName = nameof(ITableData.UpdatedAt);
    private string versionPropertyName = nameof(ITableData.Version);

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
    public TableDataPropertyMap Map(string id = nameof(ITableData.Id), string updatedAt = nameof(ITableData.UpdatedAt), string version = nameof(ITableData.Version), string deleted = nameof(ITableData.Deleted))
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
    public TableDataAccessor<TEntity> GetAccessor<TEntity>() where TEntity : class
        => (TableDataAccessor<TEntity>)this.accessors.GetOrAdd(typeof(TEntity), _ => new TableDataAccessor<TEntity>(this));

    /// <summary>
    /// Gets a metadata accessor for the provided entity type.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>A metadata accessor.</returns>
    public TableDataAccessor GetAccessor(Type entityType)
        => this.accessors.GetOrAdd(entityType, type => (TableDataAccessor)Activator.CreateInstance(typeof(TableDataAccessor<>).MakeGenericType(type), this)!);

    private void SetPropertyName(ref string field, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        field = value;
        this.accessors.Clear();
    }
}

/// <summary>
/// Provides reflection-based access to Datasync system metadata properties.
/// </summary>
public abstract class TableDataAccessor
{
    /// <summary>
    /// Creates a new metadata accessor.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="propertyMap">The property map to use.</param>
    protected TableDataAccessor(Type entityType, TableDataPropertyMap propertyMap)
    {
        EntityType = entityType;
        IdProperty = GetRequiredProperty(entityType, propertyMap.IdPropertyName, typeof(string));
        DeletedProperty = GetRequiredProperty(entityType, propertyMap.DeletedPropertyName, typeof(bool));
        UpdatedAtProperty = GetRequiredProperty(entityType, propertyMap.UpdatedAtPropertyName, typeof(DateTimeOffset), typeof(DateTimeOffset?));
        VersionProperty = GetRequiredProperty(entityType, propertyMap.VersionPropertyName, typeof(byte[]));
    }

    /// <summary>
    /// The entity type handled by this accessor.
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The property used for the entity identifier.
    /// </summary>
    public PropertyInfo IdProperty { get; }

    /// <summary>
    /// The property used for the soft-delete flag.
    /// </summary>
    public PropertyInfo DeletedProperty { get; }

    /// <summary>
    /// The property used for the last-updated timestamp.
    /// </summary>
    public PropertyInfo UpdatedAtProperty { get; }

    /// <summary>
    /// The property used for the concurrency token.
    /// </summary>
    public PropertyInfo VersionProperty { get; }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity identifier.</returns>
    public string? GetId(object entity)
        => (string?)IdProperty.GetValue(entity);

    /// <summary>
    /// Sets the entity identifier.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="id">The entity identifier.</param>
    public void SetId(object entity, string id)
        => IdProperty.SetValue(entity, id);

    /// <summary>
    /// Gets the soft-delete flag.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns><c>true</c> when the entity is soft-deleted.</returns>
    public bool GetDeleted(object entity)
        => (bool)(DeletedProperty.GetValue(entity) ?? false);

    /// <summary>
    /// Sets the soft-delete flag.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="deleted">The soft-delete flag.</param>
    public void SetDeleted(object entity, bool deleted)
        => DeletedProperty.SetValue(entity, deleted);

    /// <summary>
    /// Gets the last-updated timestamp.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The last-updated timestamp.</returns>
    public DateTimeOffset? GetUpdatedAt(object entity)
    {
        object? value = UpdatedAtProperty.GetValue(entity);
        return value is DateTimeOffset date ? date : null;
    }

    /// <summary>
    /// Sets the last-updated timestamp.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="updatedAt">The last-updated timestamp.</param>
    public void SetUpdatedAt(object entity, DateTimeOffset? updatedAt)
    {
        object? value = UpdatedAtProperty.PropertyType == typeof(DateTimeOffset) ? updatedAt.GetValueOrDefault() : updatedAt;
        UpdatedAtProperty.SetValue(entity, value);
    }

    /// <summary>
    /// Gets the concurrency token.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The concurrency token.</returns>
    public byte[] GetVersion(object entity)
        => (byte[]?)VersionProperty.GetValue(entity) ?? [];

    /// <summary>
    /// Sets the concurrency token.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="version">The concurrency token.</param>
    public void SetVersion(object entity, byte[] version)
        => VersionProperty.SetValue(entity, version);

    private static PropertyInfo GetRequiredProperty(Type entityType, string name, params Type[] allowedTypes)
    {
        PropertyInfo property = entityType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            ?? throw new InvalidOperationException($"Entity type '{entityType.Name}' does not have a public '{name}' property.");

        if (!property.CanRead || !property.CanWrite)
        {
            throw new InvalidOperationException($"Entity property '{entityType.Name}.{name}' must be readable and writable.");
        }

        if (!allowedTypes.Contains(property.PropertyType))
        {
            string validTypes = string.Join(", ", allowedTypes.Select(t => t.Name));
            throw new InvalidOperationException($"Entity property '{entityType.Name}.{name}' must be one of: {validTypes}.");
        }

        return property;
    }
}

/// <summary>
/// Provides typed access to Datasync system metadata properties.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class TableDataAccessor<TEntity> : TableDataAccessor where TEntity : class
{
    /// <summary>
    /// Creates a typed metadata accessor.
    /// </summary>
    /// <param name="propertyMap">The property map to use.</param>
    public TableDataAccessor(TableDataPropertyMap propertyMap) : base(typeof(TEntity), propertyMap)
    {
        IdExpression = CreatePropertyExpression<string>(IdProperty);
        DeletedExpression = CreatePropertyExpression<bool>(DeletedProperty);
        UpdatedAtExpression = CreateNullableDateTimeOffsetExpression(UpdatedAtProperty);
    }

    /// <summary>
    /// An expression that reads the entity identifier.
    /// </summary>
    public Expression<Func<TEntity, string>> IdExpression { get; }

    /// <summary>
    /// An expression that reads the soft-delete flag.
    /// </summary>
    public Expression<Func<TEntity, bool>> DeletedExpression { get; }

    /// <summary>
    /// An expression that reads the last-updated timestamp.
    /// </summary>
    public Expression<Func<TEntity, DateTimeOffset?>> UpdatedAtExpression { get; }

    /// <summary>
    /// Creates an expression that matches the entity identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>An expression that matches the entity identifier.</returns>
    public Expression<Func<TEntity, bool>> CreateIdEqualsExpression(string id)
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        MemberExpression property = Expression.Property(entity, IdProperty);
        BinaryExpression body = Expression.Equal(property, Expression.Constant(id));
        return Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }

    /// <summary>
    /// Creates an expression that matches non-deleted entities.
    /// </summary>
    /// <returns>An expression that matches non-deleted entities.</returns>
    public Expression<Func<TEntity, bool>> CreateNotDeletedExpression()
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        MemberExpression property = Expression.Property(entity, DeletedProperty);
        UnaryExpression body = Expression.Not(property);
        return Expression.Lambda<Func<TEntity, bool>>(body, entity);
    }

    private static Expression<Func<TEntity, TProperty>> CreatePropertyExpression<TProperty>(PropertyInfo property)
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        MemberExpression body = Expression.Property(entity, property);
        return Expression.Lambda<Func<TEntity, TProperty>>(body, entity);
    }

    private static Expression<Func<TEntity, DateTimeOffset?>> CreateNullableDateTimeOffsetExpression(PropertyInfo property)
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression body = Expression.Property(entity, property);
        if (body.Type == typeof(DateTimeOffset))
        {
            body = Expression.Convert(body, typeof(DateTimeOffset?));
        }

        return Expression.Lambda<Func<TEntity, DateTimeOffset?>>(body, entity);
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server.Abstractions.Http;
using LiteDB;

namespace CommunityToolkit.Datasync.Server.LiteDb;

/// <summary>
/// A repository implementation that stored data in a LiteDB database.
/// </summary>
/// <typeparam name="TEntity">The entity type to store in the database.</typeparam>
public class LiteDbRepository<TEntity> : IRepository<TEntity> where TEntity : LiteDbTableData
{
    private readonly LiteDatabase connection;
    private readonly TableDataAccessor<TEntity> tableData;

    // On a web server (like this is normally used in), we expect the LiteDatabase to be
    // a singleton and we want to ensure that writes to the database are serialized.  We
    // use an async semaphore to ensure that only one thread is writing to the database
    // at a time.
    private readonly static SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// Creates a new <see cref="LiteDbRepository{TEntity}"/> using the provided database connection.
    /// </summary>
    /// <remarks>
    /// The collection name is based on the entity type.
    /// </remarks>
    /// <param name="dbConnection">The <see cref="LiteDatabase"/> connection to use for storing entities.</param>
    /// <param name="tableDataProperties"></param>
    public LiteDbRepository(LiteDatabase dbConnection, TableDataPropertyMap? tableDataProperties = null) : this(dbConnection, typeof(TEntity).Name.ToLowerInvariant() + "s", tableDataProperties)
    {
    }

    /// <summary>
    /// Creates a new <see cref="LiteDbRepository{TEntity}"/> using the provided database connection
    /// and collection name.
    /// </summary>
    /// <param name="dbConnection">The <see cref="LiteDatabase"/> connection to use for storing entities.</param>
    /// <param name="collectionName">The name of the collection to use for storing the entities.</param>
    /// <param name="tableDataProperties"></param>
    public LiteDbRepository(LiteDatabase dbConnection, string collectionName, TableDataPropertyMap? tableDataProperties = null)
    {
        this.connection = dbConnection;
        this.tableData = (tableDataProperties ?? new TableDataPropertyMap()).GetAccessor<TEntity>();
        Collection = this.connection.GetCollection<TEntity>(collectionName);
        _ = Collection.EnsureIndex(this.tableData.UpdatedAtExpression);
    }

    /// <summary>
    /// The collection within the LiteDb database that stores the entities.
    /// </summary>
    public virtual ILiteCollection<TEntity> Collection { get; }

    /// <summary>
    /// The mechanism by which an Id is generated when one is not provided.
    /// </summary>
    public Func<TEntity, string> IdGenerator { get; set; } = _ => Guid.NewGuid().ToString("N");

    /// <summary>
    /// The mechanism by which a new version byte array is generated.
    /// </summary>
    public Func<byte[]> VersionGenerator { get; set; } = () => Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Updates the system properties for the provided entity on write.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    protected void UpdateEntity(TEntity entity)
    {
        this.tableData.SetUpdatedAt(entity, DateTimeOffset.UtcNow);
        this.tableData.SetVersion(entity, VersionGenerator.Invoke());
    }

    /// <summary>
    /// Executes the provided action within a lock on the collection.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A task that completes when the action is finished.</returns>
    protected async ValueTask ExecuteOnLockedCollectionAsync(Action action, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            action.Invoke();
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    /// <summary>
    /// Checks that the provided ID is valid.
    /// </summary>
    /// <param name="id">The ID of the entity to check.</param>
    /// <exception cref="HttpException">Thrown if the ID is not valid.</exception>
    protected static void CheckIdIsValid(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new HttpException(HttpStatusCodes.Status400BadRequest);
        }
    }

    /// <inheritdoc/>
    public virtual ValueTask<IQueryable<TEntity>> AsQueryableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Collection.FindAll().AsQueryable());

    /// <inheritdoc/>
    public virtual async ValueTask CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        string? entityId = this.tableData.GetId(entity);
        if (string.IsNullOrEmpty(entityId))
        {
            entityId = IdGenerator.Invoke(entity);
            this.tableData.SetId(entity, entityId);
        }

        await ExecuteOnLockedCollectionAsync(() =>
        {
            TEntity existingEntity = Collection.FindOne(this.tableData.CreateIdEqualsExpression(entityId));
            if (existingEntity != null)
            {
                throw new HttpException(HttpStatusCodes.Status409Conflict) { Payload = existingEntity };
            }

            UpdateEntity(entity);
            _ = Collection.Insert(entity);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DeleteAsync(string id, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        CheckIdIsValid(id);

        await ExecuteOnLockedCollectionAsync(() =>
        {
            TEntity storedEntity = Collection.FindOne(this.tableData.CreateIdEqualsExpression(id)) ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
            if (version?.Length > 0 && !this.tableData.GetVersion(storedEntity).SequenceEqual(version))
            {
                throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = storedEntity };
            }

            _ = Collection.Delete(id);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask<TEntity> ReadAsync(string id, CancellationToken cancellationToken = default)
    {
        CheckIdIsValid(id);

        TEntity entity = Collection.FindOne(this.tableData.CreateIdEqualsExpression(id)) ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
        return ValueTask.FromResult(entity);
    }

    /// <inheritdoc/>
    public virtual async ValueTask ReplaceAsync(TEntity entity, byte[]? version = null, CancellationToken cancellationToken = default)
    {
        string? entityId = this.tableData.GetId(entity);
        CheckIdIsValid(entityId!);

        await ExecuteOnLockedCollectionAsync(() =>
        {
            TEntity storedEntity = Collection.FindOne(this.tableData.CreateIdEqualsExpression(entityId!)) ?? throw new HttpException(HttpStatusCodes.Status404NotFound);
            if (version?.Length > 0 && !this.tableData.GetVersion(storedEntity).SequenceEqual(version))
            {
                throw new HttpException(HttpStatusCodes.Status412PreconditionFailed) { Payload = storedEntity };
            }

            UpdateEntity(entity);
            _ = Collection.Update(entity);
        }, cancellationToken);
    }
}
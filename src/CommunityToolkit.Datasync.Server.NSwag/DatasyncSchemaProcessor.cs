// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Server;
using NJsonSchema;
using NJsonSchema.Generation;
using System.Text.Json;

namespace CommunityToolkit.Datasync.Server.NSwag;

/// <summary>
/// NSwag Schema processor for the Community Datasync Toolkit.
/// </summary>
public class DatasyncSchemaProcessor : ISchemaProcessor
{
    /// <summary>
    /// List of the system properties within the <see cref="ITableData"/> interface.
    /// </summary>
    private readonly string[] systemProperties;

    /// <summary>
    /// Creates a new <see cref="DatasyncSchemaProcessor"/>.
    /// </summary>
    /// <param name="tableDataProperties">The CLR property map used for Datasync system metadata.</param>
    public DatasyncSchemaProcessor(TableDataPropertyMap? tableDataProperties = null)
    {
        tableDataProperties ??= new TableDataPropertyMap();
        JsonNamingPolicy namingPolicy = JsonNamingPolicy.CamelCase;
        this.systemProperties =
        [
            namingPolicy.ConvertName(tableDataProperties.DeletedPropertyName),
            namingPolicy.ConvertName(tableDataProperties.UpdatedAtPropertyName),
            namingPolicy.ConvertName(tableDataProperties.VersionPropertyName)
        ];
    }

    /// <summary>
    /// Processes each schema in turn, doing required modifications.
    /// </summary>
    /// <param name="context">The schema processor context.</param>
    public void Process(SchemaProcessorContext context)
    {
        if (context.ContextualType.Type.GetInterfaces().Contains(typeof(ITableData)))
        {
            foreach (KeyValuePair<string, JsonSchemaProperty> prop in context.Schema.Properties)
            {
                if (this.systemProperties.Contains(prop.Key))
                {
                    prop.Value.IsReadOnly = true;
                }
            }
        }
    }
}
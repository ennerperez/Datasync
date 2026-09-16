# Swashbuckle support

Follow the [basic instructions for Swashbuckle integration](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-10.0&tabs=visual-studio), then modify as follows:

1. Add packages to your project to support Swashbuckle.  The following packages are required:

    * [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore).
    * [CommunityToolkit.Datasync.Server.Swashbuckle](https://www.nuget.org/packages/CommunityToolkit.Datasync.Server.Swashbuckle).

2. Add a service to generate an OpenAPI definition to your `Program.cs` file:

        builder.Services.AddSwaggerGen(options => 
        {
            options.AddDatasyncControllers();
        });

    If you configured custom Datasync metadata property names, pass a `TableDataPropertyMap` with the same property names to the document filter so generated schemas use the same JSON property names:

        builder.Services.AddSwaggerGen(options =>
        {
            TableDataPropertyMap tableDataProperties = new TableDataPropertyMap()
                .Map(id: "Key", updatedAt: "ChangedOn", version: "Token", deleted: "Removed");

            options.AddDatasyncControllers(tableDataProperties);
        });

    !!! tip
        `AddDatasyncControllers()` uses the calling assembly when it searches for table controllers.  If your table controllers are in a different project, register `DatasyncDocumentFilter` directly and pass the controller assembly.

3. Enable the middleware for serving the generated JSON document and the Swagger UI, also in `Program.cs`:

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => 
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = string.Empty;
            });
        }

With this configuration, browsing to the root of the web service allows you to browse the API.  The OpenAPI definition can then be imported into other services (such as Azure API Management).  For more information on configuring Swashbuckle, see [Get started with Swashbuckle and ASP.NET Core](https://learn.microsoft.com/aspnet/core/tutorials/getting-started-with-swashbuckle).

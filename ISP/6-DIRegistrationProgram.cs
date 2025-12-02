// Register the concrete repository
builder.Services.AddScoped<ProductRepository>();

// Register as all the interfaces it implements
builder.Services.AddScoped<IReadRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

builder.Services.AddScoped<IQueryRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

builder.Services.AddScoped<IWriteRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

builder.Services.AddScoped<IBulkWriteRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

builder.Services.AddScoped<ISoftDeleteRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

builder.Services.AddScoped<ISpecificationRepository<Product>>(sp =>
    sp.GetRequiredService<ProductRepository>()
);

// Alternative: Use a factory method
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<IReadRepository<Product>, ProductRepository>();
builder.Services.AddScoped<IQueryRepository<Product>, ProductRepository>();
builder.Services.AddScoped<IWriteRepository<Product>, ProductRepository>();
builder.Services.AddScoped<IBulkWriteRepository<Product>, ProductRepository>();
builder.Services.AddScoped<ISoftDeleteRepository<Product>, ProductRepository>();

// Register services
builder.Services.AddScoped<ProductReportService>();
builder.Services.AddScoped<ProductSearchService>();
builder.Services.AddScoped<ProductManagementService>();
builder.Services.AddScoped<ProductImportService>();
builder.Services.AddScoped<ProductArchiveService>();

using LibraryManagement.Application.Mappings;
using LibraryManagement.Application.Services;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file (development only)
// and make sure the configuration system knows about them
if (builder.Environment.IsDevelopment())
{
    // search upward from current directory for a .env file (solution root may be above)
    string? envFile = null;
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (File.Exists(candidate))
        {
            envFile = candidate;
            break;
        }
        dir = dir.Parent;
    }

    if (envFile != null)
    {
        Log.Information("Loading environment variables from {EnvFile}", envFile);
        foreach (var line in File.ReadAllLines(envFile))
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
            }
        }
        // refresh configuration
        builder.Configuration.AddEnvironmentVariables();
    }
    else
    {
        Log.Warning(".env file not found in current or parent directories");
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LibraryAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Library Management API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });
});

// Add DbContext - use PostgreSQL if DATABASE_URL exists (production), otherwise SQLite (local)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    builder.Services.AddDbContext<LibraryContext>(options =>
        options.UseNpgsql(databaseUrl));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<LibraryContext>(options =>
        options.UseSqlite(connectionString));
}

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

// Register services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileService, FileService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
    options.AddPolicy("AllowCredentials", builder =>
    {
        builder.WithOrigins("https://librarymanagement-lejd.onrender.com", "http://localhost:5000", "http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add HTTP Context Accessor for getting current user
builder.Services.AddHttpContextAccessor();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Immediately verify that the connection string works
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetService<LibraryContext>();
        if (db != null)
        {
            Log.Information("Testing database connectivity...");
            if (!await db.Database.CanConnectAsync())
            {
                Log.Error("Unable to connect to the database. Check your connection string.");
            }
            else
            {
                Log.Information("Database connection succeeded.");
            }
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Database validation failed at startup");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowCredentials");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
// Redirect unauthenticated requests for index.html to landing page
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
    {
        if (!(context.User?.Identity?.IsAuthenticated ?? false))
        {
            context.Response.Redirect("/landing.html");
            return;
        }
    }
    await next();
});

app.UseStaticFiles();

// Add root redirect to landing.html
app.MapGet("/", async context =>
{
    context.Response.Redirect("/landing.html");
});

// Ensure database is created
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetService<LibraryContext>();
        if (dbContext != null)
        {
            var isPostgres = dbContext.Database.IsNpgsql();
            if (isPostgres)
            {
                Log.Information("Using PostgreSQL database, applying migrations...");
                dbContext.Database.Migrate();
                Log.Information("PostgreSQL migrations applied successfully");
            }
            else
            {
                Log.Information("Ensuring SQLite database created...");
                var dbPath = Path.GetDirectoryName(dbContext.Database.GetConnectionString()?.Replace("Data Source=", ""));
                if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
                {
                    Directory.CreateDirectory(dbPath);
                }
                dbContext.Database.EnsureCreated();
                Log.Information("SQLite database created successfully");
            }
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Database creation failed: {Message}", ex.Message);
}

try
{
    Log.Information("Starting Library Management API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
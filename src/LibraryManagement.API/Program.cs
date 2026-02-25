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
builder.Services.AddControllers();
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

// Add DbContext with SQLite or PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// sometimes we accidentally end up with the environment variable name
// prepended to the value (see Npgsql exception in logs). strip if needed.
if (!string.IsNullOrEmpty(connectionString) &&
    connectionString.StartsWith("ConnectionStrings__", StringComparison.OrdinalIgnoreCase))
{
    var eq = connectionString.IndexOf('=');
    if (eq >= 0 && eq < connectionString.Length - 1)
        connectionString = connectionString.Substring(eq + 1);
}

// attempt to resolve host to IPv4 only if DNS returns addresses
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        var npgcs = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(npgcs.Host))
        {
            var addresses = System.Net.Dns.GetHostAddresses(npgcs.Host);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ipv4 != null)
            {
                npgcs.Host = ipv4.ToString();
                connectionString = npgcs.ConnectionString;
                Log.Information("Resolved host to IPv4 {Ip}", ipv4);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to resolve host to IPv4, using original hostname");
    }
}

// log the connection string (masked) so we can debug mis‑parsing
if (!string.IsNullOrEmpty(connectionString))
{
    var safe = connectionString.Length > 60
        ? connectionString.Substring(0, 60) + "..."
        : connectionString;
    Log.Information("Using connection string: {Conn}", safe);
}

// simple heuristic: postgres strings contain Host= or Username=
if (connectionString?.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0 ||
    connectionString?.IndexOf("Username=", StringComparison.OrdinalIgnoreCase) >= 0)
{
    builder.Services.AddDbContext<LibraryContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
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
});

// Add HTTP Context Accessor for getting current user
builder.Services.AddHttpContextAccessor();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LibraryContext>();

var app = builder.Build();

// Immediately verify that the connection string works; this catches
// mis‑configured env vars (like the "ConnectionStrings__…=" prefix) before
// any request is processed.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        Log.Information("Testing database connectivity...");
        if (!await db.Database.CanConnectAsync())
        {
            Log.Error("Unable to connect to the database. Check your connection string.");
            // Optionally: throw or exit so the container restarts
        }
        else
        {
            Log.Information("Database connection succeeded.");
        }
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Database validation failed at startup");
    throw; // allow crash so the problem is visible immediately
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
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
        var dbContext = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        Log.Information("Attempting database connection...");
        
        if (dbContext.Database.IsNpgsql())
        {
            Log.Information("Using PostgreSQL, attempting migration...");
            dbContext.Database.Migrate();
            Log.Information("Database migration completed successfully");
        }
        else
        {
            Log.Information("Using SQLite, ensuring database created...");
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
catch (Exception ex)
{
    Log.Error(ex, "Database connection/migration failed: {Message}", ex.Message);
    Log.Warning("Application will continue without database migration");
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
using System.Text;
using gamepricer.Configuration;
using gamepricer.Data;
using gamepricer.Entities;
using gamepricer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:3000",
                "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GamePricer API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT token'ý buraya yapýþtýr.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<ItadOptions>(builder.Configuration.GetSection(ItadOptions.SectionName));
builder.Services.AddHttpClient<IItadApiClient, ItadApiClient>();
builder.Services.AddScoped<GameLiveDataService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Platforms.Any())
    {
        var steam = new Platform { Id = Guid.NewGuid(), Name = "Steam", WebsiteUrl = "https://store.steampowered.com" };
        var epic = new Platform { Id = Guid.NewGuid(), Name = "Epic Games", WebsiteUrl = "https://store.epicgames.com" };

        var rpg = new Category { Id = Guid.NewGuid(), Name = "RPG" };
        var action = new Category { Id = Guid.NewGuid(), Name = "Action" };

        var game1 = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Elden Ring",
            Slug = "elden-ring",
            Description = "Açýk dünya aksiyon rol yapma oyunu.",
            Developer = "FromSoftware",
            Publisher = "Bandai Namco",
            CoverImageUrl = "https://example.com/eldenring.jpg",
            CreatedAt = DateTime.UtcNow
        };

        var game2 = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Cyberpunk 2077",
            Slug = "cyberpunk-2077",
            Description = "Gelecek temalý açýk dünya RPG oyunu.",
            Developer = "CD Projekt Red",
            Publisher = "CD Projekt",
            CoverImageUrl = "https://example.com/cyberpunk.jpg",
            CreatedAt = DateTime.UtcNow
        };

        db.Platforms.AddRange(steam, epic);
        db.Categories.AddRange(rpg, action);
        db.Games.AddRange(game1, game2);

        db.GameCategories.AddRange(
            new GameCategory { GameId = game1.Id, CategoryId = rpg.Id },
            new GameCategory { GameId = game1.Id, CategoryId = action.Id },
            new GameCategory { GameId = game2.Id, CategoryId = rpg.Id },
            new GameCategory { GameId = game2.Id, CategoryId = action.Id }
        );

        db.GamePrices.AddRange(
            new GamePrice
            {
                Id = Guid.NewGuid(),
                GameId = game1.Id,
                PlatformId = steam.Id,
                Price = 1299,
                Currency = "TRY",
                DiscountRate = 0,
                ProductUrl = "https://store.steampowered.com",
                LastCheckedAt = DateTime.UtcNow,
                IsAvailable = true
            },
            new GamePrice
            {
                Id = Guid.NewGuid(),
                GameId = game1.Id,
                PlatformId = epic.Id,
                Price = 1199,
                Currency = "TRY",
                DiscountRate = 10,
                ProductUrl = "https://store.epicgames.com",
                LastCheckedAt = DateTime.UtcNow,
                IsAvailable = true
            },
            new GamePrice
            {
                Id = Guid.NewGuid(),
                GameId = game2.Id,
                PlatformId = steam.Id,
                Price = 899,
                Currency = "TRY",
                DiscountRate = 15,
                ProductUrl = "https://store.steampowered.com",
                LastCheckedAt = DateTime.UtcNow,
                IsAvailable = true
            }
        );

        db.SaveChanges();
    }

    CatalogSeed.EnsureExtendedCatalog(db);
}

// Swagger'ın her ortamda (Production dahil) çalışması için:
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GamePricer API V1");
    c.RoutePrefix = "swagger"; // Sitenin sonuna /swagger yazınca açılması için
});

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
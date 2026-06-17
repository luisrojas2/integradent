using Microsoft.EntityFrameworkCore;
using IntegraDentWallet.Data;
using IntegraDentWallet.Services;

if (args.Length > 0 && args[0] == "setup-wallet")
{
    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    await IntegraDentWallet.Services.GoogleWalletSetup.CrearClaseDeLealtadAsync(
        config["GoogleWallet:IssuerId"]!,
        config["GoogleWallet:ClassId"]!,
        config["GoogleWallet:RutaCredenciales"]!,
        "https://i.postimg.cc/gjybgnDy/Chat-GPT-Image-Jun-17-2026-03-57-08-PM.png");

    return;
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WalletDb")
    ?? throw new InvalidOperationException(
        "Falta configurar ConnectionStrings:WalletDb con dotnet user-secrets.");

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<GoogleWalletService>();
builder.Services.AddScoped<PuntosService>();
builder.Services.AddScoped<PromocionesService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "IntegraDent Wallet API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.MapControllers();

app.MapGet("/", () => "IntegraDent Wallet API esta corriendo.");

app.Run();
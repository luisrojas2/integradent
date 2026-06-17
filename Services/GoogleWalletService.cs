using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Google.Apis.Auth.OAuth2;
using Microsoft.IdentityModel.Tokens;
using IntegraDentWallet.Data;
using IntegraDentWallet.Models;
using Microsoft.EntityFrameworkCore;

namespace IntegraDentWallet.Services;

public class GoogleWalletService
{
    private readonly WalletDbContext _db;
    private readonly IConfiguration _config;
    private readonly string _issuerId;
    private readonly string _classId;
    private readonly string _origenPermitido;
    private readonly string _rutaCredenciales;

    public GoogleWalletService(WalletDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _issuerId = config["GoogleWallet:IssuerId"]!;
        _classId = config["GoogleWallet:ClassId"]!;
        _origenPermitido = config["GoogleWallet:OrigenPermitido"]!;
        _rutaCredenciales = config["GoogleWallet:RutaCredenciales"]!;
    }

    private object ConstruirObjetoDePase(Paciente paciente)
    {
        var proximaCita = paciente.Citas
            .Where(c => !c.Completada && c.FechaHora > DateTime.UtcNow)
            .OrderBy(c => c.FechaHora)
            .FirstOrDefault();

        var promoActiva = _db.Promociones
            .Where(p => p.Activa && p.FechaFin > DateTime.UtcNow)
            .Where(p => p.ParaTodos || p.PacientePromociones.Any(pp => pp.PacienteId == paciente.Id))
            .OrderByDescending(p => p.FechaInicio)
            .FirstOrDefault();

        return new
        {
            id = $"{_issuerId}.paciente-{paciente.Id}",
            classId = _classId,
            state = "ACTIVE",
            accountId = paciente.Id.ToString(),
            accountName = paciente.Nombre,
            loyaltyPoints = new
            {
                label = "Puntos",
                balance = new { @string = paciente.Puntos.ToString() }
            },
            textModulesData = new[]
            {
                new
                {
                    id = "proxima_cita",
                    header = "Proxima cita",
                    body = proximaCita != null
                        ? proximaCita.FechaHora.ToString("dd MMM yyyy, hh:mm tt")
                        : "Sin cita programada"
                },
                new
                {
                    id = "promo",
                    header = "Promocion activa",
                    body = promoActiva != null
                        ? $"{promoActiva.Titulo} - {promoActiva.Descripcion}"
                        : "Sin promociones por el momento"
                }
            },
            barcode = new
            {
                type = "QR_CODE",
                value = $"PACIENTE-{paciente.Id}"
            }
        };
    }

    public async Task CrearObjetoDePacienteAsync(Paciente paciente)
    {
        var accessToken = await ObtenerAccessTokenAsync();
        var objeto = ConstruirObjetoDePase(paciente);

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(JsonSerializer.Serialize(objeto), Encoding.UTF8, "application/json");
        var respuesta = await http.PostAsync(
            "https://walletobjects.googleapis.com/walletobjects/v1/loyaltyObject", content);

        if (!respuesta.IsSuccessStatusCode && (int)respuesta.StatusCode != 409)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            throw new Exception($"Error creando objeto de pase: {cuerpo}");
        }

        paciente.TienePaseGoogle = true;
        await _db.SaveChangesAsync();
    }

    public async Task<string> GenerarLinkDeWalletAsync(Paciente paciente)
    {
        if (!paciente.TienePaseGoogle)
        {
            await CrearObjetoDePacienteAsync(paciente);
        }

        var credencialesJson = await File.ReadAllTextAsync(_rutaCredenciales);
        using var doc = JsonDocument.Parse(credencialesJson);
        var clientEmail = doc.RootElement.GetProperty("client_email").GetString();
        var privateKeyPem = doc.RootElement.GetProperty("private_key").GetString();

        var objetoId = $"{_issuerId}.paciente-{paciente.Id}";

        var payloadInterno = new
        {
            loyaltyObjects = new[] { new { id = objetoId } }
        };

        var payload = new Dictionary<string, object>
        {
            ["iss"] = clientEmail!,
            ["aud"] = "google",
            ["typ"] = "savetowallet",
            ["origins"] = new[] { _origenPermitido },
            ["payload"] = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(payloadInterno))!
        };

        var token = FirmarJwtRS256(payload, privateKeyPem!);

        return $"https://pay.google.com/gp/v/save/{token}";
    }

    public async Task ActualizarPaseAsync(int pacienteId)
    {
        var paciente = await _db.Pacientes
            .Include(p => p.Citas)
            .FirstOrDefaultAsync(p => p.Id == pacienteId);

        if (paciente == null || !paciente.TienePaseGoogle)
        {
            return;
        }

        var accessToken = await ObtenerAccessTokenAsync();
        var objeto = ConstruirObjetoDePase(paciente);
        var objetoId = $"{_issuerId}.paciente-{paciente.Id}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"https://walletobjects.googleapis.com/walletobjects/v1/loyaltyObject/{objetoId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(objeto), Encoding.UTF8, "application/json")
        };

        var respuesta = await http.SendAsync(request);
        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            throw new Exception($"Error actualizando pase: {cuerpo}");
        }
    }

    private async Task<string> ObtenerAccessTokenAsync()
    {
        var credencialesJson = _config["GoogleWallet:CredencialesJson"]
    ?? await File.ReadAllTextAsync(_rutaCredenciales);

        GoogleCredential credential;
        if (!string.IsNullOrEmpty(credencialesJson))
        {
            credential = GoogleCredential.FromJson(credencialesJson)
                .CreateScoped("https://www.googleapis.com/auth/wallet_object.issuer");
        }
        else
        {
            using var stream = new FileStream(_rutaCredenciales, FileMode.Open, FileAccess.Read);
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped("https://www.googleapis.com/auth/wallet_object.issuer");
        }

        return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
    }

    private string FirmarJwtRS256(Dictionary<string, object> payload, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());

        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var header = new JwtHeader(credentials);
        var claims = new JwtPayload();
        foreach (var kvp in payload)
        {
            claims.Add(kvp.Key, kvp.Value);
        }

        var token = new JwtSecurityToken(header, claims);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
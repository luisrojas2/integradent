using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace IntegraDentWallet.Services;

public static class GoogleWalletSetup
{
    public static async Task CrearClaseDeLealtadAsync(string issuerId, string classId, string rutaCredenciales, string urlLogoCuadrado)
    {
        using var stream = new FileStream(rutaCredenciales, FileMode.Open, FileAccess.Read);
        var credential = GoogleCredential
            .FromStream(stream)
            .CreateScoped("https://www.googleapis.com/auth/wallet_object.issuer");

        var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        var clase = new
        {
            id = classId,
            issuerName = "IntegraDent",
            programName = "Programa de fidelidad IntegraDent",
            programLogo = new { sourceUri = new { uri = urlLogoCuadrado } },
            hexBackgroundColor = "#0f6e56",
            countryCode = "GT",
            reviewStatus = "UNDER_REVIEW",
            textModulesData = new[]
            {
                new { id = "proxima_cita", header = "Proxima cita", body = "Sin cita programada" },
                new { id = "promo", header = "Promocion activa", body = "Sin promociones por el momento" }
            },
            linksModuleData = new
            {
                uris = new[]
                {
                    new { uri = "https://wa.me/50258404748", description = "Escribir por WhatsApp", id = "whatsapp" },
                    new { uri = "https://www.instagram.com/integradent.gt", description = "Instagram", id = "instagram" }
                }
            }
        };

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(JsonSerializer.Serialize(clase), Encoding.UTF8, "application/json");
        var respuesta = await http.PostAsync(
            "https://walletobjects.googleapis.com/walletobjects/v1/loyaltyClass", content);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Console.WriteLine(respuesta.IsSuccessStatusCode
            ? $"Clase creada correctamente:\n{cuerpo}"
            : $"Error ({(int)respuesta.StatusCode}):\n{cuerpo}");
    }
}
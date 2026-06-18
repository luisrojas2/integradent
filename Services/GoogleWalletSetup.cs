using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace IntegraDentWallet.Services;

public static class GoogleWalletSetup
{
    public static async Task CrearClaseDeLealtadAsync(
        string issuerId, string classId, string rutaCredenciales,
        string urlLogoCuadrado, string? urlHeroImage = null)
    {
        using var stream = new FileStream(rutaCredenciales, FileMode.Open, FileAccess.Read);
        var credential = GoogleCredential
            .FromStream(stream)
            .CreateScoped("https://www.googleapis.com/auth/wallet_object.issuer");

        var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

        var claseDict = new Dictionary<string, object?>
        {
            ["id"] = classId,
            ["issuerName"] = "IntegraDent",
            ["programName"] = "Programa de fidelidad IntegraDent",
            ["programLogo"] = new { sourceUri = new { uri = urlLogoCuadrado } },
            ["hexBackgroundColor"] = "#1d9e75",
            ["countryCode"] = "GT",
            ["reviewStatus"] = "UNDER_REVIEW",
            ["linksModuleData"] = new
            {
                uris = new[]
                {
                    new { uri = "https://wa.me/50258404748", description = "Escribir por WhatsApp", id = "whatsapp" },
                    new { uri = "https://www.instagram.com/integradent.gt", description = "Instagram", id = "instagram" }
                }
            }
        };

        if (!string.IsNullOrEmpty(urlHeroImage))
        {
            claseDict["heroImage"] = new { sourceUri = new { uri = urlHeroImage } };
        }

        var clase = claseDict;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent(JsonSerializer.Serialize(clase), Encoding.UTF8, "application/json");
        var respuesta = await http.PostAsync(
            "https://walletobjects.googleapis.com/walletobjects/v1/loyaltyClass", content);

        if (respuesta.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var contentPatch = new StringContent(JsonSerializer.Serialize(clase), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"https://walletobjects.googleapis.com/walletobjects/v1/loyaltyClass/{classId}")
            {
                Content = contentPatch
            };
            respuesta = await http.SendAsync(request);
        }

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Console.WriteLine(respuesta.IsSuccessStatusCode
            ? $"Clase creada/actualizada correctamente:\n{cuerpo}"
            : $"Error ({(int)respuesta.StatusCode}):\n{cuerpo}");
    }
}
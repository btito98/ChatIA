using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/chat", async ([FromBody] ChatRequest request) =>
{
    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(600);

    var payload = new
    {
        model = "mistral",
        prompt = request.Message,
        Stream = true
    };

    var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    var response = await client.PostAsync("http://localhost:11434/api/generate", jsonContent);

    if (!response.IsSuccessStatusCode)
    {
        await Console.Out.WriteLineAsync($"Erro na requisição: {response.StatusCode}");
        return Results.Problem("Erro ao obter resposta.");
    }

    var completeResponse = new StringBuilder();
    var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);    

    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();    
        if (!string.IsNullOrWhiteSpace(line))
        {
            try
            {
                await Console.Out.WriteLineAsync($"JSON Recebido: {line}");

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(line);
                if (jsonElement.TryGetProperty("response", out var responseText))
                {
                    completeResponse.Append(responseText.GetString());
                }
            }
            catch (JsonException ex)
            {
                await Console.Out.WriteLineAsync($"Erro ao deserializar JSON: {ex.Message}");
            }
        }
    }

    return Results.Ok(completeResponse.ToString());
});

app.UseAuthorization();

app.MapControllers();

app.Run();

record ChatRequest(string Message);

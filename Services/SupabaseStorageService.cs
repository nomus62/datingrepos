using System.Net.Http.Headers;

namespace DatingApp.Server.Services;

public interface ISupabaseStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAsync(string publicUrl);
}

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _serviceKey;
    private readonly ILogger<SupabaseStorageService> _logger;
    private const string BucketName = "photos";

    public SupabaseStorageService(
        IConfiguration configuration,
        ILogger<SupabaseStorageService> logger)
    {
        _logger = logger;
        _supabaseUrl = configuration["SUPABASE_URL"]
            ?? throw new InvalidOperationException("SUPABASE_URL не задан");
        _serviceKey = configuration["SUPABASE_SERVICE_KEY"]
            ?? throw new InvalidOperationException("SUPABASE_SERVICE_KEY не задан");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{_supabaseUrl}/storage/v1/")
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _serviceKey);
        _httpClient.DefaultRequestHeaders.Add("apikey", _serviceKey);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var path = $"{Guid.NewGuid()}_{fileName}";
        var url = $"object/{BucketName}/{path}";

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Supabase upload failed: {Error}", error);
            throw new Exception($"Не удалось загрузить в Supabase: {error}");
        }

        // Публичный URL
        return $"{_supabaseUrl}/storage/v1/object/public/{BucketName}/{path}";
    }

    public async Task<bool> DeleteAsync(string publicUrl)
    {
        try
        {
            // Извлекаем путь из публичного URL
            var marker = $"/object/public/{BucketName}/";
            var idx = publicUrl.IndexOf(marker);
            if (idx < 0) return false;

            var path = publicUrl.Substring(idx + marker.Length);
            var url = $"object/{BucketName}/{path}";

            var response = await _httpClient.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления из Supabase");
            return false;
        }
    }
}
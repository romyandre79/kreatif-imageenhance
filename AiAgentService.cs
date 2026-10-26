using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fotocopy
{
    public class AiConfig
    {
        public string Provider { get; set; } = "Pollinations.ai (Free - No Key)";
        public string ApiKey { get; set; } = "";
        public string ImageModel { get; set; } = "flux";
        public string VisionModel { get; set; } = "gemini-1.5-flash";
        public string CustomEndpoint { get; set; } = "";

        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fotocopy",
            "ai_config.txt");

        public static AiConfig Load()
        {
            var config = new AiConfig();
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string[] lines = File.ReadAllLines(ConfigPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string val = parts[1].Trim();
                            if (key == "Provider") config.Provider = val;
                            else if (key == "ApiKey") config.ApiKey = val;
                            else if (key == "ImageModel") config.ImageModel = val;
                            else if (key == "VisionModel") config.VisionModel = val;
                            else if (key == "CustomEndpoint") config.CustomEndpoint = val;
                        }
                    }
                }
            }
            catch { }
            return config;
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                sb.AppendLine($"Provider={Provider}");
                sb.AppendLine($"ApiKey={ApiKey}");
                sb.AppendLine($"ImageModel={ImageModel}");
                sb.AppendLine($"VisionModel={VisionModel}");
                sb.AppendLine($"CustomEndpoint={CustomEndpoint}");
                File.WriteAllText(ConfigPath, sb.ToString());
            }
            catch { }
        }
    }

    public static class AiAgentService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        public static async Task<byte[]> GenerateImageAsync(string prompt, AiConfig config, int width = 1024, int height = 1024)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt tidak boleh kosong.");

            string provider = config.Provider ?? "Pollinations.ai (Free - No Key)";

            if (provider.StartsWith("Pollinations", StringComparison.OrdinalIgnoreCase))
            {
                return await GeneratePollinationsAsync(prompt, config.ApiKey, config.ImageModel, width, height);
            }
            else if (provider.StartsWith("HuggingFace", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateHuggingFaceAsync(prompt, config.ApiKey, config.ImageModel);
            }
            else if (provider.StartsWith("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateOpenAiAsync(prompt, config.ApiKey, config.ImageModel, width, height);
            }
            else if (provider.StartsWith("Gemini", StringComparison.OrdinalIgnoreCase) || provider.StartsWith("Google", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateGeminiImagenAsync(prompt, config.ApiKey, config.ImageModel);
            }
            else
            {
                // Fallback default
                return await GeneratePollinationsAsync(prompt, config.ApiKey, "flux", width, height);
            }
        }

        private static async Task<byte[]> GeneratePollinationsAsync(string prompt, string apiKey, string model, int width, int height)
        {
            string chosenModel = string.IsNullOrWhiteSpace(model) ? "flux" : model.Trim().ToLowerInvariant();
            string encodedPrompt = Uri.EscapeDataString(prompt);
            string url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={width}&height={height}&model={chosenModel}&nologo=true&seed={new Random().Next(100000, 999999)}";

            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                // Jika user memiliki Pollen key / Pollinations Token
                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "(Tidak perlu API Key)")
                {
                    req.Headers.Add("Authorization", $"Bearer {apiKey.Trim()}");
                }

                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Pollinations Error ({response.StatusCode}): {err}");
                }
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        private static async Task<byte[]> GenerateHuggingFaceAsync(string prompt, string apiKey, string model)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Token HuggingFace (Free Access Token dari huggingface.co/settings/tokens) belum diisi.");

            string chosenModel = string.IsNullOrWhiteSpace(model) ? "black-forest-labs/FLUX.1-schnell" : model.Trim();
            string url = $"https://api-inference.huggingface.co/models/{chosenModel}";

            string jsonBody = "{\"inputs\": \"" + EscapeJson(prompt) + "\"}";

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                req.Headers.Add("Authorization", $"Bearer {apiKey.Trim()}");
                req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HuggingFace Error ({response.StatusCode}): {err}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        private static async Task<byte[]> GenerateOpenAiAsync(string prompt, string apiKey, string model, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("API Key OpenAI belum diatur. Silakan isi API Key di pengaturan AI.");

            string chosenModel = string.IsNullOrWhiteSpace(model) ? "dall-e-3" : model;
            string sizeStr = $"{width}x{height}";
            if (chosenModel == "dall-e-3")
            {
                sizeStr = "1024x1024";
            }

            string jsonBody = "{" +
                $"\"model\":\"{EscapeJson(chosenModel)}\"," +
                $"\"prompt\":\"{EscapeJson(prompt)}\"," +
                "\"n\":1," +
                $"\"size\":\"{sizeStr}\"," +
                "\"response_format\":\"b64_json\"" +
                "}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations"))
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey.Trim()}");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenAI Error ({response.StatusCode}): {responseBody}");
                }

                var match = Regex.Match(responseBody, "\"b64_json\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success)
                {
                    return Convert.FromBase64String(match.Groups[1].Value);
                }

                var urlMatch = Regex.Match(responseBody, "\"url\"\\s*:\\s*\"([^\"]+)\"");
                if (urlMatch.Success)
                {
                    string imgUrl = urlMatch.Groups[1].Value.Replace("\\/", "/");
                    return await _httpClient.GetByteArrayAsync(imgUrl);
                }

                throw new Exception("Format balasan OpenAI tidak dikenali: " + responseBody);
            }
        }

        private static async Task<byte[]> GenerateGeminiImagenAsync(string prompt, string apiKey, string model = "imagen-3.0-generate-002")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("API Key Google Gemini belum diatur. Silakan isi API Key di pengaturan AI.");

            string chosenModel = string.IsNullOrWhiteSpace(model) ? "imagen-3.0-generate-002" : model.Trim();

            // Jika memilih model Gemini (seperti gemini-2.0-flash-exp), gunakan generateContent dengan responseModalities: ["IMAGE"]
            if (chosenModel.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateGeminiNativeImageAsync(prompt, apiKey, chosenModel);
            }

            // Jika memilih Imagen (misal imagen-3.0-generate-002), panggil endpoint :predict
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{chosenModel}:predict?key={apiKey.Trim()}";

            string jsonBody = "{" +
                "\"instances\": [{\"prompt\": \"" + EscapeJson(prompt) + "\"}]," +
                "\"parameters\": {\"sampleCount\": 1, \"aspectRatio\": \"1:1\", \"outputMimeType\": \"image/png\"}" +
                "}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var match = Regex.Match(responseBody, "\"bytesBase64Encoded\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                    {
                        return Convert.FromBase64String(match.Groups[1].Value);
                    }
                }
                else
                {
                    // Jika akun Gemini Free tier tidak memiliki akses Imagen 3 (Error 404/NotFound),
                    // coba fallback otomatis ke gemini-2.0-flash-exp (multimodal image generation)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound || responseBody.Contains("not found"))
                    {
                        try
                        {
                            return await GenerateGeminiNativeImageAsync(prompt, apiKey, "gemini-2.0-flash-exp");
                        }
                        catch
                        {
                            // Jika fallback juga gagal, tampilkan pesan informatif
                            throw new Exception($"Gemini Imagen Error ({response.StatusCode}): Akun API Key Anda (Free Tier) belum memiliki akses ke model Imagen 3 (memerlukan billing/paid plan di Google AI Studio).\nDetail: {responseBody}");
                        }
                    }

                    throw new Exception($"Gemini Imagen Error ({response.StatusCode}): {responseBody}");
                }

                throw new Exception("Gambar tidak ditemukan pada respons Gemini: " + responseBody);
            }
        }

        private static async Task<byte[]> GenerateGeminiNativeImageAsync(string prompt, string apiKey, string modelName)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey.Trim()}";

            string jsonBody = "{" +
                "\"contents\": [{\"parts\": [{\"text\": \"" + EscapeJson("Please generate and output the following image in high quality: " + prompt) + "\"}]}]," +
                "\"generationConfig\": {\"responseModalities\": [\"TEXT\", \"IMAGE\"]}" +
                "}";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Gemini Multimodal Error ({response.StatusCode}): {responseBody}");
                }

                // Cari "data": "base64..." di dalam inlineData
                var match = Regex.Match(responseBody, "\"data\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success)
                {
                    return Convert.FromBase64String(match.Groups[1].Value);
                }

                var matchBytes = Regex.Match(responseBody, "\"bytesBase64Encoded\"\\s*:\\s*\"([^\"]+)\"");
                if (matchBytes.Success)
                {
                    return Convert.FromBase64String(matchBytes.Groups[1].Value);
                }

                if (responseBody.Contains("NO_IMAGE"))
                {
                    throw new Exception($"Model '{modelName}' belum mendukung pembuatan gambar langsung (finishReason: NO_IMAGE). Model Gemini text-only (seperti gemini-1.5 atau flash-tts) tidak dapat menggambar. Silakan pilih 'Pollinations.ai (Free - No Key)' dengan model 'flux' atau OpenAI 'dall-e-3' untuk hasil gambar langsung.");
                }

                throw new Exception("Model tidak mengembalikan gambar: " + responseBody);
            }
        }

        public static async Task<System.Collections.Generic.List<string>> FetchAvailableModelsAsync(string provider, string apiKey)
        {
            var result = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(provider))
                return result;

            try
            {
                if (provider.StartsWith("Google", StringComparison.OrdinalIgnoreCase) || provider.StartsWith("Gemini", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(apiKey)) return result;

                    string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey.Trim()}";
                    var resp = await _httpClient.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        // Parse model blocks to filter only models suitable for generating images
                        var matches = Regex.Matches(json, "\"name\"\\s*:\\s*\"models/([^\"]+)\"");
                        foreach (Match m in matches)
                        {
                            string modelName = m.Groups[1].Value.Trim();

                            // Filter out non-image models (tts, audio, embedding, learnlm, etc.)
                            if (modelName.Contains("tts") || modelName.Contains("audio") ||
                                modelName.Contains("embedding") || modelName.Contains("aqa") ||
                                modelName.Contains("learnlm"))
                            {
                                continue;
                            }

                            if (!result.Contains(modelName))
                            {
                                result.Add(modelName);
                            }
                        }
                    }
                }
                else if (provider.StartsWith("OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(apiKey)) return result;

                    using (var req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models"))
                    {
                        req.Headers.Add("Authorization", $"Bearer {apiKey.Trim()}");
                        var resp = await _httpClient.SendAsync(req);
                        if (resp.IsSuccessStatusCode)
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            var matches = Regex.Matches(json, "\"id\"\\s*:\\s*\"([^\"]+)\"");
                            foreach (Match m in matches)
                            {
                                string modelName = m.Groups[1].Value.Trim();
                                // Filter image or visual capable models
                                if (modelName.Contains("dall-e") || modelName.Contains("gpt-4o") || modelName.Contains("vision"))
                                {
                                    if (!result.Contains(modelName))
                                    {
                                        result.Add(modelName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return result;
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}

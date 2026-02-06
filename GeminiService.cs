using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeSim
{
    public static class GeminiService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string? apiKey = null;
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public static bool IsInitialized => !string.IsNullOrEmpty(apiKey);

        public static void Initialize()
        {
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("[GeminiService] WARNING: GEMINI_API_KEY environment variable not set.");
                Console.WriteLine("[GeminiService] AI responses will be disabled. Set the environment variable and restart.");
            }
            else
            {
                Console.WriteLine("[GeminiService] API key loaded successfully.");
            }
        }

        public static async Task<string> GenerateResponseAsync(
            CharacterData character,
            string mood,
            List<ChatMessage> chatHistory,
            string userMessage)
        {
            if (!IsInitialized)
            {
                return "[AI Offline - No API Key]";
            }

            try
            {
                // Build system instruction from character data
                string systemPrompt = CharacterLoader.BuildSystemPrompt(character, mood);

                // Build contents array with chat history
                var contents = new List<object>();

                // Add chat history
                foreach (var msg in chatHistory)
                {
                    contents.Add(new
                    {
                        role = msg.Role,
                        parts = new[] { new { text = msg.Text } }
                    });
                }

                // Add current user message
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                // Build request body
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.9,
                        maxOutputTokens = 150,
                        topP = 0.95
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                string url = $"{API_URL}?key={apiKey}";
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GeminiService] API Error: {response.StatusCode} - {errorBody}");
                    return "[API Error - Check Console]";
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse response
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("candidates", out JsonElement candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out JsonElement contentElem) &&
                            contentElem.TryGetProperty("parts", out JsonElement parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out JsonElement textElem))
                            {
                                return textElem.GetString() ?? "...";
                            }
                        }
                    }
                }

                return "...";
            }
            catch (Exception e)
            {
                Console.WriteLine($"[GeminiService] Exception: {e.Message}");
                return "[Connection Error]";
            }
        }

        public static string AnalyzeSentiment(string message)
        {
            // Simple keyword-based sentiment analysis
            // Returns: "positive", "negative", or "neutral"

            string lower = message.ToLower();

            // Positive keywords
            string[] positiveWords = { "love", "like", "great", "awesome", "amazing", "happy",
                "good", "nice", "thank", "thanks", "cool", "sweet", "beautiful", "wonderful",
                "best", "fun", "excited", "joy", "glad", "appreciate", "cute", "adorable" };

            // Negative keywords  
            string[] negativeWords = { "hate", "dislike", "bad", "awful", "terrible", "sad",
                "angry", "annoyed", "stupid", "dumb", "ugly", "worst", "boring", "mean",
                "suck", "horrible", "disgusting", "pathetic", "leave", "go away", "shut up" };

            int positiveScore = 0;
            int negativeScore = 0;

            foreach (var word in positiveWords)
            {
                if (lower.Contains(word)) positiveScore++;
            }

            foreach (var word in negativeWords)
            {
                if (lower.Contains(word)) negativeScore++;
            }

            if (positiveScore > negativeScore) return "positive";
            if (negativeScore > positiveScore) return "negative";
            return "neutral";
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user"; // "user" or "model"
        public string Text { get; set; } = "";

        public ChatMessage(string role, string text)
        {
            Role = role;
            Text = text;
        }
    }
}

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
        // the model I am using is gemini-2.0-flash
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
            string userMessage,
            List<string> memories)
        {
            if (!IsInitialized)
            {
                return "[AI Offline - No API Key]";
            }

            try
            {
                // Build system instruction from character data
                string systemPrompt = CharacterLoader.BuildSystemPrompt(character, mood);

                // Inject Memories if available
                if (memories != null && memories.Count > 0)
                {
                    systemPrompt += "\n\n[RELEVANT MEMORIES (DIARY ENTRIES)]\n";
                    systemPrompt += "The following are past events you have recorded in your diary. Use them IF RELEVANT to the conversation:\n";
                    foreach (var mem in memories)
                    {
                        systemPrompt += $"- {mem}\n";
                    }
                }

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

        public static async Task<string> SelectMoodAsync(
            string userMessage,
            string currentMood,
            MoodWeights weights)
        {
            if (!IsInitialized)
            {
                return "no_change"; // Can't analyze without API
            }

            try
            {
                // Build prompt for mood selection
                string systemPrompt = @"You are analyzing a player's message to determine the best emotional reaction for an NPC character.

Your task: Choose ONE mood from the available options that best fits as the NPC's emotional response to what the player said.

Rules:
1. Respond with ONLY the mood name (e.g., 'bashful' or 'no_change')
2. Consider the mood weights - higher weight moods are more appropriate/likely
3. Choose 'no_change' if the current mood is still appropriate
4. Be realistic - not every message needs a mood change
5. Match the intensity of the player's message";

                string userPrompt = $@"Player said: ""{userMessage}""

Current NPC mood: {currentMood}

Available moods:
{weights.GetWeightsDescription()}

What mood should the NPC react with? (respond with only the mood name)";

                // Build request
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = userPrompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7, // Slightly lower for more consistent choices
                        maxOutputTokens = 20, // Just need one word
                        topP = 0.9
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                string url = $"{API_URL}?key={apiKey}";
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GeminiService] Mood Selection Error: {response.StatusCode} - {errorBody}");
                    return "no_change";
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
                                string selectedMood = textElem.GetString()?.Trim().ToLower() ?? "no_change";

                                // Validate the mood is in our list or is "no_change"
                                if (selectedMood == "no_change" || weights.Weights.ContainsKey(selectedMood))
                                {
                                    return selectedMood;
                                }

                                Console.WriteLine($"[GeminiService] AI selected invalid mood: {selectedMood}, defaulting to no_change");
                                return "no_change";
                            }
                        }
                    }
                }

                return "no_change";
            }
            catch (Exception e)
            {
                Console.WriteLine($"[GeminiService] Mood Selection Exception: {e.Message}");
                return "no_change";
            }
        }
        public static async Task<string> SendRawPromptAsync(string systemPrompt, string userPrompt, int maxTokens = 150)
        {
            return await SendRawPromptAsync(systemPrompt, userPrompt, maxTokens, 0.9f, 0.95f);
        }

        public static async Task<string> SendRawPromptAsync(string systemPrompt, string userPrompt, int maxTokens, float temperature, float topP)
        {
            if (!IsInitialized)
            {
                return "[AI Offline - No API Key]";
            }

            try
            {
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = userPrompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = maxTokens,
                        topP = topP
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                string url = $"{API_URL}?key={apiKey}";
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GeminiService] Raw Prompt Error: {response.StatusCode} - {errorBody}");
                    return "[API Error]";
                }

                string responseBody = await response.Content.ReadAsStringAsync();

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
                Console.WriteLine($"[GeminiService] Raw Prompt Exception: {e.Message}");
                return "[Connection Error]";
            }
        }

        public static async Task<(string Summary, string Content)> GenerateDiarySummaryAsync(string npcName, List<ChatMessage> recentHistory)
        {
            if (!IsInitialized) return ("System", "AI Offline. Cannot generate diary.");

            try
            {
                // Construct parameters
                string historyText = "";
                foreach (var msg in recentHistory)
                {
                    historyText += $"{msg.Role}: {msg.Text}\n";
                }

                string systemPrompt = $@"You are writing a diary entry for yourself, you are '{npcName}'.
                    Task: Summarize the following conversation into a single diary entry written in the first person ('I').
                    1. Create a short title for the entry (e.g., 'Met a new friend', 'Talked about cats').
                    2. Write the diary content (2-5 sentences max) capturing the key points and your feelings.
                    3. Output format must be strictly JSON: {{ ""title"": ""..."", ""content"": ""..."" }}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Conversation History:");
                sb.AppendLine(historyText);
                string userPrompt = sb.ToString();

                // Call API
                string responseJson = await SendRawPromptAsync(systemPrompt, userPrompt, 200, 0.7f, 0.95f);

                // Clean response if md code blocks exist
                if (responseJson.Contains("```json"))
                {
                    responseJson = responseJson.Replace("```json", "").Replace("```", "").Trim();
                }
                else if (responseJson.Contains("```"))
                {
                    responseJson = responseJson.Replace("```", "").Trim();
                }

                // Parse JSON
                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    string title = "Diary Entry";
                    string content = "...";

                    if (doc.RootElement.TryGetProperty("title", out JsonElement titleEl))
                        title = titleEl.GetString() ?? "Diary Entry";

                    if (doc.RootElement.TryGetProperty("content", out JsonElement contentEl))
                        content = contentEl.GetString() ?? "...";

                    return (title, content);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[GeminiService] Diary Generation Error: {e.Message}");
                // If JSON fails, fallback to raw text if it looks like content
                return ("Diary Entry", "Could not generate structured diary entry.");
            }
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

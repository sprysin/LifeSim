using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace LifeSim
{
    public static class DialogueManager
    {
        private static Dictionary<string, Dictionary<string, List<string>>> npcDialogues = new Dictionary<string, Dictionary<string, List<string>>>();

        public static void Initialize()
        {
            // Clear existing if any (re-init support)
            npcDialogues.Clear();

            try
            {
                string jsonString = File.ReadAllText(Path.Combine("NPC_Data", "NPC_dialog.JSON"));

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("NPC_dialog", out JsonElement dialogNode))
                    {
                        foreach (JsonProperty npcProperty in dialogNode.EnumerateObject())
                        {
                            string npcName = npcProperty.Name;
                            var moodDict = new Dictionary<string, List<string>>();

                            foreach (JsonProperty moodProperty in npcProperty.Value.EnumerateObject())
                            {
                                string moodName = moodProperty.Name;
                                var phrases = new List<string>();

                                // The JSON structure has keys like "neutral_1", "neutral_2" pointing to strings
                                // We iterate the object and collect all string values
                                foreach (JsonProperty phraseProperty in moodProperty.Value.EnumerateObject())
                                {
                                    if (phraseProperty.Value.ValueKind == JsonValueKind.String)
                                    {
                                        string? val = phraseProperty.Value.GetString();
                                        if (val != null) phrases.Add(val);
                                    }
                                }

                                moodDict[moodName] = phrases;
                            }

                            npcDialogues[npcName] = moodDict;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading dialogue JSON: {e.Message}");
            }
        }

        public static string GetRandomPhrase(string npcName, string mood)
        {
            if (npcDialogues != null && npcDialogues.ContainsKey(npcName))
            {
                if (npcDialogues[npcName].ContainsKey(mood))
                {
                    var phrases = npcDialogues[npcName][mood];
                    if (phrases.Count > 0)
                    {
                        Random rng = new Random();
                        return phrases[rng.Next(phrases.Count)];
                    }
                }
                // Fallback: try "neutral" if specific mood fails
                else if (mood != "neutral" && npcDialogues[npcName].ContainsKey("neutral"))
                {
                    return GetRandomPhrase(npcName, "neutral");
                }
            }
            return "...";
        }

        public static List<string> GetAvailableMoods(string npcName)
        {
            if (npcDialogues != null && npcDialogues.ContainsKey(npcName))
            {
                return npcDialogues[npcName].Keys.ToList();
            }
            return new List<string>();
        }
    }
}

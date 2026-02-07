using System.Text.Json;
using System.IO;

namespace LifeSim
{
    public class CharacterData
    {
        public string Name { get; set; } = "";
        public string Appearance { get; set; } = "";
        public string Backstory { get; set; } = "";
        public string Personality { get; set; } = "";
        public string Likes_Dislikes { get; set; } = "";
    }

    public static class CharacterLoader
    {
        private static readonly string CharactersFolder = Path.Combine("NPC_Data", "characters");
        private static Dictionary<string, CharacterData> loadedCharacters = new Dictionary<string, CharacterData>();

        public static CharacterData? LoadCharacter(string name)
        {
            // Check cache first
            if (loadedCharacters.ContainsKey(name))
            {
                return loadedCharacters[name];
            }

            string filePath = Path.Combine(CharactersFolder, $"{name}_Character_sheet.json");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Character file not found: {filePath}");
                return null;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                CharacterData? character = JsonSerializer.Deserialize<CharacterData>(jsonString, options);

                if (character != null)
                {
                    loadedCharacters[name] = character;
                }

                return character;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading character {name}: {e.Message}");
                return null;
            }
        }

        public static string BuildSystemPrompt(CharacterData character, string currentMood)
        {
            string prompt = $@"You are {character.Name}, a character in a life simulation game. Stay completely in character at all times.

## Character Information
- **Name**: {character.Name}
- **Current Mood**: {currentMood}

## Appearance
{(string.IsNullOrEmpty(character.Appearance) ? "Not specified" : character.Appearance)}

## Backstory
{(string.IsNullOrEmpty(character.Backstory) ? "Not specified" : character.Backstory)}

## Personality
{(string.IsNullOrEmpty(character.Personality) ? "Not specified" : character.Personality)}

## Likes & Dislikes
{(string.IsNullOrEmpty(character.Likes_Dislikes) ? "Not specified" : character.Likes_Dislikes)}

## Mood Guidelines
Your current mood is ""{currentMood}"". This should influence your responses:
- If Neutral: Respond normally, casually friendly.
- If Grumpy: Be shorter, colder, slightly irritated in your responses.
- If Energized: Be enthusiastic, use exclamation marks, show excitement.
- If Sad: Be subdued, melancholic, shorter responses.
- If Bashful: Be shy, flustered, use ellipses and trailing off. Sometimes flirtatious back to the player. sometimes snarky.
- If Shocked: Express disbelief, gasp, use interrobangs (?!).
- If Spooked: Stutter, express fear, scream.

## Response Rules
1. Keep responses concise (1-3 sentences max) - this is a game dialogue box.
2. When you want to write performing an action, use asterisks to denote the action. For example: *Walks to the window and looks out*. Try to keep actions minimal, only write actions if the scene needs one.
3. Your mood may shift based on the conversation - express this naturally, do not switch moods too quickly sit on an emotion for a while. Unless it is necessary for an instant mood shift (E.g. getting Shocked or spooked).
4. Write dialog as continuous prose without line breaks, bullets, or lists.
";
            return prompt;
        }

        public static void ClearCache()
        {
            loadedCharacters.Clear();
        }
    }
}

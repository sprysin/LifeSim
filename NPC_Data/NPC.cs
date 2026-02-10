using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public class NPC
    {
        public int GridX;
        public int GridY;
        public string SpritePath;
        private Texture2D customSprite;

        public string Name;
        public string DialogueText;
        public string PortraitPath;
        public string PortraitCode = "";
        public float Scale = 1.0f;
        public string CurrentMood = "neutral";



        // Animation State

        // Sprite Sheet Details
        private const int GapX = 1;
        private const int GapY = 8;
        private const int SheetColumns = 16;
        private const int SheetRows = 4;

        // Animation Timing
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private const float FrameSpeed = 0.15f;

        public Queue<string> ConversationQueue = new Queue<string>();

        // LLM Integration
        public CharacterData? CharacterData { get; private set; }
        public ConversationManager? Conversation { get; private set; }
        public bool IsWaitingForAI { get; set; } = false;
        public string? PendingAIResponse { get; set; } = null;

        public string CurrentSkin = "Skin0";
        // Static tracker for exclusive following
        public static NPC? ActiveFollower = null;
        public bool IsFollowing => ActiveFollower == this;

        public void SetFollow(bool follow)
        {
            if (follow) ActiveFollower = this;
            else if (ActiveFollower == this) ActiveFollower = null;
        }

        public NPC(int x, int y, string name, string text, string portraitPath = "", string spritePath = "", float scale = 1.0f)
        {
            GridX = x;
            @GridY = y;
            Name = name;
            DialogueText = text;
            PortraitPath = portraitPath;
            SpritePath = spritePath;
            Scale = scale;

            // Extract Portrait Code based on NPC name
            if (Name == "Boogie") PortraitCode = "B";
            if (Name == "Testern") PortraitCode = "main_char_full_body";
            if (!string.IsNullOrEmpty(SpritePath) && System.IO.File.Exists(SpritePath))
            {
                customSprite = Raylib.LoadTexture(SpritePath);
            }

            // Initialize Dialogue from Manager
            UpdateDialogue();

            // Load Character Data for LLM
            CharacterData = CharacterLoader.LoadCharacter(name);
            if (CharacterData != null)
            {
                Conversation = new ConversationManager(name);
                Console.WriteLine($"[NPC] Loaded character data for {name}");
            }
        }

        public string GetCurrentPortraitPath()
        {
            if (!string.IsNullOrEmpty(PortraitCode))
            {
                // Format: Visual Novel Images/{Name}/{Code}_{Mood}_{Skin}.png
                // Capitalize Mood
                string moodCap = char.ToUpper(CurrentMood[0]) + CurrentMood.Substring(1).ToLower();

                // Check for specific skin + mood
                string path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_{moodCap}_{CurrentSkin}.png");

                if (System.IO.File.Exists(path))
                {
                    return path;
                }

                // Fallback to Skin0 if specific skin not found
                if (CurrentSkin != "Skin0")
                {
                    path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_{moodCap}_Skin0.png");
                    if (System.IO.File.Exists(path)) return path;
                }

                // Fallback to Neutral + Current Skin
                path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_Neutral_{CurrentSkin}.png");
                if (System.IO.File.Exists(path)) return path;

                // Fallback to Neutral + Skin0
                path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_Neutral_Skin0.png");
                if (System.IO.File.Exists(path)) return path;
            }

            return PortraitPath; // Fallback to static path
        }

        public void UpdateDialogue()
        {
            string newText = DialogueManager.GetRandomPhrase(Name, CurrentMood);
            if (newText != "...")
            {
                DialogueText = newText;
            }
        }

        public void SetConversation(List<string> lines)
        {
            ConversationQueue.Clear();
            foreach (var line in lines)
            {
                ConversationQueue.Enqueue(line);
            }
        }

        public void PrependConversation(IEnumerable<string> lines)
        {
            var newQueue = new Queue<string>(lines);
            while (ConversationQueue.Count > 0)
            {
                newQueue.Enqueue(ConversationQueue.Dequeue());
            }
            ConversationQueue = newQueue;
        }

        public string? AdvanceConversation()
        {
            if (ConversationQueue.Count > 0)
            {
                return ConversationQueue.Dequeue();
            }
            return null;
        }

        public async Task<string?> GetAIResponseAsync(string userMessage)
        {
            if (CharacterData == null || Conversation == null)
            {
                return null; // No LLM support for this NPC
            }

            if (!GeminiService.IsInitialized)
            {
                return "[AI Offline - No API Key]";
            }

            IsWaitingForAI = true;

            try
            {
                // Step 1: Let AI select the best mood for this interaction
                string selectedMood = await GeminiService.SelectMoodAsync(
                    userMessage,
                    CurrentMood,
                    Conversation.MoodWeights
                );

                // Update mood if AI suggested a change
                if (selectedMood != "no_change" && selectedMood != CurrentMood)
                {
                    Console.WriteLine($"[NPC] {Name} mood shifted: {CurrentMood} -> {selectedMood}");
                    CurrentMood = selectedMood;
                }
                else if (selectedMood == "no_change")
                {
                    Console.WriteLine($"[NPC] {Name} keeping mood: {CurrentMood}");
                }

                // Step 2: Add user message to history
                Conversation.AddUserMessage(userMessage);

                // Step 3: Get AI response with the (possibly new) mood
                string response = await GeminiService.GenerateResponseAsync(
                    CharacterData,
                    CurrentMood,
                    Conversation.GetHistory(),
                    userMessage
                );

                // Step 4: Add AI response to history
                Conversation.AddModelMessage(response);
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NPC] AI Error: {e.Message}");
                return "[Error getting response]";
            }
            finally { IsWaitingForAI = false; }
        }

        public void Update(float dt)
        {
            UpdateAnimation(dt);
        }

        private void UpdateAnimation(float dt)
        {
            // Just idle animation or simple "breathing"
            frameTimer += dt;
            if (frameTimer >= FrameSpeed)
            {
                currentFrame = (currentFrame + 1) % 4;
                frameTimer = 0;
            }
        }

        public Texture2D PortraitTexture;
        private string loadedPortraitPath = "";

        public void DrawStatic(int centerX, int centerY, float scale)
        {
            string currentPath = GetCurrentPortraitPath();

            // Load/Update Texture if needed
            if (currentPath != loadedPortraitPath)
            {
                if (PortraitTexture.Id != 0) Raylib.UnloadTexture(PortraitTexture);

                // Debug logging
                Console.WriteLine($"[NPC.DrawStatic] {Name} - Trying to load portrait: '{currentPath}'");
                Console.WriteLine($"[NPC.DrawStatic] {Name} - File exists: {System.IO.File.Exists(currentPath)}");
                Console.WriteLine($"[NPC.DrawStatic] {Name} - PortraitCode: '{PortraitCode}', PortraitPath: '{PortraitPath}'");

                if (!string.IsNullOrEmpty(currentPath) && System.IO.File.Exists(currentPath))
                {
                    PortraitTexture = Raylib.LoadTexture(currentPath);
                    loadedPortraitPath = currentPath;
                    Raylib.SetTextureFilter(PortraitTexture, TextureFilter.Bilinear); // High Res needs bilinear
                    Console.WriteLine($"[NPC.DrawStatic] {Name} - Portrait loaded successfully! Texture ID: {PortraitTexture.Id}");
                }
                else
                {
                    loadedPortraitPath = ""; // Failed
                    Console.WriteLine($"[NPC.DrawStatic] {Name} - Failed to load portrait!");
                }
            }

            if (PortraitTexture.Id != 0)
            {
                // ⬇️ VN SPRITE SIZE ADJUSTMENT - CHANGE THESE VALUES:
                // The sprite auto-scales to fit screen height. Adjust the percentage below:

                float drawScale = 1.0f;
                int screenH = Raylib.GetScreenHeight();

                // ⬇️ CHANGE THIS: 0.8f = sprite fits to 80% of screen height
                // Try: 0.9f for larger, 0.7f for smaller, 1.0f for full height
                float maxH = screenH * 0.94f;  // ← ADJUST THIS VALUE (0.8f = 80% of screen)

                if (PortraitTexture.Height > maxH)
                {
                    drawScale = maxH / (float)PortraitTexture.Height;
                }

                float finalW = PortraitTexture.Width * drawScale;
                float finalH = PortraitTexture.Height * drawScale;

                // Position: centerX and centerY are passed from TileSystem.DrawStaticScene
                Rectangle source = new Rectangle(0, 0, PortraitTexture.Width, PortraitTexture.Height);
                Rectangle dest = new Rectangle(centerX - finalW / 2, centerY - finalH + 100, finalW + 20, finalH);

                Raylib.DrawTexturePro(PortraitTexture, source, dest, Vector2.Zero, 0f, Color.White);
            }
        }

    }
}

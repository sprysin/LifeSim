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
        private readonly bool hasCustomSprite = false;

        public string Name;
        public string DialogueText;
        public string PortraitPath;
        public string PortraitCode = "";
        public float Scale = 1.0f;
        public string CurrentMood = "neutral";



        // Animation State
        private enum Direction { Down = 0, Left = 1, Right = 2, Up = 3 }
        private Direction currentDir = Direction.Down;
        private bool isMoving = false;

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

        // AI State
        private enum AIState { Idle, Walking }
        private AIState currentState = AIState.Idle;
        private float stateTimer = 0f;

        // Wandering Params
        private int tilesToWalk = 0;
        private float walkStepTimer = 0f;
        private const float WalkStepInterval = 0.4f; // Time to move 1 tile (slower than player for deliberate feel)
        private System.Random rng = new System.Random();

        public NPC(int x, int y, string name, string text, string portraitPath = "", string spritePath = "", float scale = 1.0f)
        {
            GridX = x;
            GridY = y;
            Name = name;
            DialogueText = text;
            PortraitPath = portraitPath;
            SpritePath = spritePath;
            Scale = scale;

            // Extract Portrait Code if not provided but Name is "Boogie" (Auto-detect or Manual)
            if (Name == "Boogie") PortraitCode = "B";

            if (!string.IsNullOrEmpty(SpritePath) && System.IO.File.Exists(SpritePath))
            {
                customSprite = Raylib.LoadTexture(SpritePath);
                hasCustomSprite = true;
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
                // Format: Visual Novel Images/{Name}/{Code}_{Mood}_Skin0.png
                // Capitalize Mood
                string moodCap = char.ToUpper(CurrentMood[0]) + CurrentMood.Substring(1).ToLower();

                string path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_{moodCap}_Skin0.png");

                if (System.IO.File.Exists(path))
                {
                    return path;
                }

                // Fallback to Neutral if specific mood not found
                path = Path.Combine("NPC_Data", "Visual Novel Images", Name, $"{PortraitCode}_Neutral_Skin0.png");
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
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
            finally
            {
                IsWaitingForAI = false;
            }
        }

        public void Update(float dt)
        {
            stateTimer -= dt;

            if (currentState == AIState.Idle)
            {
                // Transition to Walking
                if (stateTimer <= 0)
                {
                    currentState = AIState.Walking;

                    // Pick direction
                    int dir = rng.Next(0, 4); // 0-3
                    if (dir == 0) currentDir = Direction.Up;
                    else if (dir == 1) currentDir = Direction.Down;
                    else if (dir == 2) currentDir = Direction.Left;
                    else if (dir == 3) currentDir = Direction.Right;

                    // Pick distance
                    tilesToWalk = rng.Next(1, 6); // 1 to 5 tiles

                    // We reuse walkStepTimer to pace the individual steps
                    walkStepTimer = 0f;
                }
            }
            else if (currentState == AIState.Walking)
            {
                isMoving = true;
                walkStepTimer -= dt;

                if (walkStepTimer <= 0)
                {
                    walkStepTimer = WalkStepInterval;

                    // Try to move 1 tile
                    int dx = 0;
                    int dy = 0;

                    switch (currentDir)
                    {
                        case Direction.Up: dy = -1; break;
                        case Direction.Down: dy = 1; break;
                        case Direction.Left: dx = -1; break;
                        case Direction.Right: dx = 1; break;
                    }

                    int newX = GridX + dx;
                    int newY = GridY + dy;

                    if (TileSystem.IsWalkable(newX, newY))
                    {
                        GridX = newX;
                        GridY = newY;
                        tilesToWalk--;

                        if (tilesToWalk <= 0)
                        {
                            // Done walking
                            SwitchToIdle();
                        }
                    }
                    else
                    {
                        // Blocked
                        SwitchToIdle();
                    }
                }
            }

            // Animation Logic
            // Only animate if we are in Walking state (and thus "isMoving" concept applies)
            if (currentState == AIState.Walking)
            {
                frameTimer += dt;
                if (frameTimer >= FrameSpeed)
                {
                    currentFrame++;
                    if (currentFrame >= 4) currentFrame = 0;
                    frameTimer = 0;
                }
            }
            else
            {
                isMoving = false;
                currentFrame = 0;
                frameTimer = 0;
            }
        }

        private void SwitchToIdle()
        {
            currentState = AIState.Idle;
            isMoving = false;
            // Wait 2-5 seconds
            stateTimer = (float)rng.NextDouble() * 3.0f + 2.0f;
            currentFrame = 0;
        }

        public void Draw(Texture2D sheet)
        {
            // Assuming sheet is the 16x4 character sheet (standard or boogie)
            // Use constants defined in class

            // Calculate Frame Size
            int frameWidth = (sheet.Width - (SheetColumns - 1) * GapX) / SheetColumns;
            int frameHeight = (sheet.Height - (SheetRows - 1) * GapY) / SheetRows;

            int scale = 4;
            float scaledTileSize = TileSystem.TileSize * scale;

            Texture2D textureToDraw = sheet;

            // Check for custom sprite
            if (hasCustomSprite)
            {
                textureToDraw = customSprite;

                // Adjust frame calculations for custom sprite if it matches the sheet layout
                // If custom sprite is big (like Boogie's sheet), recalculate frameWidth/Height based on IT
                if (customSprite.Width > 64) // Arbitrary threshold to distinguish single sprite vs sheet
                {
                    frameWidth = (customSprite.Width - (SheetColumns - 1) * GapX) / SheetColumns;
                    frameHeight = (customSprite.Height - (SheetRows - 1) * GapY) / SheetRows;
                }
                else
                {
                    // Small sprite (single frame fallback)
                    frameWidth = customSprite.Width;
                    frameHeight = customSprite.Height;
                    // Force Idle
                    currentFrame = 0;
                }
            }

            int srcX = currentFrame * (frameWidth + GapX);
            // Use currentDir enum cast to int
            int srcY = (int)currentDir * (frameHeight + GapY);

            // Safety check for source rectangle
            if (srcX + frameWidth > textureToDraw.Width) srcX = 0;
            if (srcY + frameHeight > textureToDraw.Height) srcY = 0;

            Rectangle source = new Rectangle(srcX, srcY, frameWidth, frameHeight);

            // Calculate Destination
            // Use "cookie cutter" logic for aspect ratio preservation
            // Reverted to 2x scale
            float drawScale = ((float)scale / 2.0f) * Scale;
            float finalW = frameWidth * drawScale;
            float finalH = frameHeight * drawScale;

            // Draw at grid position (Bottom-Anchored)
            Rectangle dest = new Rectangle(GridX * scaledTileSize, (GridY + 1) * scaledTileSize - finalH, finalW, finalH);

            Raylib.DrawTexturePro(textureToDraw, source, dest, Vector2.Zero, 0f, Color.White);
        }

        public bool IsPlayerNearby(Player p)
        {
            int dx = System.Math.Abs(p.GridX - GridX);
            int dy = System.Math.Abs(p.GridY - GridY);

            return (dx + dy) <= 1;
        }
    }
}

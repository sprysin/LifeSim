using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Dialogue State
        public static bool IsOpen = false;
        private static string currentText = "";
        private static string currentName = "";

        // Typewriter
        private static float typeTimer = 0f;
        private const float TypeSpeed = 0.05f;
        private static int charIndex = 0;

        // Options Menu State
        private static bool showOptionsMenu = false;
        private static int optionSelection = 0;
        private static NPC? currentDialogueNPC = null;
        private static readonly string[] optionLabels = ["Respond", "Action", "Thoughts?"];

        // Visual Novel Character Portrait
        private static Texture2D currentPortrait;
        private static string currentPortraitPath = ""; // Track current path to detect changes
        private static bool hasPortrait = false;

        // Text Input State
        private static bool showTextInput = false;
        private static string inputText = "";
        private static bool isWaitingForAI = false;
        private static float thinkingDots = 0f;
        private static Task<string?>? pendingAITask = null;

        public static void OpenDialogue(string name, string text, string portraitPath = "")
        {
            OpenDialogue(name, text, portraitPath, null);
        }

        public static void OpenDialogue(string name, string text, string portraitPath, NPC? npc)
        {
            IsOpen = true;
            currentName = name;
            currentText = text;
            charIndex = 0;
            typeTimer = 0f;

            // Reset options menu state
            showOptionsMenu = false;
            optionSelection = 0;
            currentDialogueNPC = npc;

            // Load portrait if provided
            if (!string.IsNullOrEmpty(portraitPath) && System.IO.File.Exists(portraitPath))
            {
                if (hasPortrait)
                {
                    Raylib.UnloadTexture(currentPortrait);
                }
                currentPortrait = Raylib.LoadTexture(portraitPath);
                currentPortraitPath = portraitPath; // Cache path
                hasPortrait = true;
            }
            else
            {
                hasPortrait = false;
                currentPortraitPath = "";
            }
        }

        public static void CloseDialogue()
        {
            IsOpen = false;
            if (hasPortrait)
            {
                Raylib.UnloadTexture(currentPortrait);
                hasPortrait = false;
                currentPortraitPath = "";
            }
        }

        public static void Update()
        {
            if (!IsOpen) return;

            // Handle AI response waiting
            if (isWaitingForAI)
            {
                thinkingDots += Raylib.GetFrameTime() * 3f;
                if (thinkingDots > 3f) thinkingDots = 0f;

                // Check if AI response is ready
                if (pendingAITask != null && pendingAITask.IsCompleted)
                {
                    string? response = pendingAITask.Result;
                    pendingAITask = null;
                    isWaitingForAI = false;

                    if (!string.IsNullOrEmpty(response))
                    {
                        // Show AI response with typewriter
                        showTextInput = false;
                        showOptionsMenu = false;
                        currentText = response;
                        charIndex = 0;
                        typeTimer = 0f;
                    }
                }
                return; // Don't process other updates while waiting
            }

            if (charIndex < currentText.Length)
            {
                typeTimer += Raylib.GetFrameTime();
                if (typeTimer >= TypeSpeed)
                {
                    typeTimer = 0;
                    charIndex++;
                }
            }

            // Check for Dynamic Portrait Updates (e.g. Mood Change)
            if (IsOpen && currentDialogueNPC != null)
            {
                string newPath = currentDialogueNPC.GetCurrentPortraitPath();
                if (newPath != currentPortraitPath && System.IO.File.Exists(newPath))
                {
                    // Reload Portrait
                    if (hasPortrait) Raylib.UnloadTexture(currentPortrait);
                    currentPortrait = Raylib.LoadTexture(newPath);
                    currentPortraitPath = newPath;
                    hasPortrait = true;
                }
            }
        }

        public static void HandleInput()
        {
            // Don't process input while waiting for AI
            if (isWaitingForAI) return;

            // Text Input Mode
            if (showTextInput)
            {
                HandleTextInput();
                return;
            }

            if (showOptionsMenu)
            {
                // Options menu navigation
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    optionSelection--;
                    if (optionSelection < 0) optionSelection = optionLabels.Length - 1;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                {
                    optionSelection++;
                    if (optionSelection >= optionLabels.Length) optionSelection = 0;
                }

                // Select option
                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    HandleOptionSelection(optionSelection);
                }

                // Close with Z
                if (Raylib.IsKeyPressed(KeyboardKey.Z))
                {
                    CloseDialogue();
                }
            }
            else
            {
                // Normal dialogue mode
                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (charIndex < currentText.Length)
                    {
                        // Skip typewriter
                        charIndex = currentText.Length;
                    }
                    else
                    {
                        // Check for multi-step conversation
                        if (currentDialogueNPC != null)
                        {
                            string? nextLine = currentDialogueNPC.AdvanceConversation();
                            if (nextLine != null)
                            {
                                // Advance to next line immediately
                                OpenDialogue(currentDialogueNPC.Name, nextLine, currentDialogueNPC.GetCurrentPortraitPath(), currentDialogueNPC);
                                return;
                            }
                        }

                        // Show options menu instead of closing
                        showOptionsMenu = true;
                        optionSelection = 0;
                    }
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Z))
                {
                    CloseDialogue();
                }
            }
        }

        private static void HandleTextInput()
        {
            // Get characters typed
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                // Valid ASCII characters (32-126)
                if (key >= 32 && key <= 126 && inputText.Length < 100)
                {
                    inputText += (char)key;
                }
                key = Raylib.GetCharPressed();
            }

            // Backspace
            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && inputText.Length > 0)
            {
                inputText = inputText[..^1];
            }

            // Submit with Enter
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) && inputText.Length > 0)
            {
                SubmitPlayerMessage(inputText);
                inputText = "";
            }

            // Cancel with Escape or Z
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                showTextInput = false;
                inputText = "";
                showOptionsMenu = true; // Go back to options
            }
        }

        private static void SubmitPlayerMessage(string message)
        {
            if (currentDialogueNPC == null) return;

            // Check if NPC has LLM support
            if (currentDialogueNPC.CharacterData != null)
            {
                // Start async AI request
                isWaitingForAI = true;
                thinkingDots = 0f;
                pendingAITask = currentDialogueNPC.GetAIResponseAsync(message);
            }
            else
            {
                // Fallback to static response
                showTextInput = false;
                currentText = "(No AI response available)";
                charIndex = 0;
                typeTimer = 0f;
            }
        }

        private static void HandleOptionSelection(int selection)
        {
            switch (selection)
            {
                case 0: // Respond - Open text input
                    showOptionsMenu = false;
                    showTextInput = true;
                    inputText = "";
                    currentText = "Type your message...";
                    charIndex = currentText.Length; // Show full prompt
                    break;
                case 1: // Action
                    showOptionsMenu = false;
                    currentText = "(Actions not yet implemented)";
                    charIndex = 0;
                    typeTimer = 0f;
                    break;
                case 2: // What's on your Mind? - Ask AI directly
                    if (currentDialogueNPC?.CharacterData != null)
                    {
                        showOptionsMenu = false;
                        isWaitingForAI = true;
                        thinkingDots = 0f;
                        pendingAITask = currentDialogueNPC.GetAIResponseAsync("What's on your mind right now?");
                    }
                    else
                    {
                        showOptionsMenu = false;
                        currentText = "Nothing much...";
                        charIndex = 0;
                        typeTimer = 0f;
                    }
                    break;
                default:
                    break;
            }
        }

        private static void DrawOptionsPanel(int panelX, int panelY, int panelW, int panelH, int gridSize, float offset, Color gridColor)
        {
            Rectangle panelRect = new Rectangle(panelX, panelY, panelW, panelH);

            // Background (Black Transparent - same as dialogue box)
            Raylib.DrawRectangleRec(panelRect, new Color(0, 0, 0, 220));

            // Scrolling Grid (matching dialogue box style)
            for (int i = -1; i <= panelW / gridSize + 1; i++)
            {
                float lineX = panelX + (i * gridSize) + offset;
                if (lineX >= panelX && lineX <= panelX + panelW)
                {
                    Raylib.DrawLine((int)lineX, panelY, (int)lineX, panelY + panelH, gridColor);
                }
            }
            for (int j = 0; j <= panelH / gridSize; j++)
            {
                int lineY = panelY + (j * gridSize);
                Raylib.DrawLine(panelX, lineY, panelX + panelW, lineY, gridColor);
            }

            // Border (Solid White)
            Raylib.DrawRectangleLinesEx(panelRect, 1, Color.White);

            // Text drawing removed to prevent doubling (see DrawDialogue)

            for (int i = 0; i < optionLabels.Length; i++)
            {
                // Note: Text is drawn in Screen Space in DrawDialogue() to avoid blurriness.
                // We only draw the geometry here.
            }
        }

        public static void DrawDialogue()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // 1. Draw Character Portrait (behind the pixelated box, rendered at full res for smoothness)
            if (hasPortrait)
            {
                // Darken background slightly when dialogue is open
                Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 100));

                float scale = 1.0f;
                if (currentPortrait.Height > screenH * 1.5f) scale = (float)screenH / currentPortrait.Height;

                float scaledW = currentPortrait.Width * scale;
                float scaledH = currentPortrait.Height * scale;
                float posX = (screenW - scaledW) / 2;
                float posY = (screenH * 0.8f) - (scaledH / 1.8f); // Positioned slightly lower for VN feel

                Raylib.DrawTextureEx(currentPortrait, new Vector2(posX, posY), 0f, scale, Color.White);

                // 2. Render Dialogue Box to Low-Res Buffer
                Raylib.BeginTextureMode(UIBuffer);
                Raylib.ClearBackground(Color.Blank);

                // Settings - compress dialogue box when options menu is visible
                int boxX = 10;
                int boxY = 110;
                int fullBoxW = 300;
                int boxW = showOptionsMenu ? (int)(fullBoxW * 0.75f) : fullBoxW; // 225 when options visible
                int boxH = 60; // Restoring for grid loops
                Rectangle boxRect = new Rectangle(boxX, boxY, boxW, boxH);

                // 1. Background (Black Transparent)
                Raylib.DrawRectangleRec(boxRect, new Color(0, 0, 0, 220));

                // 2. Scrolling Pixel Grid
                // Use time-based scrolling
                float time = (float)Raylib.GetTime();
                float scrollSpeed = 5.0f;
                int gridSize = 10;
                float offset = (time * scrollSpeed) % gridSize;

                // Use Scissor Mode to clip grid to box
                // Note: Scissor mode works in screen coordinates, but we are in TextureMode (0,0 is top-left of texture)
                // Raylib.BeginScissorMode(boxX, boxY, boxW, boxH); 
                // Raylib-cs bindings for ScissorMode inside TextureMode can be tricky with offsets. 
                // Instead, we'll just manually loop within bounds for this simple effect.

                Color gridColor = new(200, 200, 200, 30); // Low opacity light grey

                // Draw Vertical Lines (Moving Horizontal)
                for (int i = -1; i <= boxW / gridSize + 1; i++)
                {
                    float lineX = boxX + (i * gridSize) + offset;
                    if (lineX >= boxX && lineX <= boxX + boxW)
                    {
                        Raylib.DrawLine((int)lineX, boxY, (int)lineX, boxY + boxH, gridColor);
                    }
                }

                // Draw Horizontal Lines (Static)
                for (int j = 0; j <= boxH / gridSize; j++)
                {
                    int lineY = boxY + (j * gridSize);
                    Raylib.DrawLine(boxX, lineY, boxX + boxW, lineY, gridColor);
                }

                // 3. Border (Solid)
                Raylib.DrawRectangleLinesEx(boxRect, 1, Color.White);

                if (showOptionsMenu)
                {
                    // Draw Options Panel (Grid/BG only to Buffer)
                    DrawOptionsPanel(boxX + boxW + 5, boxY, fullBoxW - boxW - 5, boxH, gridSize, offset, gridColor);
                }

                // 1. Name Tag Box
                if (!string.IsNullOrEmpty(currentName))
                {
                    Vector2 nameSize = Raylib.MeasureTextEx(FontSmall, currentName, 12, 0);
                    int namePad = 4;
                    // +14 to account for the border and (namePad * 2) is for padding
                    Rectangle nameRect = new Rectangle(boxX, boxY - 18, nameSize.X + (namePad * 2), nameSize.Y + 3);

                    // Solid Black Background for Name
                    Raylib.DrawRectangleRec(nameRect, Color.Black);
                    Raylib.DrawRectangleLinesEx(nameRect, 1, Color.White);
                }

                // 2. [Z] Close Button Box (Top Right)
                int closeBtnW = 12;
                int closeBtnH = 12;
                int closeBtnX = boxX + boxW - closeBtnW - 4;
                int closeBtnY = boxY + 4;
                Rectangle closeBtnRect = new Rectangle(closeBtnX, closeBtnY, closeBtnW, closeBtnH);
                Raylib.DrawRectangleRec(closeBtnRect, Color.Black);
                Raylib.DrawRectangleLinesEx(closeBtnRect, 1, Color.White);

                // 3. Text Input Box
                if (showTextInput)
                {
                    int inputBoxY = boxY + 25;
                    int inputBoxH = 20;
                    Rectangle inputRect = new Rectangle(boxX + 8, inputBoxY, boxW - 16, inputBoxH);

                    // Input background
                    Raylib.DrawRectangleRec(inputRect, new Color(30, 30, 30, 255));
                    Raylib.DrawRectangleLinesEx(inputRect, 1, Color.Yellow);
                }

                Raylib.EndTextureMode();
            }

            // 3. Draw Buffer to Screen
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UIBuffer.Texture.Width, -UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

            // 4. Draw High-Res Text (Screen Space)
            if (IsOpen && hasPortrait)
            {
                float scaleX = (float)screenW / VirtualWidth;
                float scaleY = (float)screenH / VirtualHeight;
                float scale = Math.Min(scaleX, scaleY);

                // Centered Screen Offsets (if aspect ratio differs)
                float offX = (screenW - (VirtualWidth * scale)) / 2;
                float offY = (screenH - (VirtualHeight * scale)) / 2;

                // Re-calculate Text Positions based on Scale
                // Settings from TextureMode block
                int boxX = 10;
                int boxY = 110;
                int fullBoxW = 300;
                int boxW = showOptionsMenu ? (int)(fullBoxW * 0.75f) : fullBoxW;
                int boxH = 60;

                // 4.1 Draw Name
                if (!string.IsNullOrEmpty(currentName))
                {
                    int namePad = 4;
                    // Note: We are redrawing the Name Text here, the box behind it is in the buffer
                    // We need to calculate where the Name Box *is* on screen

                    float nameBoxY = boxY - 18;
                    Vector2 nameScreenPos = new Vector2(
                        offX + ((boxX + namePad) * scale),
                        offY + ((nameBoxY + 2.5f) * scale)
                    );

                    Raylib.DrawTextEx(FontHuge, currentName, nameScreenPos, 12 * scale, 0, Color.White);
                }

                // 4.2 Draw Close Hint [Z]
                int closeBtnX = boxX + boxW - 12 - 4; // 12 is btnW
                int closeBtnY = boxY + 4;
                Vector2 closeScreenPos = new Vector2(
                    offX + ((closeBtnX + 3) * scale),
                    offY + ((closeBtnY + 2) * scale)
                );
                Raylib.DrawTextEx(FontHuge, "Z", closeScreenPos, 10 * scale, 0, Color.White);

                // 4.3 Draw Main Dialogue
                int textStartX = boxX + 10;
                float textY = boxY + 8;

                string visibleText = currentText.Substring(0, charIndex);
                // TEXT WRAPPING WIDTH:
                // Decrease the constant (-20) to wrap sooner/tighten the text box.
                float maxTextWidth = boxW - (textStartX - boxX) - 58;

                // Recalculate wrapping for display (Logic duplicated roughly, but needed for drawing loops)
                // Ideally this state is cached, but for now we re-wrap.

                string[] words = visibleText.Split(' ');
                string drawBuffer = "";
                string lineBuffer = "";

                foreach (var word in words)
                {
                    string testLine = lineBuffer + word + " ";
                    // Measure using virtual size to match buffer layout logic
                    Vector2 size = Raylib.MeasureTextEx(FontSmall, testLine, 12, 0);

                    // TEXT WRAPPING RULE:
                    // If the current line width exceeds maxTextWidth, we start a new line.
                    // Tweak 'maxTextWidth' calculation above to adjust where the wrap happens.
                    if (size.X > maxTextWidth)
                    {
                        drawBuffer += lineBuffer.TrimEnd() + "\n";
                        lineBuffer = word + " ";
                    }
                    else
                    {
                        lineBuffer = testLine;
                    }
                }
                drawBuffer += lineBuffer.TrimEnd();

                if (isWaitingForAI)
                {
                    int dotCount = (int)thinkingDots + 1;
                    string dots = new string('.', dotCount);
                    Vector2 dotPos = new Vector2(offX + (textStartX * scale), offY + (textY * scale));
                    Raylib.DrawTextEx(FontHuge, dots, dotPos, 16 * scale, 1, Color.Yellow);
                }
                else if (showTextInput)
                {
                    // Input Text
                    int inputBoxX = boxX + 8;
                    int inputBoxY = boxY + 25;

                    string displayText = inputText;
                    if ((int)(Raylib.GetTime() * 2) % 2 == 0) displayText += "_";

                    // Truncate (Virtual measurement OK for logic logic)
                    float maxInputWidth = (boxW - 16) - 8;
                    while (Raylib.MeasureTextEx(FontTiny, displayText, 10, 0).X > maxInputWidth && displayText.Length > 1)
                    {
                        displayText = displayText.Substring(1);
                    }

                    Vector2 inputPos = new Vector2(offX + ((inputBoxX + 4) * scale), offY + ((inputBoxY + 5) * scale));
                    Raylib.DrawTextEx(FontHuge, displayText, inputPos, 10 * scale, 0, Color.White);

                    Vector2 hintPos = new Vector2(offX + ((boxX + 10) * scale), offY + ((boxY + 50) * scale));
                    Raylib.DrawTextEx(FontHuge, "ENTER to send | Z to cancel", hintPos, 10 * scale, 0, new Color(150, 150, 150, 255));
                }
                else
                {
                    Vector2 mainTextPos = new Vector2(offX + (textStartX * scale), offY + (textY * scale));
                    Raylib.DrawTextEx(FontHuge, drawBuffer, mainTextPos, 12 * scale, 1 * scale, Color.White);
                }

                // 4.4 Options Menu Text (if open)
                if (showOptionsMenu)
                {
                    int panelX = boxX + boxW + 5;
                    // Draw Options Panel Text
                    // Logic from DrawOptionsPanel - extracting text content drawing
                    int optionStartY = boxY + 8;
                    int optionSpacing = 16;

                    for (int i = 0; i < optionLabels.Length; i++)
                    {
                        bool isSelected = (i == optionSelection);
                        Color textColor = isSelected ? Color.Yellow : Color.White;
                        string label = optionLabels[i];
                        if (label.Length > 10) label = string.Concat(label.AsSpan(0, 8), "..");

                        int virtualTextX = panelX + 6;
                        int virtualTextY = optionStartY + (i * optionSpacing);

                        Vector2 pos = new Vector2(offX + (virtualTextX * scale), offY + (virtualTextY * scale));
                        Raylib.DrawTextEx(FontHuge, label, pos, 12 * scale, 1, textColor);

                        if (isSelected)
                        {
                            Vector2 arrowPos = new Vector2(offX + ((panelX + 2) * scale), offY + (virtualTextY * scale));
                            Raylib.DrawTextEx(FontHuge, ">", arrowPos, 12 * scale, 1, Color.Yellow);
                        }
                    }
                }

                // 4.5 When Debug is enabled, show Mood HUD (Top Right)
                if (TerminalSystem.DebugShowMood && currentDialogueNPC != null)
                {
                    string mood = currentDialogueNPC.CurrentMood;
                    string hudText = $"Mood: {mood}";

                    // Draw in top right of screen
                    Vector2 hudPos = new Vector2(screenW - (200 * scale), 20 * scale);

                    Raylib.DrawTextEx(FontHuge, hudText, hudPos, 20 * scale - 25, 0, Color.Yellow);
                }
            }
        }
    }
}

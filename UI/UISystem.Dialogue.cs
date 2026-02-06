using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Constants
        private const float TypeSpeed = 0.05f;
        private const int BoxX = 10;
        private const int BoxY = 110;
        private const int FullBoxW = 300;
        private const int BoxH = 60;
        private const int GridSize = 10;
        private const float ScrollSpeed = 5.0f;
        private const int NameTagPadding = 4;
        private const int CloseBtnSize = 12;
        private const int OptionSpacing = 16;
        private const int MaxInputLength = 100;

        // Dialogue State
        public static bool IsOpen = false;
        private static string currentText = "";
        private static string currentName = "";
        private static float typeTimer = 0f;
        private static int charIndex = 0;

        // Options Menu State
        private static bool showOptionsMenu = false;
        private static int optionSelection = 0;
        private static NPC? currentDialogueNPC = null;
        private static readonly string[] optionLabels = ["Respond", "Action", "Thoughts?"];

        // Visual Novel Character Portrait
        private static Texture2D currentPortrait;
        private static string currentPortraitPath = "";
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
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if (key >= 32 && key <= 126 && inputText.Length < MaxInputLength)
                {
                    inputText += (char)key;
                }
                key = Raylib.GetCharPressed();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && inputText.Length > 0)
            {
                inputText = inputText[..^1];
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) && inputText.Length > 0)
            {
                SubmitPlayerMessage(inputText);
                inputText = "";
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                showTextInput = false;
                inputText = "";
                showOptionsMenu = true;
            }
        }

        private static void SubmitPlayerMessage(string message)
        {
            if (currentDialogueNPC == null) return;

            if (currentDialogueNPC.CharacterData != null)
            {
                isWaitingForAI = true;
                thinkingDots = 0f;
                pendingAITask = currentDialogueNPC.GetAIResponseAsync(message);
            }
            else
            {
                ShowStaticResponse("(No AI response available)");
            }
        }

        private static void ShowStaticResponse(string text)
        {
            showTextInput = false;
            currentText = text;
            charIndex = 0;
            typeTimer = 0f;
        }

        private static void HandleOptionSelection(int selection)
        {
            showOptionsMenu = false;

            switch (selection)
            {
                case 0: // Respond
                    showTextInput = true;
                    inputText = "";
                    currentText = "Type your message...";
                    charIndex = currentText.Length;
                    break;

                case 1: // Action
                    ShowStaticResponse("(Actions not yet implemented)");
                    break;

                case 2: // What's on your Mind?
                    if (currentDialogueNPC?.CharacterData != null)
                    {
                        isWaitingForAI = true;
                        thinkingDots = 0f;
                        pendingAITask = currentDialogueNPC.GetAIResponseAsync("What's on your mind right now?");
                    }
                    else
                    {
                        ShowStaticResponse("Nothing much...");
                    }
                    break;
            }
        }

        private static void DrawGrid(int x, int y, int w, int h, float offset, Color gridColor)
        {
            // Vertical lines (scrolling)
            for (int i = -1; i <= w / GridSize + 1; i++)
            {
                float lineX = x + (i * GridSize) + offset;
                if (lineX >= x && lineX <= x + w)
                {
                    Raylib.DrawLine((int)lineX, y, (int)lineX, y + h, gridColor);
                }
            }

            // Horizontal lines (static)
            for (int j = 0; j <= h / GridSize; j++)
            {
                int lineY = y + (j * GridSize);
                Raylib.DrawLine(x, lineY, x + w, lineY, gridColor);
            }
        }

        private static int GetBoxWidth() => showOptionsMenu ? (int)(FullBoxW * 0.75f) : FullBoxW;

        private static string WrapText(string text, float maxWidth)
        {
            string[] words = text.Split(' ');
            string result = "";
            string lineBuffer = "";

            foreach (var word in words)
            {
                string testLine = lineBuffer + word + " ";
                Vector2 size = Raylib.MeasureTextEx(FontSmall, testLine, 12, 0);

                if (size.X > maxWidth)
                {
                    result += lineBuffer.TrimEnd() + "\n";
                    lineBuffer = word + " ";
                }
                else
                {
                    lineBuffer = testLine;
                }
            }

            return result + lineBuffer.TrimEnd();
        }

        private static void DrawPortrait(int screenW, int screenH)
        {
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 100));

            float scale = currentPortrait.Height > screenH * 1.5f ? (float)screenH / currentPortrait.Height : 1.0f;
            float scaledW = currentPortrait.Width * scale;
            float scaledH = currentPortrait.Height * scale;
            float posX = (screenW - scaledW) / 2;
            float posY = (screenH * 0.8f) - (scaledH / 1.8f);

            Raylib.DrawTextureEx(currentPortrait, new Vector2(posX, posY), 0f, scale, Color.White);
        }

        private static void DrawDialogueBuffer()
        {
            int boxW = GetBoxWidth();
            float offset = ((float)Raylib.GetTime() * ScrollSpeed) % GridSize;
            Color gridColor = new(200, 200, 200, 30);

            Raylib.BeginTextureMode(UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            // Main dialogue box
            Rectangle boxRect = new Rectangle(BoxX, BoxY, boxW, BoxH);
            Raylib.DrawRectangleRec(boxRect, new Color(0, 0, 0, 220));
            DrawGrid(BoxX, BoxY, boxW, BoxH, offset, gridColor);
            Raylib.DrawRectangleLinesEx(boxRect, 1, Color.White);

            // Options panel
            if (showOptionsMenu)
            {
                int panelX = BoxX + boxW + 5;
                int panelW = FullBoxW - boxW - 5;
                Rectangle panelRect = new Rectangle(panelX, BoxY, panelW, BoxH);
                Raylib.DrawRectangleRec(panelRect, new Color(0, 0, 0, 220));
                DrawGrid(panelX, BoxY, panelW, BoxH, offset, gridColor);
                Raylib.DrawRectangleLinesEx(panelRect, 1, Color.White);
            }

            // Name tag
            if (!string.IsNullOrEmpty(currentName))
            {
                Vector2 nameSize = Raylib.MeasureTextEx(FontSmall, currentName, 12, 0);
                Rectangle nameRect = new Rectangle(BoxX, BoxY - 18, nameSize.X + (NameTagPadding * 2), nameSize.Y + 3);
                Raylib.DrawRectangleRec(nameRect, Color.Black);
                Raylib.DrawRectangleLinesEx(nameRect, 1, Color.White);
            }

            // Close button
            int closeBtnX = BoxX + boxW - CloseBtnSize - 4;
            int closeBtnY = BoxY + 4;
            Rectangle closeBtnRect = new Rectangle(closeBtnX, closeBtnY, CloseBtnSize, CloseBtnSize);
            Raylib.DrawRectangleRec(closeBtnRect, Color.Black);
            Raylib.DrawRectangleLinesEx(closeBtnRect, 1, Color.White);

            // Text input box
            if (showTextInput)
            {
                Rectangle inputRect = new Rectangle(BoxX + 8, BoxY + 25, boxW - 16, 20);
                Raylib.DrawRectangleRec(inputRect, new Color(30, 30, 30, 255));
                Raylib.DrawRectangleLinesEx(inputRect, 1, Color.Yellow);
            }

            Raylib.EndTextureMode();
        }

        public static void DrawDialogue()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            if (hasPortrait)
            {
                DrawPortrait(screenW, screenH);
                DrawDialogueBuffer();

                Rectangle dest = new Rectangle(0, 0, screenW, screenH);
                Rectangle flipSrc = new Rectangle(0, 0, UIBuffer.Texture.Width, -UIBuffer.Texture.Height);
                Raylib.DrawTexturePro(UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

                DrawTextContent(screenW, screenH);
            }
        }

        private static void DrawTextContent(int screenW, int screenH)
        {
            float scaleX = (float)screenW / VirtualWidth;
            float scaleY = (float)screenH / VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (VirtualWidth * scale)) / 2;
            float offY = (screenH - (VirtualHeight * scale)) / 2;

            int boxW = GetBoxWidth();

            // Name tag
            if (!string.IsNullOrEmpty(currentName))
            {
                Vector2 nameScreenPos = new Vector2(
                    offX + ((BoxX + NameTagPadding) * scale),
                    offY + ((BoxY - 18 + 2.5f) * scale)
                );
                Raylib.DrawTextEx(FontHuge, currentName, nameScreenPos, 12 * scale, 0, Color.White);
            }

            // Close button
            int closeBtnX = BoxX + boxW - CloseBtnSize - 4;
            Vector2 closeScreenPos = new Vector2(
                offX + ((closeBtnX + 3) * scale),
                offY + ((BoxY + 4 + 2) * scale)
            );
            Raylib.DrawTextEx(FontHuge, "Z", closeScreenPos, 10 * scale, 0, Color.White);

            // Main content
            if (isWaitingForAI)
            {
                DrawAIThinking(offX, offY, scale);
            }
            else if (showTextInput)
            {
                DrawTextInputContent(boxW, offX, offY, scale);
            }
            else
            {
                DrawMainDialogue(boxW, offX, offY, scale);
            }

            // Options menu
            if (showOptionsMenu)
            {
                DrawOptionsMenuText(boxW, offX, offY, scale);
            }

            // Mood HUD
            if (TerminalSystem.DebugShowMood && currentDialogueNPC != null)
            {
                string hudText = $"Mood: {currentDialogueNPC.CurrentMood}";
                Vector2 hudPos = new Vector2(screenW - (200 * scale), 20 * scale);
                Raylib.DrawTextEx(FontHuge, hudText, hudPos, 20 * scale - 25, 0, Color.Yellow);
            }
        }

        private static void DrawAIThinking(float offX, float offY, float scale)
        {
            int dotCount = (int)thinkingDots + 1;
            string dots = new string('.', dotCount);
            Vector2 dotPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 8) * scale));
            Raylib.DrawTextEx(FontHuge, dots, dotPos, 16 * scale, 1, Color.Yellow);
        }

        private static void DrawTextInputContent(int boxW, float offX, float offY, float scale)
        {
            string displayText = inputText;
            if ((int)(Raylib.GetTime() * 2) % 2 == 0) displayText += "_";

            float maxInputWidth = (boxW - 16) - 8;
            while (Raylib.MeasureTextEx(FontTiny, displayText, 10, 0).X > maxInputWidth && displayText.Length > 1)
            {
                displayText = displayText.Substring(1);
            }

            Vector2 inputPos = new Vector2(offX + ((BoxX + 12) * scale), offY + ((BoxY + 30) * scale));
            Raylib.DrawTextEx(FontHuge, displayText, inputPos, 10 * scale, 0, Color.White);

            Vector2 hintPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 50) * scale));
            Raylib.DrawTextEx(FontHuge, "ENTER to send | Z to cancel", hintPos, 10 * scale, 0, new Color(150, 150, 150, 255));
        }

        private static void DrawMainDialogue(int boxW, float offX, float offY, float scale)
        {
            string visibleText = currentText.Substring(0, charIndex);
            float maxTextWidth = boxW - 10 - 58;
            string wrappedText = WrapText(visibleText, maxTextWidth);

            Vector2 mainTextPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 8) * scale));
            Raylib.DrawTextEx(FontHuge, wrappedText, mainTextPos, 12 * scale, 1 * scale, Color.White);
        }

        private static void DrawOptionsMenuText(int boxW, float offX, float offY, float scale)
        {
            int panelX = BoxX + boxW + 5;
            int optionStartY = BoxY + 8;

            for (int i = 0; i < optionLabels.Length; i++)
            {
                bool isSelected = (i == optionSelection);
                Color textColor = isSelected ? Color.Yellow : Color.White;
                string label = optionLabels[i].Length > 10 ? string.Concat(optionLabels[i].AsSpan(0, 8), "..") : optionLabels[i];

                int virtualTextY = optionStartY + (i * OptionSpacing);
                Vector2 pos = new Vector2(offX + ((panelX + 6) * scale), offY + (virtualTextY * scale));
                Raylib.DrawTextEx(FontHuge, label, pos, 12 * scale, 1, textColor);

                if (isSelected)
                {
                    Vector2 arrowPos = new Vector2(offX + ((panelX + 2) * scale), offY + (virtualTextY * scale));
                    Raylib.DrawTextEx(FontHuge, ">", arrowPos, 12 * scale, 1, Color.Yellow);
                }
            }
        }
    }
}

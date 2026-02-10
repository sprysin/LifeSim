using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Dialogue State
        public static bool IsOpen = false;
        private static string currentText = "";
        private static string currentName = "";
        private static float typeTimer = 0f;
        private static int charIndex = 0;

        // Chat Log State
        private static bool showChatLog = false;
        private static List<(string Name, string Text)> conversationHistory = new List<(string Name, string Text)>();
        private static float chatLogScroll = 0f;

        // Options Menu State
        private static bool showOptionsMenu = false;
        private static int optionSelection = 0;
        private static NPC? currentDialogueNPC = null;
        private static readonly string[] optionLabels = ["Respond", "Action", "Thoughts?"];

        // Visual Novel Character Portrait
        private static Texture2D currentPortrait;
        private static string currentPortraitPath = "";
        private static bool hasPortrait = false;
        private static float moodShakeTimer = 0f;

        // Text Input State
        private static bool showTextInput = false;
        private static string inputText = "";
        private static int inputCursorIndex = 0;
        private static bool isWaitingForAI = false;
        private static float thinkingDots = 0f;
        private static Task<string?>? pendingAITask = null;
        private static bool isActionMode = false; // Track if input is an action

        // Text Layout Constants (High Res 1600x900)
        private const int DialogueFontSize = 32;
        private const float TextSpacing = 2.5f;
        private const int MaxTextWidth = 1100; // 1600 * 0.75 - Padding
        private const int MaxLinesPerBox = 4;
        private const int MaxInputLength = 255;
        private const int LineHeightPadding = 8;

        public static void OpenDialogue(string name, string text, string portraitPath = "")
        {
            OpenDialogue(name, text, portraitPath, null);
        }

        public static void OpenDialogue(string name, string text, string portraitPath, NPC? npc)
        {
            // If opening fresh (not advancing conversation), ensure history is clear if needed
            // But usually OpenDialogue is called for each line. 
            // We only want to clear history when the *session* starts.
            // Since we don't have a distinct "StartSession", we'll rely on CloseDialogue to clear it.

            IsOpen = true;
            currentName = name;

            // Centralize text setting logic
            SetDialogueText(text, npc);

            // Reset options menu state
            showOptionsMenu = false;
            optionSelection = 0;
            currentDialogueNPC = npc;

            // Load portrait if provided
            if (!string.IsNullOrEmpty(portraitPath) && System.IO.File.Exists(portraitPath))
            {
                if (hasPortrait)
                {
                    // Check if path changed for mood shake
                    if (currentPortraitPath != portraitPath)
                    {
                        moodShakeTimer = MoodShakeDuration;
                    }
                    Raylib.UnloadTexture(currentPortrait);
                }
                else
                {
                    // First load, maybe shake?
                    moodShakeTimer = MoodShakeDuration;
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

        private static void SetDialogueText(string text, NPC? npc)
        {
            // Record to history
            if (!string.IsNullOrEmpty(currentName))
            {
                // Check if the last entry is the same to avoid duplicates if SetDialogueText is called multiple times for same text
                conversationHistory.Add((currentName, text));
            }

            // Splitting Logic
            List<string> pages = SplitTextIntoPages(text, MaxTextWidth);

            if (pages.Count > 0)
            {
                currentText = pages[0];

                // If there are more pages, prepend them to the NPC's conversation queue
                if (pages.Count > 1 && npc != null)
                {
                    npc.PrependConversation(pages.Skip(1));
                }
            }
            else
            {
                currentText = "";
            }

            charIndex = 0;
            typeTimer = 0f;
        }

        public static void CloseDialogue(bool preserveState = false)
        {
            IsOpen = false;

            if (!preserveState)
            {
                conversationHistory.Clear();
            }

            showChatLog = false;

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
                        SetDialogueText(response, currentDialogueNPC);
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

            // Update Mood Shake
            if (moodShakeTimer > 0)
            {
                moodShakeTimer -= Raylib.GetFrameTime();
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

                    // Trigger Shake
                    moodShakeTimer = MoodShakeDuration;
                }
            }
        }

        public static void HandleInput()
        {
            // Chat Log Toggle
            if (Raylib.IsKeyPressed(KeyboardKey.C) && !showTextInput)
            {
                showChatLog = !showChatLog;
                if (showChatLog)
                {
                    // Scroll to bottom when opening
                    // We need to calculate content height first to know where bottom is.
                    // For now, set a high number, clamping will handle it in Draw
                    chatLogScroll = 100000;
                }
                return;
            }

            // Chat Log Input
            if (showChatLog)
            {
                // Scrolling
                float wheel = Raylib.GetMouseWheelMove();
                if (wheel != 0)
                {
                    chatLogScroll -= wheel * 20; // Scroll speed
                }

                // checking logic below:
                if (Raylib.IsKeyPressed(KeyboardKey.C))
                {
                    showChatLog = false;
                }

                return;
            }

            // Don't process other input why chatting
            if (isWaitingForAI) return;


            // Text Input Mode
            if (showTextInput)
            {
                HandleTextInput();
                return;
            }

            // Follow Logic - Allow at any point in dialogue as long as not typing
            if (Raylib.IsKeyPressed(KeyboardKey.V))
            {
                if (currentDialogueNPC != null)
                {
                    Console.WriteLine($"[UiSystem] Starting Follow: {currentDialogueNPC.Name}");
                    currentDialogueNPC.SetFollow(true);
                    CloseDialogue(preserveState: true);
                    return;
                }
            }

            if (showOptionsMenu)
            {
                // Mouse Interaction
                Vector2 mousePos = Raylib.GetMousePosition();

                if (Raylib.CheckCollisionPointRec(mousePos, OptionRespondRect))
                {
                    optionSelection = 0;
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left)) HandleOptionSelection(0);
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, OptionActionRect))
                {
                    optionSelection = 1;
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left)) HandleOptionSelection(1);
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, OptionThoughtsRect))
                {
                    optionSelection = 2;
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left)) HandleOptionSelection(2);
                }

                // Options menu navigation (Keyboard)
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
                // Allow Mouse Click or X to advance
                if (Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsMouseButtonPressed(MouseButton.Left))
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

                // Follow Logic
                if (Raylib.IsKeyPressed(KeyboardKey.V))
                {
                    Console.WriteLine($"[UiSystem] V Pressed. NPC: {currentDialogueNPC?.Name ?? "null"}");
                    if (currentDialogueNPC != null)
                    {
                        currentDialogueNPC.SetFollow(true);
                        CloseDialogue();
                    }
                }
            }
        }

        private static void HandleTextInput()
        {
            // Cursor Navigation
            if (Raylib.IsKeyPressed(KeyboardKey.Left))
            {
                inputCursorIndex--;
                if (inputCursorIndex < 0) inputCursorIndex = 0;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Right))
            {
                inputCursorIndex++;
                if (inputCursorIndex > inputText.Length) inputCursorIndex = inputText.Length;
            }

            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if (key >= 32 && key <= 126 && inputText.Length < MaxInputLength)
                {
                    inputText = inputText.Insert(inputCursorIndex, ((char)key).ToString());
                    inputCursorIndex++;
                }
                key = Raylib.GetCharPressed();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && inputText.Length > 0 && inputCursorIndex > 0)
            {
                inputText = inputText.Remove(inputCursorIndex - 1, 1);
                inputCursorIndex--;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Enter) && inputText.Length > 0)
            {
                SubmitPlayerMessage(inputText);
                inputText = "";
                inputCursorIndex = 0;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                showTextInput = false;
                inputText = "";
                inputCursorIndex = 0;
                showOptionsMenu = true;
            }
        }

        private static void SubmitPlayerMessage(string message)
        {
            if (currentDialogueNPC == null) return;

            // Wrap message in asterisks if in action mode
            string finalMessage = isActionMode ? $"*{message}*" : message;

            // Add to history
            conversationHistory.Add(("Player", finalMessage));

            if (currentDialogueNPC.CharacterData != null)
            {
                isWaitingForAI = true;
                thinkingDots = 0f;
                pendingAITask = currentDialogueNPC.GetAIResponseAsync(finalMessage);
            }
            else
            {
                ShowStaticResponse("(No AI response available)");
            }

            isActionMode = false; // Reset mode after submit
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
                    inputCursorIndex = 0;
                    isActionMode = false;
                    currentText = "Type your message...";
                    charIndex = currentText.Length;
                    break;

                case 1: // Action
                    showTextInput = true;
                    inputText = "";
                    inputCursorIndex = 0;
                    isActionMode = true;
                    currentText = "Type your action...";
                    charIndex = currentText.Length;
                    break;

                case 2: // What's on your Mind? this feature allows the NPC to continue speaking without player input
                    if (currentDialogueNPC?.CharacterData != null)
                    {
                        isWaitingForAI = true;
                        thinkingDots = 0f;
                        pendingAITask = currentDialogueNPC.GetAIResponseAsync("*Continue speaking on what you were just saying, or whatever else is on your mind. the player did not speak, you are doing this on your own accord*");
                    }
                    else
                    {
                        ShowStaticResponse("Nothing much...");
                    }
                    break;
            }
        }

        private static List<string> SplitTextIntoPages(string text, float maxWidth)
        {
            List<string> pages = new List<string>();
            string[] words = text.Split(' ');
            string currentPage = "";
            string currentLine = "";
            float fontSize = DialogueFontSize; // Use Constant
            float spacing = TextSpacing;
            int maxLines = MaxLinesPerBox; // Use Constant
            int currentLineCount = 1;

            foreach (var word in words)
            {
                string testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
                // Use FontMedium for Dialogue Text
                Vector2 size = Raylib.MeasureTextEx(FontMedium, testLine, fontSize, spacing);

                if (size.X > maxWidth)
                {
                    // Line full, push current line to page
                    currentPage += currentLine + "\n";
                    currentLineCount++;

                    if (currentLineCount > maxLines)
                    {
                        // Page full, push page and start new one
                        pages.Add(currentPage);
                        currentPage = "";
                        currentLineCount = 1;
                    }

                    currentLine = word; // Start new line with current word
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Add remaining content
            if (!string.IsNullOrEmpty(currentLine) || !string.IsNullOrEmpty(currentPage))
            {
                if (!string.IsNullOrEmpty(currentLine)) currentPage += currentLine;
                pages.Add(currentPage);
            }

            return pages;
        }
    }
}

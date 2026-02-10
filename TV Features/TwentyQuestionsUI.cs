using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static class TwentyQuestionsUI
    {
        // Input state
        private static string inputText = "";
        private static int inputCursorIndex = 0;
        private static bool isGuessMode = false; // false = Ask, true = Answer
        private static float thinkingDots = 0f;

        // Scroll state for question log
        private static float logScroll = 0f;

        // Role selection state
        private static int roleSelection = 0; // 0 = Ask Questions, 1 = Answer Questions

        // Result screen: cached portrait
        private static Texture2D resultPortrait;
        private static bool resultPortraitLoaded = false;

        // Dialogue box constants (matching UISystem.Dialogue.Draw.cs)
        private const int BoxX = 10;
        private const int BoxY = 110;
        private const int FullBoxW = 300;
        private const int BoxH = 60;
        private const int GridSize = 10;
        private const float ScrollSpeed = 5.0f;
        private const int NameTagPadding = 4;
        private const float TypeSpeed = 0.04f;

        public static void Reset()
        {
            inputText = "";
            inputCursorIndex = 0;
            isGuessMode = false;
            thinkingDots = 0f;
            logScroll = 0f;
            roleSelection = 0;

            // Unload cached portrait
            if (resultPortraitLoaded)
            {
                Raylib.UnloadTexture(resultPortrait);
                resultPortraitLoaded = false;
            }
        }

        public static void Draw(TwentyQuestionsGame game)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Draw dark overlay behind everything
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 200));

            if (game.Phase == TwentyQuestionsGame.GamePhase.RoleSelect)
            {
                DrawRoleSelect(game, screenW, screenH);
                return;
            }

            if (game.Phase == TwentyQuestionsGame.GamePhase.ShowingResult)
            {
                DrawResultScreen(game, screenW, screenH);
                return;
            }

            // Transitional phases (waiting for AI reaction) — show game greyed out with loading
            if (game.Phase == TwentyQuestionsGame.GamePhase.PlayerWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.NPCWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.GaveUp)
            {
                DrawGameBoard(game, screenW, screenH);
                // Draw extra grey overlay on top
                Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 150));
                DrawLoadingReaction(game, screenW, screenH);
                return;
            }

            DrawGameBoard(game, screenW, screenH);
        }

        private static void DrawGameBoard(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // Draw UI buffer (boxes)
            DrawUIBuffer(game);

            // Blit buffer to screen
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UISystem.UIBuffer.Texture.Width, -UISystem.UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UISystem.UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

            // Draw text content at screen resolution
            DrawTextContent(game, screenW, screenH);
        }

        private static void DrawLoadingReaction(TwentyQuestionsGame game, int screenW, int screenH)
        {
            float scaleX = (float)screenW / UISystem.VirtualWidth;
            float scaleY = (float)screenH / UISystem.VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (UISystem.VirtualWidth * scale)) / 2;
            float offY = (screenH - (UISystem.VirtualHeight * scale)) / 2;

            // Status message centered
            thinkingDots += Raylib.GetFrameTime() * 3f;
            if (thinkingDots > 3f) thinkingDots = 0f;
            string dots = new string('.', (int)thinkingDots + 1);
            string msg = $"{game.StatusMessage}  {game.Npc.Name} reacting{dots}";

            Vector2 msgSize = Raylib.MeasureTextEx(UISystem.FontHuge, msg, 10 * scale, 0);
            Vector2 msgPos = new Vector2((screenW - msgSize.X) / 2, (screenH - msgSize.Y) / 2);
            Raylib.DrawTextEx(UISystem.FontHuge, msg, msgPos, 10 * scale, 0, Color.Yellow);
        }

        private static void DrawResultScreen(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // 1. Draw the greyed-out game board behind
            DrawGameBoard(game, screenW, screenH);
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 150));

            // 2. Draw NPC portrait (Visual Novel style, matching dialogue system)
            if (!resultPortraitLoaded)
            {
                string portraitPath = game.Npc.GetCurrentPortraitPath();
                if (!string.IsNullOrEmpty(portraitPath) && System.IO.File.Exists(portraitPath))
                {
                    resultPortrait = Raylib.LoadTexture(portraitPath);
                    resultPortraitLoaded = true;
                }
            }

            if (resultPortraitLoaded)
            {
                // Dim overlay for portrait background (matching DrawPortrait)
                Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 100));

                float portraitScale = resultPortrait.Height > screenH * 1.5f
                    ? (float)screenH / resultPortrait.Height : 1.0f;
                float scaledW = resultPortrait.Width * portraitScale;
                float scaledH = resultPortrait.Height * portraitScale;
                float posX = (screenW - scaledW) / 2;
                float posY = (screenH * 0.8f) - (scaledH / 1.8f);

                Raylib.DrawTextureEx(resultPortrait, new Vector2(posX, posY), 0f, portraitScale, Color.White);
            }

            // 3. Draw dialogue box (matching UISystem.DrawDialogueBuffer style)
            float offset = ((float)Raylib.GetTime() * ScrollSpeed) % GridSize;
            Color gridColor = new(200, 200, 200, 30);

            Raylib.BeginTextureMode(UISystem.UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            // Main dialogue box
            Rectangle boxRect = new Rectangle(BoxX, BoxY, FullBoxW, BoxH);
            Raylib.DrawRectangleRec(boxRect, new Color(0, 0, 0, 220));
            UISystem.DrawGrid(BoxX, BoxY, FullBoxW, BoxH, offset, gridColor);
            Raylib.DrawRectangleLinesEx(boxRect, 1, Color.White);

            // Name tag
            string npcName = game.Npc.Name;
            Vector2 nameSize = Raylib.MeasureTextEx(UISystem.FontSmall, npcName, 12, 0);
            Rectangle nameRect = new Rectangle(BoxX, BoxY - 18, nameSize.X + (NameTagPadding * 2), nameSize.Y + 3);
            Raylib.DrawRectangleRec(nameRect, Color.Black);
            Raylib.DrawRectangleLinesEx(nameRect, 1, Color.White);

            Raylib.EndTextureMode();

            // Blit buffer
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UISystem.UIBuffer.Texture.Width, -UISystem.UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UISystem.UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

            // 4. Draw text content at screen resolution
            float scaleX = (float)screenW / UISystem.VirtualWidth;
            float scaleY = (float)screenH / UISystem.VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (UISystem.VirtualWidth * scale)) / 2;
            float offY = (screenH - (UISystem.VirtualHeight * scale)) / 2;

            // Name tag text
            Vector2 nameTextPos = new Vector2(offX + ((BoxX + NameTagPadding) * scale), offY + ((BoxY - 16) * scale));
            Raylib.DrawTextEx(UISystem.FontHuge, npcName, nameTextPos, 10 * scale, 0, Color.White);

            // Result header (Top Band)
            string resultHeader = game.ResultOutcome switch
            {
                TwentyQuestionsGame.GamePhase.PlayerWin => $"VICTORY! Secret word: {game.SecretWord}",
                TwentyQuestionsGame.GamePhase.NPCWin => $"{game.Npc.Name} WINS! Secret word: {game.SecretWord}",
                TwentyQuestionsGame.GamePhase.GaveUp => $"Gave Up! Secret word: {game.SecretWord}",
                _ => ""
            };

            // Top Band Background
            float bandHeight = 40 * scale;
            Raylib.DrawRectangle(0, 0, screenW, (int)bandHeight, new Color(0, 0, 0, 200));
            Raylib.DrawRectangleLinesEx(new Rectangle(-2, -2, screenW + 4, bandHeight + 2), 2, Color.White);

            // Centered Header Text (YELLOW)
            Vector2 headerSize = Raylib.MeasureTextEx(UISystem.FontHuge, resultHeader, 12 * scale, 1);
            Vector2 headerPos = new Vector2(
                (screenW - headerSize.X) / 2,
                (bandHeight - headerSize.Y) / 2
            );
            Raylib.DrawTextEx(UISystem.FontHuge, resultHeader, headerPos, 12 * scale, 1, Color.Yellow);

            // Typewriter text for NPC reaction inside the dialogue box
            float fontSize = 10 * scale;
            float spacing = 1 * scale;
            float maxTextWidth = (FullBoxW - 20) * scale;
            float lineHeight = fontSize + 2 * scale;

            // Advance typewriter
            game.ResultTypeTimer += Raylib.GetFrameTime();
            if (game.ResultTypeTimer >= TypeSpeed)
            {
                game.ResultTypeTimer = 0f;
                if (game.ResultCharIndex < game.ResultReaction.Length)
                {
                    game.ResultCharIndex++;
                }
            }

            string visibleText = game.ResultReaction.Substring(0, game.ResultCharIndex);
            List<string> lines = WrapText(visibleText, fontSize, spacing, maxTextWidth);

            Vector2 textStart = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 8) * scale));

            for (int i = 0; i < lines.Count; i++)
            {
                Vector2 linePos = new Vector2(textStart.X, textStart.Y + i * lineHeight);
                Raylib.DrawTextEx(UISystem.FontHuge, lines[i], linePos, fontSize, spacing, Color.White);
            }

            // Dismiss hint at bottom
            bool fullyRevealed = game.ResultCharIndex >= game.ResultReaction.Length;
            if (fullyRevealed)
            {
                string hint = "Press X to continue";
                Vector2 hintSize = Raylib.MeasureTextEx(UISystem.FontHuge, hint, 8 * scale, 0);
                Vector2 hintPos = new Vector2(
                    offX + ((BoxX + FullBoxW) * scale) - hintSize.X - 5 * scale,
                    offY + ((BoxY + BoxH - 12) * scale)
                );
                Raylib.DrawTextEx(UISystem.FontHuge, hint, hintPos, 8 * scale, 0, new Color(150, 150, 150, 255));
            }
        }

        private static void DrawRoleSelect(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // Draw UI buffer for role selection panel
            Raylib.BeginTextureMode(UISystem.UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            int panelW = 160;
            int panelH = 90;
            int panelX = (UISystem.VirtualWidth - panelW) / 2;
            int panelY = (UISystem.VirtualHeight - panelH) / 2;

            // Panel background
            Rectangle panelRect = new Rectangle(panelX, panelY, panelW, panelH);
            Raylib.DrawRectangleRec(panelRect, new Color(0, 0, 0, 230));
            Raylib.DrawRectangleLinesEx(panelRect, 1, Color.White);

            Raylib.EndTextureMode();

            // Blit buffer
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UISystem.UIBuffer.Texture.Width, -UISystem.UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UISystem.UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

            // Draw text at screen resolution
            float scaleX = (float)screenW / UISystem.VirtualWidth;
            float scaleY = (float)screenH / UISystem.VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (UISystem.VirtualWidth * scale)) / 2;
            float offY = (screenH - (UISystem.VirtualHeight * scale)) / 2;

            // Title
            string title = "20 Questions";
            Vector2 titleSize = Raylib.MeasureTextEx(UISystem.FontHuge, title, 12 * scale, 0);
            Vector2 titlePos = new Vector2(
                offX + ((panelX + panelW / 2) * scale) - titleSize.X / 2,
                offY + ((panelY + 8) * scale)
            );
            Raylib.DrawTextEx(UISystem.FontHuge, title, titlePos, 12 * scale, 0, Color.Yellow);

            // Subtitle
            string subtitle = $"Playing with {game.Npc.Name}";
            Vector2 subSize = Raylib.MeasureTextEx(UISystem.FontHuge, subtitle, 8 * scale, 0);
            Vector2 subPos = new Vector2(
                offX + ((panelX + panelW / 2) * scale) - subSize.X / 2,
                offY + ((panelY + 24) * scale)
            );
            Raylib.DrawTextEx(UISystem.FontHuge, subtitle, subPos, 8 * scale, 0, Color.White);

            // Role options
            string[] roleOptions = { "Ask Questions (Guesser)", "Answer Questions (Coming Soon)" };
            float optionStartY = panelY + 42;

            for (int i = 0; i < roleOptions.Length; i++)
            {
                bool selected = (i == roleSelection);
                string prefix = selected ? "> " : "  ";
                string label = prefix + roleOptions[i];
                Color col = selected ? Color.White : Color.Gray;

                // Dim option 1 since it's coming soon
                if (i == 1) col = new Color(80, 80, 80, 255);

                Vector2 optPos = new Vector2(
                    offX + ((panelX + 15) * scale),
                    offY + ((optionStartY + i * 16) * scale)
                );
                Raylib.DrawTextEx(UISystem.FontHuge, label, optPos, 9 * scale, 0, col);
            }

            // Hint
            string hint = "X:Select  ESC:Back";
            Vector2 hintSize = Raylib.MeasureTextEx(UISystem.FontHuge, hint, 7 * scale, 0);
            Vector2 hintPos = new Vector2(
                offX + ((panelX + panelW / 2) * scale) - hintSize.X / 2,
                offY + ((panelY + panelH - 12) * scale)
            );
            Raylib.DrawTextEx(UISystem.FontHuge, hint, hintPos, 7 * scale, 0, new Color(120, 120, 120, 255));
        }

        private static void DrawUIBuffer(TwentyQuestionsGame game)
        {
            Raylib.BeginTextureMode(UISystem.UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            // Header bar (top)
            int headerX = 5, headerY = 3, headerW = UISystem.VirtualWidth - 10, headerH = 18;
            Rectangle headerRect = new Rectangle(headerX, headerY, headerW, headerH);
            Raylib.DrawRectangleRec(headerRect, new Color(0, 0, 0, 230));
            Raylib.DrawRectangleLinesEx(headerRect, 1, Color.White);

            // Question log area (middle)
            int logX = 5, logY = 24, logW = UISystem.VirtualWidth - 10, logH = 100;
            Rectangle logRect = new Rectangle(logX, logY, logW, logH);
            Raylib.DrawRectangleRec(logRect, new Color(0, 0, 0, 200));
            Raylib.DrawRectangleLinesEx(logRect, 1, Color.White);

            // Bottom input panel
            int inputPanelX = 5, inputPanelY = 127, inputPanelW = UISystem.VirtualWidth - 10, inputPanelH = 48;
            Rectangle inputPanelRect = new Rectangle(inputPanelX, inputPanelY, inputPanelW, inputPanelH);
            Raylib.DrawRectangleRec(inputPanelRect, new Color(0, 0, 0, 230));
            Raylib.DrawRectangleLinesEx(inputPanelRect, 1, Color.White);

            // Input text box (inside bottom panel)
            if (game.Phase == TwentyQuestionsGame.GamePhase.WaitingForQuestion)
            {
                Rectangle inputRect = new Rectangle(inputPanelX + 5, inputPanelY + 5, inputPanelW - 12, 18);
                Raylib.DrawRectangleRec(inputRect, new Color(30, 30, 30, 255));
                Color borderColor = isGuessMode ? Color.Green : Color.Yellow;
                Raylib.DrawRectangleLinesEx(inputRect, 1, borderColor);
            }

            Raylib.EndTextureMode();
        }

        private static void DrawTextContent(TwentyQuestionsGame game, int screenW, int screenH)
        {
            float scaleX = (float)screenW / UISystem.VirtualWidth;
            float scaleY = (float)screenH / UISystem.VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (UISystem.VirtualWidth * scale)) / 2;
            float offY = (screenH - (UISystem.VirtualHeight * scale)) / 2;

            // Header text
            string headerLeft = $"{game.Npc.Name} — 20 Questions";
            string headerRight = $"Q: {game.QuestionsAsked}/{TwentyQuestionsGame.MaxQuestions}";

            Vector2 headerPos = new Vector2(offX + (10 * scale), offY + (6 * scale));
            Raylib.DrawTextEx(UISystem.FontHuge, headerLeft, headerPos, 10 * scale, 0, Color.Yellow);

            Vector2 rightSize = Raylib.MeasureTextEx(UISystem.FontHuge, headerRight, 10 * scale, 0);
            Vector2 headerRightPos = new Vector2(offX + ((UISystem.VirtualWidth - 10) * scale) - rightSize.X, offY + (6 * scale));
            Raylib.DrawTextEx(UISystem.FontHuge, headerRight, headerRightPos, 10 * scale, 0, Color.White);

            // Question log content
            DrawQuestionLog(game, offX, offY, scale);

            // Bottom panel content
            DrawBottomPanel(game, offX, offY, scale);
        }

        /// Wraps text to fit within maxWidth pixels, returning a list of lines.
        private static List<string> WrapText(string text, float fontSize, float spacing, float maxWidth)
        {
            List<string> lines = new();
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return lines;
            }

            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string test = currentLine.Length == 0 ? word : currentLine + " " + word;
                Vector2 size = Raylib.MeasureTextEx(UISystem.FontHuge, test, fontSize, spacing);

                if (size.X > maxWidth && currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = test;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        private static void DrawQuestionLog(TwentyQuestionsGame game, float offX, float offY, float scale)
        {
            float logX = 10;
            float logY = 28;
            float logW = UISystem.VirtualWidth - 20;
            float logH = 93;
            float maxTextWidth = logW * scale - 10 * scale;

            Raylib.BeginScissorMode(
                (int)(offX + (logX * scale)),
                (int)(offY + (logY * scale)),
                (int)(logW * scale),
                (int)(logH * scale)
            );

            float fontSize = 9 * scale;
            float spacing = 1 * scale;
            float lineHeight = fontSize + 3 * scale;

            // Calculate total content height for scrolling (with wrapping)
            float totalHeight = 0;
            foreach (var (q, a) in game.QuestionLog)
            {
                List<string> qLines = WrapText($"You: {q}", fontSize, spacing, maxTextWidth);
                List<string> aLines = WrapText($"{game.Npc.Name}: {a}", fontSize, spacing, maxTextWidth);
                totalHeight += (qLines.Count + aLines.Count) * lineHeight + 4 * scale;
            }

            // Status message at bottom
            if (!string.IsNullOrEmpty(game.StatusMessage) && game.QuestionLog.Count == 0)
            {
                totalHeight += lineHeight + 4 * scale;
            }

            float maxScroll = Math.Max(0, totalHeight - (logH * scale));
            logScroll = Math.Clamp(logScroll, 0, maxScroll);

            // Auto-scroll to bottom
            logScroll = maxScroll;

            float cursorY = -logScroll;

            foreach (var (q, a) in game.QuestionLog)
            {
                // Draw question (wrapped)
                List<string> qLines = WrapText($"You: {q}", fontSize, spacing, maxTextWidth);
                foreach (string line in qLines)
                {
                    Vector2 qPos = new Vector2(offX + (logX * scale), offY + (logY * scale) + cursorY);
                    Raylib.DrawTextEx(UISystem.FontHuge, line, qPos, fontSize, spacing, Color.SkyBlue);
                    cursorY += lineHeight;
                }

                // Draw answer (wrapped)
                Color answerColor = a == "..." ? Color.Gray : Color.Orange;
                List<string> aLines = WrapText($"{game.Npc.Name}: {a}", fontSize, spacing, maxTextWidth);
                foreach (string line in aLines)
                {
                    Vector2 aPos = new Vector2(offX + (logX * scale), offY + (logY * scale) + cursorY);
                    Raylib.DrawTextEx(UISystem.FontHuge, line, aPos, fontSize, spacing, answerColor);
                    cursorY += lineHeight;
                }

                cursorY += 4 * scale;
            }

            // Status message
            if (!string.IsNullOrEmpty(game.StatusMessage) && game.QuestionLog.Count == 0)
            {
                Vector2 statusPos = new Vector2(offX + (logX * scale), offY + (logY * scale) + cursorY);
                Color statusColor = game.IsWaitingForAI ? Color.Yellow : Color.White;
                Raylib.DrawTextEx(UISystem.FontHuge, game.StatusMessage, statusPos, fontSize, spacing, statusColor);
            }

            Raylib.EndScissorMode();
        }

        private static void DrawBottomPanel(TwentyQuestionsGame game, float offX, float offY, float scale)
        {
            float panelX = 10;
            float panelY = 130;
            float fontSize = 9 * scale;
            float spacing = 1 * scale;

            if (game.IsWaitingForAI)
            {
                // Thinking animation
                thinkingDots += Raylib.GetFrameTime() * 3f;
                if (thinkingDots > 3f) thinkingDots = 0f;
                int dotCount = (int)thinkingDots + 1;
                string dots = new string('.', dotCount);

                Vector2 thinkPos = new Vector2(offX + (panelX * scale), offY + ((panelY + 3) * scale));
                Raylib.DrawTextEx(UISystem.FontHuge, $"{game.Npc.Name} is thinking{dots}", thinkPos, fontSize, spacing, Color.Yellow);
                return;
            }

            if (game.Phase == TwentyQuestionsGame.GamePhase.WaitingForQuestion)
            {
                // Draw input text
                string display = inputText;
                int localCursor = inputCursorIndex;

                Vector2 inputPos = new Vector2(offX + ((10 + 7) * scale), offY + ((132 + 4) * scale));
                Raylib.DrawTextEx(UISystem.FontHuge, display, inputPos, 9 * scale, 0, Color.White);

                // Cursor blink
                if ((int)(Raylib.GetTime() * 2) % 2 == 0)
                {
                    string preCursor = display.Substring(0, Math.Min(localCursor, display.Length));
                    Vector2 cursorMeasure = Raylib.MeasureTextEx(UISystem.FontHuge, preCursor, 9 * scale, 0);
                    float cursorX = inputPos.X + cursorMeasure.X;
                    Rectangle cursorRect = new Rectangle(cursorX, inputPos.Y, 2 * scale, 9 * scale);
                    Color cursorColor = isGuessMode ? Color.Green : Color.Yellow;
                    Raylib.DrawRectangleRec(cursorRect, cursorColor);
                }

                // Mode and button hints
                string modeLabel = isGuessMode ? "[ANSWER MODE]" : "[QUESTION MODE]";
                Color modeColor = isGuessMode ? Color.Green : Color.Yellow;
                Vector2 modePos = new Vector2(offX + (panelX * scale), offY + ((152) * scale));
                Raylib.DrawTextEx(UISystem.FontHuge, modeLabel, modePos, 8 * scale, 0, modeColor);

                string hints = "ENTER:Send | TAB:Mode | G:Give Up | ESC:Quit";
                Vector2 hintsSize = Raylib.MeasureTextEx(UISystem.FontHuge, hints, 7 * scale, 0);
                Vector2 hintPos = new Vector2(offX + ((UISystem.VirtualWidth - 10) * scale) - hintsSize.X, offY + ((153) * scale));
                Raylib.DrawTextEx(UISystem.FontHuge, hints, hintPos, 7 * scale, 0, new Color(120, 120, 120, 255));
            }
        }

        public static void HandleInput(TwentyQuestionsGame game)
        {
            // Role selection input
            if (game.Phase == TwentyQuestionsGame.GamePhase.RoleSelect)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                {
                    roleSelection = 1;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    roleSelection = 0;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (roleSelection == 0) // Ask Questions
                    {
                        game.SelectRole_AskQuestions();
                    }
                    // roleSelection == 1 is "Coming Soon" — do nothing
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Reset();
                    MinigameManager.EndMinigame();
                }
                return;
            }

            // Result screen — dismiss on X/Enter after fully revealed, or skip typewriter
            if (game.Phase == TwentyQuestionsGame.GamePhase.ShowingResult)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    if (game.ResultCharIndex >= game.ResultReaction.Length)
                    {
                        // Fully revealed — dismiss
                        game.DismissPressed = true;
                        Reset();
                    }
                    else
                    {
                        // Skip to end of typewriter
                        game.ResultCharIndex = game.ResultReaction.Length;
                    }
                }
                return;
            }

            // Waiting for AI reaction — no input
            if (game.Phase == TwentyQuestionsGame.GamePhase.PlayerWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.NPCWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.GaveUp)
            {
                return;
            }

            if (game.IsWaitingForAI) return;

            if (game.Phase == TwentyQuestionsGame.GamePhase.WaitingForQuestion)
            {
                // Tab to toggle between Ask/Answer mode
                if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                {
                    isGuessMode = !isGuessMode;
                }

                // Give up
                if (Raylib.IsKeyPressed(KeyboardKey.G) && inputText.Length == 0)
                {
                    game.GiveUp();
                    return;
                }

                // Escape to quit minigame
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Reset();
                    MinigameManager.EndMinigame();
                    return;
                }

                // Cursor navigation
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

                // Text input
                int key = Raylib.GetCharPressed();
                while (key > 0)
                {
                    if (key >= 32 && key <= 126 && inputText.Length < 200)
                    {
                        inputText = inputText.Insert(inputCursorIndex, ((char)key).ToString());
                        inputCursorIndex++;
                    }
                    key = Raylib.GetCharPressed();
                }

                // Backspace
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && inputText.Length > 0 && inputCursorIndex > 0)
                {
                    inputText = inputText.Remove(inputCursorIndex - 1, 1);
                    inputCursorIndex--;
                }

                // Submit
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) && inputText.Length > 0)
                {
                    string text = inputText;
                    inputText = "";
                    inputCursorIndex = 0;

                    if (isGuessMode)
                    {
                        game.SubmitGuess(text);
                    }
                    else
                    {
                        game.SubmitQuestion(text);
                    }
                }

                // Scroll log
                float wheel = Raylib.GetMouseWheelMove();
                if (wheel != 0)
                {
                    logScroll -= wheel * 20;
                }
            }
        }
    }
}

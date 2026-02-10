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
        private static float inputCursorTimer = 0f;
        private static bool isGuessMode = false; // false = Ask, true = Answer
        private static float thinkingDots = 0f;

        // Scroll state for question log
        private static float logScroll = 0f;

        // Role selection state
        private static int roleSelection = 0; // 0 = Ask Questions, 1 = Answer Questions

        // Result screen: cached portrait
        private static Texture2D resultPortrait;
        private static bool resultPortraitLoaded = false;

        // Layout Constants
        private const int ContentPadding = 20;

        public static void Reset()
        {
            inputText = "";
            inputCursorTimer = 0f;
            isGuessMode = false;
            thinkingDots = 0f;
            logScroll = 0f;
            roleSelection = 0;

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

            // Background Overlay
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

            // Game Board
            DrawGameBoard(game, screenW, screenH);

            // Loading / Waiting Overlay
            if (game.Phase == TwentyQuestionsGame.GamePhase.PlayerWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.NPCWin ||
                game.Phase == TwentyQuestionsGame.GamePhase.GaveUp)
            {
                Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 100));
                DrawLoadingReaction(game, screenW, screenH);
            }
        }

        private static void DrawRoleSelect(TwentyQuestionsGame game, int screenW, int screenH)
        {
            int panelW = 500;
            int panelH = 350;
            Rectangle panelRect = new Rectangle((screenW - panelW) / 2, (screenH - panelH) / 2, panelW, panelH);

            UISystem.DrawCozyPanel(panelRect, "20 QUESTIONS");

            // Subtitle
            string sub = $"Playing with {game.Npc.Name}";
            Vector2 subSize = Raylib.MeasureTextEx(UISystem.FontMedium, sub, 24, 1);
            Raylib.DrawTextEx(UISystem.FontMedium, sub, new Vector2(panelRect.X + (panelW - subSize.X) / 2, panelRect.Y + 60), 24, 1, UISystem.ColorCream);

            // Options
            int startY = (int)panelRect.Y + 120;
            int btnH = 50;
            int spacing = 20;

            Rectangle btn1 = new Rectangle(panelRect.X + 50, startY, panelW - 100, btnH);
            Rectangle btn2 = new Rectangle(panelRect.X + 50, startY + btnH + spacing, panelW - 100, btnH);

            bool sel1 = (roleSelection == 0);
            if (UISystem.DrawCozyButton(btn1, "Ask Questions", sel1))
            {
                roleSelection = 0;
                game.SelectRole_AskQuestions();
            }

            bool sel2 = (roleSelection == 1);
            // Draw button 2 visually disabled? 
            // DrawCozyButton doesn't support disabled state directly, but we can hack color or just not react.
            // For now, standard button.
            if (UISystem.DrawCozyButton(btn2, "Answer Questions (Soon)", sel2))
            {
                // No-op
            }

            // Hint
            Raylib.DrawTextEx(UISystem.FontSmall, "Use Arrows & Enter", new Vector2(panelRect.X + 20, panelRect.Y + panelH - 30), 16, 1, UISystem.ColorTan);
        }

        private static void DrawGameBoard(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // Layout: 
            // - Header (Top)
            // - Log (Middle)
            // - Input (Bottom)

            int headerH = 60;
            int inputH = 100;
            int logH = screenH - headerH - inputH - (ContentPadding * 2);
            // Wait, we want full screen coverage minus minimal padding? Or a centered box?
            // Let's go with a centered content area approx 800x600 or responsive.
            // We'll use almost full screen with margins.
            int margin = 40;

            Rectangle mainRect = new Rectangle(margin, margin, screenW - margin * 2, screenH - margin * 2);
            // Actually, let's treat the whole screen as the "desk" and place panels.

            // Header Panel
            Rectangle headerRect = new Rectangle(margin, margin, mainRect.Width, headerH);
            UISystem.DrawCozyPanel(headerRect, $"{game.Npc.Name} - Q: {game.QuestionsAsked}/{TwentyQuestionsGame.MaxQuestions}");

            // Log Panel
            Rectangle logRect = new Rectangle(margin, margin + headerH + 10, mainRect.Width, logH);
            UISystem.DrawCozyPanel(logRect);

            // Input Panel
            Rectangle inputRect = new Rectangle(margin, logRect.Y + logRect.Height + 10, mainRect.Width, inputH);
            UISystem.DrawCozyPanel(inputRect);

            // Draw Log Content
            DrawQuestionLog(game, logRect);

            // Draw Input Content
            DrawInputPanel(game, inputRect);
        }

        private static void DrawQuestionLog(TwentyQuestionsGame game, Rectangle rect)
        {
            // Scissor
            Raylib.BeginScissorMode((int)rect.X + 10, (int)rect.Y + 10, (int)rect.Width - 20, (int)rect.Height - 20);

            float fontSize = 24;
            float spacing = 1;
            float lineHeight = 30;
            float startY = rect.Y + 10 - logScroll;
            float currentY = startY;
            float maxWidth = rect.Width - 40;

            // Log
            foreach (var (q, a) in game.QuestionLog)
            {
                // Question
                string qText = $"You: {q}";
                Vector2 qSize = Raylib.MeasureTextEx(UISystem.FontMedium, qText, fontSize, spacing);
                // Wrap logic simplified for now (assuming single line or implementing wrap if vital)
                // Let's implement basic wrap in future if needed, for new let's clip.
                Raylib.DrawTextEx(UISystem.FontMedium, qText, new Vector2(rect.X + 20, currentY), fontSize, spacing, UISystem.ColorTan);
                currentY += lineHeight;

                // Answer
                string aText = $"{game.Npc.Name}: {a}";
                Raylib.DrawTextEx(UISystem.FontMedium, aText, new Vector2(rect.X + 20, currentY), fontSize, spacing, UISystem.ColorCream);
                currentY += lineHeight + 5;
            }

            // Auto scroll simplified: if content > height, scroll to bottom
            if (currentY > rect.Y + rect.Height - 20)
            {
                // Logic to set logScroll to keep bottom visible
                // For now, let's just reset currentY if we are refreshing logic, but Draw is immediate.
                // We need persistent scroll target.
                // Let's just clamp logScroll.
                float totalH = currentY - startY;
                if (totalH > rect.Height - 20)
                {
                    logScroll = totalH - (rect.Height - 20);
                }
            }

            // Status message if empty
            if (game.QuestionLog.Count == 0)
            {
                string status = game.StatusMessage;
                Raylib.DrawTextEx(UISystem.FontMedium, status, new Vector2(rect.X + 20, rect.Y + 20), fontSize, spacing, UISystem.ColorTan);
            }

            Raylib.EndScissorMode();
        }

        private static void DrawInputPanel(TwentyQuestionsGame game, Rectangle rect)
        {
            float centerX = rect.X + rect.Width / 2;
            float centerY = rect.Y + rect.Height / 2;

            if (game.IsWaitingForAI)
            {
                thinkingDots += Raylib.GetFrameTime() * 3f;
                if (thinkingDots > 3f) thinkingDots = 0f;
                string dots = new string('.', (int)thinkingDots + 1);
                string text = $"{game.Npc.Name} is thinking{dots}";
                Vector2 size = Raylib.MeasureTextEx(UISystem.FontMedium, text, 24, 1);
                Raylib.DrawTextEx(UISystem.FontMedium, text, new Vector2(centerX - size.X / 2, centerY - size.Y / 2), 24, 1, UISystem.ColorWarmToffee);
            }
            else if (game.Phase == TwentyQuestionsGame.GamePhase.WaitingForQuestion)
            {
                // Input Box
                Rectangle inputField = new Rectangle(rect.X + 20, rect.Y + 40, rect.Width - 40, 40);
                Raylib.DrawRectangleRounded(inputField, 0.2f, 8, UISystem.ColorCharcoal);

                // Mode Border
                Color borderCol = isGuessMode ? Color.Green : UISystem.ColorTan; // Green for guess mode distinction? Maybe Cozy Green?
                if (isGuessMode) borderCol = new Color(100, 200, 100, 255); // Soft Green
                Raylib.DrawRectangleRoundedLines(inputField, 0.2f, 8, borderCol);

                // Text
                string display = inputText;
                // Cursor logic
                inputCursorTimer += Raylib.GetFrameTime();
                if ((int)(inputCursorTimer * 2) % 2 == 0) display += "|";

                Raylib.DrawTextEx(UISystem.FontMedium, display, new Vector2(inputField.X + 10, inputField.Y + 8), 24, 1, UISystem.ColorCream);

                // Labels
                Raylib.DrawTextEx(UISystem.FontSmall, isGuessMode ? "GUESS MODE (Tab to switch)" : "QUESTION MODE (Tab to switch)", new Vector2(inputField.X, inputField.Y - 20), 16, 1, borderCol);

                string hint = "ENTER: Send  G: Give Up  ESC: Back";
                Vector2 hintSize = Raylib.MeasureTextEx(UISystem.FontSmall, hint, 16, 1);
                Raylib.DrawTextEx(UISystem.FontSmall, hint, new Vector2(inputField.X + inputField.Width - hintSize.X, inputField.Y - 20), 16, 1, UISystem.ColorTan);
            }
        }

        private static void DrawLoadingReaction(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // Centered Text
            string text = "Calculating Outcome...";
            Vector2 size = Raylib.MeasureTextEx(UISystem.FontLarge, text, 48, 2);
            Raylib.DrawTextEx(UISystem.FontLarge, text, new Vector2((screenW - size.X) / 2, (screenH - size.Y) / 2), 48, 2, UISystem.ColorCream);
        }

        private static void DrawResultScreen(TwentyQuestionsGame game, int screenW, int screenH)
        {
            // Dark Background
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 200));

            // Portrait if available
            if (!resultPortraitLoaded)
            {
                string path = game.Npc.GetCurrentPortraitPath();
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    resultPortrait = Raylib.LoadTexture(path);
                    resultPortraitLoaded = true;
                }
            }
            if (resultPortraitLoaded)
            {
                float scale = resultPortrait.Height > screenH * 1.5f ? (float)screenH / resultPortrait.Height : 1.0f;
                float pW = resultPortrait.Width * scale;
                float pH = resultPortrait.Height * scale;
                Raylib.DrawTextureEx(resultPortrait, new Vector2((screenW - pW) / 2, screenH - pH), 0f, scale, Color.White);
            }

            // Dialogue Box at Bottom
            int boxH = 200;
            Rectangle boxRect = new Rectangle(40, screenH - boxH - 40, screenW - 80, boxH);
            UISystem.DrawCozyPanel(boxRect, game.Npc.Name);

            // Text
            string resultHeader = game.ResultOutcome switch
            {
                TwentyQuestionsGame.GamePhase.PlayerWin => $"VICTORY! Secret word: {game.SecretWord}",
                TwentyQuestionsGame.GamePhase.NPCWin => $"{game.Npc.Name} WINS! Secret word: {game.SecretWord}",
                TwentyQuestionsGame.GamePhase.GaveUp => $"Gave Up! Secret word: {game.SecretWord}",
                _ => ""
            };

            Raylib.DrawTextEx(UISystem.FontLarge, resultHeader, new Vector2(boxRect.X + 30, boxRect.Y + 50), 32, 2, UISystem.ColorWarmToffee);

            string reaction = game.ResultReaction;
            // Typewriter effect logic remains in game or managed here?
            // The original managed typewriter in Draw. Let's simplify and show full text for now or implement timer.
            // We'll show full text to avoid complexity in this refactor unless necessary.
            Raylib.DrawTextEx(UISystem.FontMedium, reaction, new Vector2(boxRect.X + 30, boxRect.Y + 100), 24, 1, UISystem.ColorCream);

            Raylib.DrawTextEx(UISystem.FontSmall, "Press Enter or X to Close", new Vector2(boxRect.X + boxRect.Width - 250, boxRect.Y + boxRect.Height - 30), 16, 1, UISystem.ColorTan);
        }

        public static void HandleInput(TwentyQuestionsGame game)
        {
            // Input handling logic (copied/adapted from original but mapped to new UI states if needed)

            if (game.Phase == TwentyQuestionsGame.GamePhase.RoleSelect)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    roleSelection = (roleSelection == 0) ? 1 : 0;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (roleSelection == 0) game.SelectRole_AskQuestions();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Reset();
                    MinigameManager.EndMinigame();
                }
                return;
            }

            if (game.Phase == TwentyQuestionsGame.GamePhase.ShowingResult)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Reset();
                    MinigameManager.EndMinigame();
                }
                return;
            }

            if (game.Phase == TwentyQuestionsGame.GamePhase.WaitingForQuestion)
            {
                // Type text
                int key = Raylib.GetCharPressed();
                while (key > 0)
                {
                    if ((key >= 32) && (key <= 125) && (inputText.Length < 50))
                    {
                        inputText += (char)key;
                    }
                    key = Raylib.GetCharPressed();
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    if (inputText.Length > 0) inputText = inputText.Substring(0, inputText.Length - 1);
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Tab))
                {
                    isGuessMode = !isGuessMode;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (!string.IsNullOrWhiteSpace(inputText))
                    {
                        if (isGuessMode) game.SubmitGuess(inputText);
                        else game.SubmitQuestion(inputText);
                        inputText = "";
                    }
                }

                if (Raylib.IsKeyPressed(KeyboardKey.G))
                {
                    game.GiveUp();
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    Reset();
                    MinigameManager.EndMinigame();
                }
            }
        }
    }
}

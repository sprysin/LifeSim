using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static class TVSystem
    {
        public static bool IsOpen { get; private set; } = false;

        private static int selection = 0;
        private static string[] options = { "20 Questions", "Coming Soon" };

        // Error message display
        private static string errorMessage = "";
        private static float errorTimer = 0f;
        private const float ErrorDisplayTime = 2.0f;

        public static void Initialize()
        {
            // No assets to load yet
        }

        public static void Open()
        {
            IsOpen = true;
            selection = 0;
            errorMessage = "";
            errorTimer = 0f;
        }

        public static void Close()
        {
            IsOpen = false;
        }

        public static void Update()
        {
            if (!IsOpen) return;

            // Error timer countdown
            if (errorTimer > 0)
            {
                errorTimer -= Raylib.GetFrameTime();
                if (errorTimer <= 0) errorMessage = "";
            }

            // Navigation
            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                selection++;
                if (selection >= options.Length) selection = 0;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                selection--;
                if (selection < 0) selection = options.Length - 1;
            }

            // Selection
            if (Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                ExecuteSelection(selection);
            }

            // Exit
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                Close();
            }
        }

        private static void ExecuteSelection(int index)
        {
            if (index == 0) // 20 Questions
            {
                // Allow playing even if not following. Pick Boogie if no one is following.
                NPC? opponent = NPC.ActiveFollower;
                if (opponent == null)
                {
                    opponent = Engine.ActiveNPCs.Find(n => n.Name == "Boogie");
                }

                if (opponent != null)
                {
                    // Start 20 Questions
                    TwentyQuestionsUI.Reset();
                    MinigameManager.StartMinigame(MinigameType.TwentyQuestions, opponent);
                    Close();
                }
                else
                {
                    errorMessage = "No one available to play.";
                    errorTimer = ErrorDisplayTime;
                }
            }
            else
            {
                // Placeholder for other options
                errorMessage = "Feature coming soon!";
                errorTimer = ErrorDisplayTime;
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // 1. Darken Background
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 150));

            // 2. Define Panel Dimensions (Cozy Style)
            int panelW = 400;
            int panelH = 300;
            Rectangle panelRect = new Rectangle((screenW - panelW) / 2, (screenH - panelH) / 2, panelW, panelH);

            // 3. Draw Panel
            UISystem.DrawCozyPanel(panelRect, "TV OPTIONS");

            // 4. Draw Options as Cozy Buttons
            int startY = (int)panelRect.Y + 80;
            int btnH = 50;
            int spacing = 20;
            int btnW = panelW - 60; // Padding
            int btnX = (int)panelRect.X + 30;

            for (int i = 0; i < options.Length; i++)
            {
                Rectangle btnRect = new Rectangle(btnX, startY + (i * (btnH + spacing)), btnW, btnH);
                bool isSelected = (i == selection);

                // Check for mouse hover to update selection (Cozy UI typically follows mouse too)
                if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), btnRect))
                {
                    selection = i;
                    isSelected = true;
                }

                if (UISystem.DrawCozyButton(btnRect, options[i], isSelected))
                {
                    ExecuteSelection(i);
                }
            }

            // 5. Draw Exit Hint or Button
            // Add a small Exit button at the bottom center or instruction text
            string exitText = "Press Z or ESC to Close";
            Vector2 exitSize = Raylib.MeasureTextEx(UISystem.FontSmall, exitText, 20, 1);
            Raylib.DrawTextEx(UISystem.FontSmall, exitText,
                new Vector2(panelRect.X + (panelRect.Width - exitSize.X) / 2, panelRect.Y + panelRect.Height - 30),
                20, 1, UISystem.ColorTan);

            // 6. Draw Error Message Overlay
            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Draw a small pill for error
                Vector2 errSize = Raylib.MeasureTextEx(UISystem.FontSmall, errorMessage, 20, 1);
                float pad = 20;
                Rectangle errRect = new Rectangle(
                    (screenW - errSize.X - pad) / 2,
                    panelRect.Y + panelRect.Height + 20,
                    errSize.X + pad,
                    errSize.Y + pad
                );

                Raylib.DrawRectangleRounded(errRect, 0.5f, 5, new Color(50, 0, 0, 200));
                Raylib.DrawRectangleRoundedLines(errRect, 0.5f, 5, Color.Red);
                Raylib.DrawTextEx(UISystem.FontSmall, errorMessage,
                    new Vector2(errRect.X + pad / 2, errRect.Y + pad / 2),
                    20, 1, Color.White);
            }
        }
    }
}

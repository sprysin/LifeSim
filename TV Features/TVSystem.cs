using Raylib_cs;
using System.Numerics;

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

        // UI Layout
        private const int PanelW = 120;
        private const int PanelH = 140;
        private const int PanelX = UISystem.VirtualWidth - PanelW - 10;
        private const int PanelY = (UISystem.VirtualHeight - PanelH) / 2;
        private const int GridSize = 10;

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
            if (Raylib.IsKeyPressed(KeyboardKey.X))
            {
                if (selection == 0) // 20 Questions
                {
                    if (NPC.ActiveFollower != null)
                    {
                        // Start 20 Questions with the following NPC
                        TwentyQuestionsUI.Reset();
                        MinigameManager.StartMinigame(MinigameType.TwentyQuestions, NPC.ActiveFollower);
                        Close();
                        return;
                    }
                    else
                    {
                        errorMessage = "Someone must follow you to play";
                        errorTimer = ErrorDisplayTime;
                    }
                }
            }

            // Exit
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                Close();
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Render to UI Buffer
            Raylib.BeginTextureMode(UISystem.UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            // Draw Panel Background
            Rectangle panelRect = new Rectangle(PanelX, PanelY, PanelW, PanelH);
            Raylib.DrawRectangleRec(panelRect, new Color(0, 0, 0, 220));

            // Draw Grid
            Color gridColor = new Color(200, 200, 200, 30);

            // Vertical lines
            for (int x = 0; x <= PanelW; x += GridSize)
            {
                Raylib.DrawLine(PanelX + x, PanelY, PanelX + x, PanelY + PanelH, gridColor);
            }
            // Horizontal lines
            for (int y = 0; y <= PanelH; y += GridSize)
            {
                Raylib.DrawLine(PanelX, PanelY + y, PanelX + PanelW, PanelY + y, gridColor);
            }


            Raylib.DrawRectangleLinesEx(panelRect, 1, Color.White);

            // Draw Header
            string title = "TV OPTIONS";
            Vector2 titleSize = Raylib.MeasureTextEx(UISystem.FontSmall, title, 12, 0);
            Raylib.DrawTextEx(UISystem.FontSmall, title, new Vector2(PanelX + (PanelW - titleSize.X) / 2, PanelY + 10), 12, 0, Color.Yellow);

            // Draw Options
            int startY = PanelY + 35;
            int spacing = 20;

            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = (i == selection);
                string text = options[i];
                Color textColor = isSelected ? Color.White : Color.Gray;

                if (isSelected)
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, "> " + text, new Vector2(PanelX + 15, startY + (i * spacing)), 12, 0, textColor);
                }
                else
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, "  " + text, new Vector2(PanelX + 15, startY + (i * spacing)), 12, 0, textColor);
                }
            }
            // Draw error message if present
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Vector2 errSize = Raylib.MeasureTextEx(UISystem.FontSmall, errorMessage, 10, 0);
                float errX = PanelX + (PanelW - errSize.X) / 2;
                float errY = PanelY + PanelH - 20;
                Raylib.DrawTextEx(UISystem.FontSmall, errorMessage, new Vector2(errX, errY), 10, 0, Color.Red);
            }

            Raylib.EndTextureMode();

            // Draw Buffer to Screen
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UISystem.UIBuffer.Texture.Width, -UISystem.UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UISystem.UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);
        }
    }
}

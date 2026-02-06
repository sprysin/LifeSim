using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Menu State
        private static float scrollTimer = 0f;

        public static void DrawHomeMenu(int selection)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // 1. Draw Procedural Background
            DrawMenuBackground();

            // 2. Render UI to Buffer
            Raylib.BeginTextureMode(UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            string[] options = { "Start", "Debug Room", "Quit" };
            int startY = 60;
            int spacing = 25;

            // Draw Title
            Vector2 titleSize = Raylib.MeasureTextEx(FontMedium, "LIFESIM", 32, 0);
            Raylib.DrawTextEx(FontMedium, "LIFESIM", new Vector2((VirtualWidth - titleSize.X) / 2, 20), 32, 0, Color.White);
            Raylib.DrawTextEx(FontMedium, "LIFESIM", new Vector2((VirtualWidth - titleSize.X) / 2 + 2, 22), 32, 0, new Color(100, 100, 100, 255)); // Shadow


            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = (i == selection);
                string text = options[i];

                int btnW = 120;
                int btnH = 20;
                Rectangle btnRect = new Rectangle((VirtualWidth - btnW) / 2, startY + (i * spacing), btnW, btnH);

                // Modern Button UI (Black/White High Contrast)
                Color btnColor = isSelected ? Color.White : new Color(0, 0, 0, 200);
                Color textColor = isSelected ? Color.Black : Color.White;
                Color borderColor = Color.White;

                // Shadow
                if (!isSelected)
                {
                    Raylib.DrawRectangle((int)btnRect.X + 2, (int)btnRect.Y + 2, (int)btnRect.Width, (int)btnRect.Height, new Color(0, 0, 0, 100));
                }

                // Base
                Raylib.DrawRectangleRec(btnRect, btnColor);

                // Border
                Raylib.DrawRectangleLinesEx(btnRect, 1, borderColor);

                // Text drawn to Buffer here is acceptable for Title Menu/Buttons as they are large
                // Keeping Title Menu simpler to avoid logic duplication for now unless User complains
                Vector2 textSize = Raylib.MeasureTextEx(FontSmall, text, 12, 0);
                Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSize.X) / 2, btnRect.Y + (btnRect.Height - textSize.Y) / 2);

                Raylib.DrawTextEx(FontSmall, text, textPos, 12, 0, textColor);

                if (isSelected)
                {
                    Raylib.DrawTextEx(FontSmall, ">", new Vector2(btnRect.X - 15, btnRect.Y + 4), 12, 0, Color.White);
                    Raylib.DrawTextEx(FontSmall, "<", new Vector2(btnRect.X + btnRect.Width + 5, btnRect.Y + 4), 12, 0, Color.White);

                }
            }

            Raylib.EndTextureMode();

            // 3. Draw Buffer to Screen
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UIBuffer.Texture.Width, -UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);
        }

        private static void DrawMenuBackground()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Dark Background
            Raylib.ClearBackground(new Color(10, 10, 15, 255));

            // Scrolling Grid (Same as dialogue box but fullscreen)
            scrollTimer += Raylib.GetFrameTime() * 10f;
            int gridSize = 40;
            float offset = (float)(Raylib.GetTime() * 10.0) % gridSize;

            Color gridColor = new Color(50, 50, 60, 255);

            // Vertical Lines
            for (int i = 0; i < screenW / gridSize + 1; i++)
            {
                float x = (i * gridSize) + offset;
                Raylib.DrawLine((int)x, 0, (int)x, screenH, gridColor);
            }

            // Horizontal Lines
            for (int j = 0; j < screenH / gridSize + 1; j++)
            {
                int y = j * gridSize;
                Raylib.DrawLine(0, y, screenW, y, gridColor);
            }
        }

        public static void DrawDebugLocationMenu(int selection)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Draw background
            DrawMenuBackground();

            // Render to buffer
            Raylib.BeginTextureMode(UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            // Title
            Vector2 titleSize = Raylib.MeasureTextEx(FontMedium, "Choose Location", 24, 0);
            Raylib.DrawTextEx(FontMedium, "Choose Location", new Vector2((VirtualWidth - titleSize.X) / 2, 20), 24, 0, Color.White);

            // Options
            string[] options = { "Debug Room", "Kitchen" };
            int startY = 60;
            int spacing = 25;

            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = (i == selection);
                string text = options[i];

                int btnW = 120;
                int btnH = 20;
                Rectangle btnRect = new Rectangle((VirtualWidth - btnW) / 2, startY + (i * spacing), btnW, btnH);

                Color btnColor = isSelected ? Color.White : new Color(0, 0, 0, 200);
                Color textColor = isSelected ? Color.Black : Color.White;
                Color borderColor = Color.White;

                if (!isSelected)
                {
                    Raylib.DrawRectangle((int)btnRect.X + 2, (int)btnRect.Y + 2, (int)btnRect.Width, (int)btnRect.Height, new Color(0, 0, 0, 100));
                }

                Raylib.DrawRectangleRec(btnRect, btnColor);
                Raylib.DrawRectangleLinesEx(btnRect, 1, borderColor);

                Vector2 textSize = Raylib.MeasureTextEx(FontSmall, text, 12, 0);
                Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSize.X) / 2, btnRect.Y + (btnRect.Height - textSize.Y) / 2);
                Raylib.DrawTextEx(FontSmall, text, textPos, 12, 0, textColor);

                if (isSelected)
                {
                    Raylib.DrawTextEx(FontSmall, ">", new Vector2(btnRect.X - 15, btnRect.Y + 4), 12, 0, Color.White);
                    Raylib.DrawTextEx(FontSmall, "<", new Vector2(btnRect.X + btnRect.Width + 5, btnRect.Y + 4), 12, 0, Color.White);
                }
            }

            // Instructions
            Vector2 instrSize = Raylib.MeasureTextEx(FontTiny, "ESC to go back", 10, 0);
            Raylib.DrawTextEx(FontTiny, "ESC to go back", new Vector2((VirtualWidth - instrSize.X) / 2, VirtualHeight - 20), 10, 0, new Color(150, 150, 150, 255));

            Raylib.EndTextureMode();

            // Draw to screen
            Rectangle dest = new Rectangle(0, 0, screenW, screenH);
            Rectangle flipSrc = new Rectangle(0, 0, UIBuffer.Texture.Width, -UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);
        }
    }
}

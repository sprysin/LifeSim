using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Menu State
        private static float scrollTimer = 0f;

        public static (int, bool) DrawHomeMenu(int selection)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            bool clicked = false;

            // 1. Draw Procedural Background
            DrawMenuBackground();

            string[] options = { "Start", "Debug Room", "Quit" };

            // Layout
            int startY = screenH / 2;
            int spacing = 80;
            int btnW = 400;
            int btnH = 60;

            // Draw Title
            string title = "LIFESIM";
            float titleSizeVal = 96;
            float titleSpacing = 4;
            Vector2 titleSize = Raylib.MeasureTextEx(FontTitle, title, titleSizeVal, titleSpacing);
            Vector2 titlePos = new Vector2((screenW - titleSize.X) / 2, screenH / 4);

            // Shadow
            Raylib.DrawTextEx(FontTitle, title, new Vector2(titlePos.X + 4, titlePos.Y + 4), titleSizeVal, titleSpacing, ColorEspresso);
            // Main
            Raylib.DrawTextEx(FontTitle, title, titlePos, titleSizeVal, titleSpacing, ColorCream);

            Vector2 mousePos = Raylib.GetMousePosition();

            for (int i = 0; i < options.Length; i++)
            {
                string text = options[i];
                Rectangle btnRect = new Rectangle((screenW - btnW) / 2, startY + (i * spacing), btnW, btnH);

                // Mouse Interaction (Update Selection)
                if (Raylib.CheckCollisionPointRec(mousePos, btnRect))
                {
                    selection = i;
                }

                bool isSelected = (i == selection);

                // Draw Cozy Button
                if (DrawCozyButton(btnRect, text, isSelected))
                {
                    clicked = true;
                }
            }

            return (selection, clicked);
        }

        private static void DrawMenuBackground()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Dark Cozy Background
            Raylib.ClearBackground(ColorCharcoal);

            // Scrolling Grid (Subtle)
            scrollTimer += Raylib.GetFrameTime() * 10f;
            int gridSize = 40;
            float offset = (float)(Raylib.GetTime() * 10.0) % gridSize;

            Color gridColor = new Color(46, 26, 18, 50); // Espresso low alpha

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

        public static (int, bool) DrawDebugLocationMenu(int selection)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            bool clicked = false;

            // Draw background
            DrawMenuBackground();

            // Title
            string title = "Choose Location";
            Vector2 titleSize = Raylib.MeasureTextEx(FontMedium, title, 32, 2);
            Raylib.DrawTextEx(FontMedium, title, new Vector2((screenW - titleSize.X) / 2, 60), 32, 2, Color.White);

            string[] options = { "Debug Room", "Kitchen" };

            // Layout
            int startY = 150;
            int spacing = 80;
            int btnW = 300;
            int btnH = 50;

            Vector2 mousePos = Raylib.GetMousePosition();

            for (int i = 0; i < options.Length; i++)
            {
                string text = options[i];
                Rectangle btnRect = new Rectangle((screenW - btnW) / 2, startY + (i * spacing), btnW, btnH);

                if (Raylib.CheckCollisionPointRec(mousePos, btnRect))
                {
                    selection = i;
                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        clicked = true;
                    }
                }

                bool isSelected = (i == selection);

                Color btnColor = isSelected ? Color.White : new Color(0, 0, 0, 200);
                Color textColor = isSelected ? Color.Black : Color.White;
                Color borderColor = Color.White;

                if (!isSelected)
                {
                    Raylib.DrawRectangle((int)btnRect.X + 2, (int)btnRect.Y + 2, (int)btnRect.Width, (int)btnRect.Height, new Color(0, 0, 0, 100));
                }

                Raylib.DrawRectangleRec(btnRect, btnColor);
                Raylib.DrawRectangleLinesEx(btnRect, 2, borderColor);

                Vector2 textSize = Raylib.MeasureTextEx(FontSmall, text, 20, 2);
                Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSize.X) / 2, btnRect.Y + (btnRect.Height - textSize.Y) / 2);
                Raylib.DrawTextEx(FontSmall, text, textPos, 20, 2, textColor);

                if (isSelected)
                {
                    Raylib.DrawTextEx(FontSmall, ">", new Vector2(btnRect.X - 20, btnRect.Y + 15), 20, 0, Color.White);
                    Raylib.DrawTextEx(FontSmall, "<", new Vector2(btnRect.X + btnRect.Width + 10, btnRect.Y + 15), 20, 0, Color.White);
                }
            }

            // Instructions
            string instr = "ESC to go back";
            Vector2 instrSize = Raylib.MeasureTextEx(FontTiny, instr, 20, 0);
            Raylib.DrawTextEx(FontTiny, instr, new Vector2((screenW - instrSize.X) / 2, screenH - 40), 20, 0, new Color(150, 150, 150, 255));

            return (selection, clicked);
        }
    }
}

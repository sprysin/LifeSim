using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static class DiarySystem
    {
        public static bool IsOpen { get; private set; } = false;

        private static int selection = 0;
        private static string[] options = { "Read Diary (Coming Soon)", "Write Diary (Coming Soon)" };

        public static void Initialize()
        {
            // No assets to load yet
        }

        public static void Open()
        {
            IsOpen = true;
            selection = 0;
        }

        public static void Close()
        {
            IsOpen = false;
        }

        public static void Update()
        {
            if (!IsOpen) return;

            // Keyboard Navigation
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

            // Keyboard Selection
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

        public static void Draw()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Darken Background
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 150));

            // Main Stick to Screen Center
            int panelW = 400;
            int panelH = 300;
            Rectangle panelRect = new Rectangle((screenW - panelW) / 2, (screenH - panelH) / 2, panelW, panelH);

            UISystem.DrawCozyPanel(panelRect, "DIARY");

            // Draw Options
            int startY = (int)panelRect.Y + 80;
            int btnH = 50;
            int btnW = panelW - 60;
            int spacing = 20;

            for (int i = 0; i < options.Length; i++)
            {
                Rectangle btnRect = new Rectangle(panelRect.X + 30, startY + (i * (btnH + spacing)), btnW, btnH);
                bool isSelected = (i == selection);

                // Check for mouse hover to update selection
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

            // Hint
            Raylib.DrawTextEx(UISystem.FontSmall, "Press Z to Close", new Vector2(panelRect.X + 30, panelRect.Y + panelH - 30), 16, 1, UISystem.ColorTan);
        }

        private static void ExecuteSelection(int index)
        {
            if (index == 0)
            {
                // Read Diary
            }
            else if (index == 1)
            {
                // Write Diary
            }
        }
    }
}

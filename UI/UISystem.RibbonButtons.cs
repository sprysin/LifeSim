using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static partial class UISystem
    {
        // New Ribbon Button Style
        // isLeft: true if button comes from left side, false if from right
        public static bool DrawRibbonButton(Rectangle rect, string text, bool isLeft)
        {
            float mouseX = Raylib.GetMouseX();
            float mouseY = Raylib.GetMouseY();
            bool isHovered = Raylib.CheckCollisionPointRec(new Vector2(mouseX, mouseY), rect);

            // Animation: Slide out slightly on hover
            // BOTH ribbons slide AWAY from their screen edge (toward the edge, not toward center)
            // This makes the tips appear to extend toward the center
            float xOffset = isHovered ? 10 : 0;

            // Draw Main Body (Rectangle)
            Rectangle drawRect;

            if (isLeft)
                drawRect = new Rectangle(rect.X + xOffset, rect.Y, rect.Width, rect.Height);
            else
                drawRect = new Rectangle(rect.X - xOffset, rect.Y, rect.Width, rect.Height);

            Color bodyColor = new Color(0, 0, 0, 200); // Transparent Black
            Color glowColor = new Color(139, 69, 19, 255); // SaddleBrown for glow

            // Draw Diamond Tip
            Vector2 v1, v2, v3;
            int tipSize = (int)(rect.Height / 2);

            if (isLeft)
            {
                // Tip points Right
                // Winding: Top -> Bottom -> Tip (Clockwise)
                v1 = new Vector2(drawRect.X + drawRect.Width, drawRect.Y);
                v2 = new Vector2(drawRect.X + drawRect.Width, drawRect.Y + drawRect.Height);
                v3 = new Vector2((drawRect.X + drawRect.Width + tipSize), drawRect.Y + (drawRect.Height / 2));
            }
            else
            {
                float baseX = drawRect.X;
                float midY = drawRect.Y + drawRect.Height / 2f;

                v1 = new Vector2(baseX, drawRect.Y);             // 1. Top Left
                v2 = new Vector2(baseX - tipSize, midY);         // 2. Tip (This was v3)
                v3 = new Vector2(baseX, drawRect.Y + drawRect.Height); // 3. Bottom Left (This was v2)
            }


            if (isHovered)
            {
                // Simple outline logic for triangle is hard, just fill generic glow color
                Raylib.DrawTriangle(v1, v2, v3, glowColor);
            }
            Raylib.DrawTriangle(v1, v2, v3, bodyColor);

            if (isHovered)
            {
                // Draw Glow/Border
                Raylib.DrawRectangleRec(new Rectangle(drawRect.X - 2, drawRect.Y - 2, drawRect.Width + 4, drawRect.Height + 4), glowColor);
            }

            Raylib.DrawRectangleRec(drawRect, bodyColor);

            // Draw Text
            // FontMedium is size 32. Button height might be 40-50.
            int fontSize = 24;
            Vector2 textSize = Raylib.MeasureTextEx(FontMedium, text, fontSize, 1);

            // Center text in rect
            Vector2 textPos = new Vector2(
                drawRect.X + (drawRect.Width - textSize.X) / 2,
                drawRect.Y + (drawRect.Height - textSize.Y) / 2
            );

            Raylib.DrawTextEx(FontMedium, text, textPos, fontSize, 1, Color.White);

            return isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }


        // Draw Action Buttons
        private static void DrawActionButtons()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // --- LEFT SIDE BUTTONS ---
            // Scaled up 3x roughly
            int btnHeight = 80; // Was 50
            int btnWidth = 350;  // Was 140
            int spacing = 25;

            int startY = screenH / 2 - (btnHeight * 2); // Center vertically

            // 1. Change Room
            // Move more onto screen. Let's say we want 250px visible.
            // X = -100 (100px offscreen, 250px onscreen)
            Rectangle changeRoomBtn = new Rectangle(-50, startY, btnWidth, btnHeight);
            if (DrawRibbonButton(changeRoomBtn, "Change Room", true))
            {
                System.Console.WriteLine("Change Room Clicked");
            }
            startY += btnHeight + spacing;

            // 2. Cellphone
            Rectangle phoneBtn = new Rectangle(-50, startY, btnWidth, btnHeight);
            if (DrawRibbonButton(phoneBtn, "Cellphone", true))
            {
                System.Console.WriteLine("Cellphone Clicked");
            }
            startY += btnHeight + spacing;

            // 3. Terminal
            Rectangle terminalBtn = new Rectangle(-50, startY, btnWidth, btnHeight);
            if (DrawRibbonButton(terminalBtn, "Terminal", true))
            {
                TerminalSystem.Open(Engine.ActiveNPCs);
                Engine.CurrentState = Engine.GameState.Terminal;
            }


            // --- RIGHT SIDE BUTTONS ---
            // Reset Y
            startY = screenH / 2 - (btnHeight * 2);

            // X position for Right Side
            // specific placement: screenW - visible_amount
            // If we want 250px visible: X = screenW - 250. 
            // The rect width is 350. So X = screenW - 300 allows 50px overhang offscreen right.
            int rightX = screenW - 300;

            // 1. TV
            Rectangle tvBtn = new Rectangle(rightX, startY, btnWidth, btnHeight);
            if (DrawRibbonButton(tvBtn, "TV", false))
            {
                TVSystem.Open();
                Engine.CurrentState = Engine.GameState.TV;
            }
            startY += btnHeight + spacing;

            // 2. Diary (Living Room Only)
            if (SceneSystem.GetCurrentSceneName() == "Living Room")
            {
                Rectangle diaryBtn = new Rectangle(rightX, startY, btnWidth, btnHeight);
                if (DrawRibbonButton(diaryBtn, "Diary", false))
                {
                    DiarySystem.Open();
                }
            }
        }
    }
}

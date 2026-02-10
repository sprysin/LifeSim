using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static partial class UISystem
    {
        // New Ribbon Button Style
        // Inside UISystem.RibbonButtons.cs

        public static bool DrawRibbonButton(Rectangle rect, string text, bool isLeft)
        {
            float mouseX = Raylib.GetMouseX();
            float mouseY = Raylib.GetMouseY();
            bool isHovered = Raylib.CheckCollisionPointRec(new Vector2(mouseX, mouseY), rect);

            // --- Configuration ---
            float xOffset = isHovered ? 15 : 0;

            Color bodyColor = new Color(0, 0, 0, 220);
            Color glowColor = new Color(139, 69, 19, 255);
            Color brightGlow = new Color(160, 90, 40, 255);

            Rectangle drawRect;
            if (isLeft)
                drawRect = new Rectangle(rect.X + xOffset, rect.Y, rect.Width, rect.Height);
            else
                drawRect = new Rectangle(rect.X - xOffset, rect.Y, rect.Width, rect.Height);

            // --- 1. Draw Background Gradient ---
            if (isHovered)
            {
                Color start = isLeft ? glowColor : bodyColor;
                Color end = isLeft ? bodyColor : glowColor;
                Raylib.DrawRectangleGradientH((int)drawRect.X, (int)drawRect.Y, (int)drawRect.Width, (int)drawRect.Height, start, end);
            }
            else
            {
                Raylib.DrawRectangleRec(drawRect, bodyColor);
            }

            // --- 2. Halftone Texture (Hover Only) ---
            if (isHovered)
            {
                int dotSpacing = 7;
                int dotSize = 2;
                for (float x = drawRect.X; x < drawRect.X + drawRect.Width; x += dotSpacing)
                {
                    for (float y = drawRect.Y; y < drawRect.Y + drawRect.Height; y += dotSpacing)
                    {
                        float progress = isLeft ? 1.0f - ((x - drawRect.X) / drawRect.Width) : (x - drawRect.X) / drawRect.Width;
                        int alpha = (int)(100 * progress);
                        if (alpha > 0) Raylib.DrawCircle((int)x, (int)y, dotSize, new Color(255, 255, 255, alpha));
                    }
                }
            }

            // --- 3. Draw The Triangle Tip ---
            Vector2 v1, v2, v3;
            int tipSize = (int)(rect.Height / 2); // Length of the triangle point

            // We also need a specific center point for the diamond
            Vector2 diamondCenter;
            float diamondOffset = tipSize * 0.6f; // How far INSIDE the tip the diamond sits

            if (isLeft)
            {
                // Tip points Right
                v1 = new Vector2(drawRect.X + drawRect.Width, drawRect.Y); // Top Right
                v2 = new Vector2(drawRect.X + drawRect.Width, drawRect.Y + drawRect.Height); // Bottom Right
                v3 = new Vector2((drawRect.X + drawRect.Width + tipSize), drawRect.Y + (drawRect.Height / 2)); // The Point

                // Move diamond LEFT (negative X) from the tip (v3) so it sits inside the shape
                diamondCenter = new Vector2(v3.X - diamondOffset, v3.Y);
            }
            else
            {
                // Tip points Left
                float baseX = drawRect.X;
                float midY = drawRect.Y + drawRect.Height / 2f;

                v1 = new Vector2(baseX, drawRect.Y);
                v2 = new Vector2(baseX - tipSize, midY); // The Point
                v3 = new Vector2(baseX, drawRect.Y + drawRect.Height);

                // Move diamond RIGHT (positive X) from the tip (v2) so it sits inside the shape
                diamondCenter = new Vector2(v2.X + diamondOffset, v2.Y);
            }

            // Draw the triangular connector
            // Note: We use the same color as the body/gradient end so it blends seamlessly
            Color tipColor = isHovered ? (isLeft ? glowColor : glowColor) : bodyColor;
            Raylib.DrawTriangle(v1, v2, v3, tipColor);

            // --- 4. Draw the "Diamond Divot" ---
            // User requested "Much Larger" and "Inside". 
            // We use Color.Black to make it look like a hole/cutout, or brightGlow for a gem. 
            // Based on reference, let's use a Dark color to simulate a "hole" or "socket".
            float diamondSize = 18.0f; // Increased size (was approx 12 implicitly)

            // Draw the Diamond
            // 4 sides, 0 rotation = Diamond shape in Raylib
            Raylib.DrawPoly(diamondCenter, 4, diamondSize, 0, Color.Black);

            // Optional: Add a smaller colored "Gem" inside the black hole if hovered
            if (isHovered)
            {
                Raylib.DrawPoly(diamondCenter, 4, diamondSize * 0.6f, 0, brightGlow);
            }

            // --- 5. Text & Outline ---
            Raylib.DrawRectangleLinesEx(drawRect, 2, isHovered ? brightGlow : Color.Black);

            // Also draw outline for the triangle so the border is continuous
            // (This is a simple approximation, drawing lines between vertices)
            Color outlineColor = isHovered ? brightGlow : Color.Black;
            Raylib.DrawLineEx(v1, v3, 2, outlineColor);
            Raylib.DrawLineEx(v3, v2, 2, outlineColor);
            // Note: We don't draw v1-v2 line because that's where it connects to the rect

            int fontSize = 28;
            Vector2 textSize = Raylib.MeasureTextEx(FontMedium, text, fontSize, 1);
            Vector2 textPos = new Vector2(
                drawRect.X + (drawRect.Width - textSize.X) / 2,
                drawRect.Y + (drawRect.Height - textSize.Y) / 2
            );

            Raylib.DrawTextEx(FontMedium, text, new Vector2(textPos.X + 2, textPos.Y + 2), fontSize, 1, Color.Black);
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
                    Engine.CurrentState = Engine.GameState.Diary;
                }
            }
        }
    }
}
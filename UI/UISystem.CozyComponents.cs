using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // --- COZY UI COMPONENTS ---

        public static void DrawCozyPanel(Rectangle rect, string? title = null)
        {
            // Shadow / Glow
            Raylib.DrawRectangleRounded(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), 0.1f, 10, new Color(0, 0, 0, 100));

            // Background
            Raylib.DrawRectangleRounded(rect, 0.1f, 10, ColorEspresso);

            // Border
            Raylib.DrawRectangleRoundedLines(rect, 0.1f, 10, ColorTan);

            // Optional Title
            if (!string.IsNullOrEmpty(title))
            {
                // Title Background Pill
                Vector2 titleSize = Raylib.MeasureTextEx(FontMedium, title, 24, 2);
                float pillW = titleSize.X + 40;
                float pillH = 36;
                Rectangle titleRect = new Rectangle(rect.X + (rect.Width - pillW) / 2, rect.Y - (pillH / 2), pillW, pillH);

                Raylib.DrawRectangleRounded(titleRect, 0.5f, 10, ColorCharcoal);
                Raylib.DrawRectangleRoundedLines(titleRect, 0.5f, 10, ColorTan);

                // Text
                Vector2 textPos = new Vector2(titleRect.X + (titleRect.Width - titleSize.X) / 2, titleRect.Y + (titleRect.Height - titleSize.Y) / 2);
                Raylib.DrawTextEx(FontMedium, title, textPos, 24, 2, ColorTan);
            }
        }

        // Draws a cozy button. Returns true if clicked.
        public static bool DrawCozyButton(Rectangle rect, string text, bool isSelected)
        {
            bool clicked = false;
            Vector2 mousePos = Raylib.GetMousePosition();
            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, rect);

            // Color Logic
            Color bg = ColorEspresso;
            Color border = ColorTan;
            Color textColor = ColorCream;

            if (isSelected || isHovered)
            {
                bg = ColorWarmToffee;
                textColor = ColorCharcoal; // Dark text on light button
            }

            // Shadow
            if (!isSelected && !isHovered)
            {
                Raylib.DrawRectangleRounded(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), 0.2f, 10, new Color(0, 0, 0, 80));
            }

            // Button Body
            Raylib.DrawRectangleRounded(rect, 0.2f, 10, bg);
            Raylib.DrawRectangleRoundedLines(rect, 0.2f, 10, border);

            // Text
            Vector2 textSize = Raylib.MeasureTextEx(FontSmall, text, 20, 2);
            Vector2 textPos = new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2);
            Raylib.DrawTextEx(FontSmall, text, textPos, 20, 2, textColor);

            // Interaction
            if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                clicked = true;
            }

            return clicked;
        }

        public static void DrawCozyTextInput(Rectangle rect, string text, bool isFocused)
        {
            // Background
            Raylib.DrawRectangleRounded(rect, 0.1f, 10, new Color(20, 20, 20, 200)); // Darker slot

            Color borderColor = isFocused ? ColorTan : new Color(80, 80, 80, 255);
            Raylib.DrawRectangleRoundedLines(rect, 0.1f, 10, borderColor);

            // Text
            if (!string.IsNullOrEmpty(text))
            {
                // Clip text if too long? For now just draw.
                Raylib.DrawTextEx(FontSmall, text, new Vector2(rect.X + 10, rect.Y + (rect.Height - 20) / 2), 20, 1, ColorCream);
            }

            // Cursor
            if (isFocused && (int)(Raylib.GetTime() * 2) % 2 == 0)
            {
                Vector2 textSize = Raylib.MeasureTextEx(FontSmall, text, 20, 1);
                float cursorX = rect.X + 10 + textSize.X;
                Raylib.DrawLineEx(new Vector2(cursorX, rect.Y + 10), new Vector2(cursorX, rect.Y + rect.Height - 10), 2, ColorTan);
            }
        }

        private static void DrawBottomPanel(bool dialogueActive)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            int panelH = (int)(screenH * 0.3f);
            int panelY = screenH - panelH;

            // Calculate split: 75% dialogue area, 25% button panel
            int dialogueW = (int)(screenW * 0.75f);
            int buttonPanelW = screenW - dialogueW;

            // 1. Main Dialogue Area Panel (Semi-Transparent)
            Rectangle dialogueRect = new(0, panelY, dialogueW, panelH);

            // Semi-transparent background (allow NPC legs to show through)
            Color semiTransparentEspresso = new Color(
                (byte)ColorCharcoal.R,
                (byte)ColorCharcoal.G,
                (byte)ColorCharcoal.B,
                (byte)200  // Alpha ~200 for transparency
            );

            Raylib.DrawRectangleRounded(dialogueRect, 0.1f, 10, semiTransparentEspresso);
            Raylib.DrawRectangleRoundedLines(dialogueRect, 0.1f, 10, ColorTan);

            // 2. Button Side Panel (Full Opacity)
            Rectangle buttonPanelRect = new Rectangle(dialogueW, panelY, buttonPanelW, panelH);

            // Full opacity for button panel
            Raylib.DrawRectangleRounded(buttonPanelRect, 0.1f, 10, ColorEspresso);
            Raylib.DrawRectangleRoundedLines(buttonPanelRect, 0.1f, 10, ColorTan);

            // Visual separator line
            Raylib.DrawLineEx(
                new Vector2(dialogueW, panelY),
                new Vector2(dialogueW, panelY + panelH),
                3,
                ColorTan
            );

            // 3. Vertical Stack of Action Buttons (Larger)
            string[] btnLabels = { "THOUGHTS", "ACTION", "RESPOND" };
            int btnW = buttonPanelW - 40; // 20px padding each side
            int btnH = 60; // Increased from 35 to 60
            int spacing = 20;

            // Center buttons vertically
            int totalBtnH = (btnH * btnLabels.Length) + (spacing * (btnLabels.Length - 1));
            int startY = panelY + (panelH - totalBtnH) / 2;

            for (int i = 0; i < btnLabels.Length; i++)
            {
                Rectangle btnRect = new Rectangle(
                    dialogueW + 20,
                    startY + (i * (btnH + spacing)),
                    btnW,
                    btnH
                );

                bool isSelected = false; // Placeholder logic

                if (DrawCozyButton(btnRect, btnLabels[i], isSelected))
                {
                    // Click logic placeholder
                }
            }
        }
    }
}

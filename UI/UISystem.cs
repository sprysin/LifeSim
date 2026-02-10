using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Retro Assets
        public static Font FontHuge;   // Size 128 (High Res Master)
        public static Font FontTiny;   // Keep for fallback/small widgets
        public static Font FontSmall;  // Keep for legacy references
        public static Font FontMedium; // Size 32 (Title)
        public static Font FontLarge;  // Size 40 (Prompt)
        public static Font FontTitle;  // Size 48 (Homescreen Title)

        // --- COZY DESIGN SYSTEM COLORS ---
        public static readonly Color ColorCharcoal = new Color(28, 28, 28, 255);    // #1C1C1C
        public static readonly Color ColorEspresso = new Color(46, 26, 18, 255);    // #2E1A12
        public static readonly Color ColorTan = new Color(210, 180, 140, 255);      // #D2B48C
        public static readonly Color ColorCream = new Color(245, 232, 216, 255);    // #F5E8D8
        public static readonly Color ColorWarmToffee = new Color(180, 140, 100, 255); // #B48C64
        // ---------------------------------

        // UI Zones
        public static Rectangle ExitButtonRect;
        public static Rectangle TerminalButtonRect;
        public static Rectangle TVButtonRect;
        public static Rectangle OptionThoughtsRect;
        public static Rectangle OptionActionRect;
        public static Rectangle OptionRespondRect;

        private const int GridSize = 10;

        public static void DrawGrid(int x, int y, int w, int h, float offset, Color gridColor)
        {
            // Vertical lines (scrolling)
            for (int i = -1; i <= w / GridSize + 1; i++)
            {
                float lineX = x + (i * GridSize) + offset;
                if (lineX >= x && lineX <= x + w)
                {
                    Raylib.DrawLine((int)lineX, y, (int)lineX, y + h, gridColor);
                }
            }

            // Horizontal lines (static)
            for (int j = 0; j <= h / GridSize; j++)
            {
                int lineY = y + (j * GridSize);
                Raylib.DrawLine(x, lineY, x + w, lineY, gridColor);
            }
        }

        public static RenderTexture2D UIBuffer;
        public const int VirtualWidth = 320;
        public const int VirtualHeight = 180;

        // Removed bubbleTexture as it is no longer used and file is deleted.

        public static void Initialize()
        {
            // Load Fonts
            // 1. Main Font Check
            string mainFontPath = "pokemon-b-w.otf/pokemon-b-w.otf";
            Font commonFont = Raylib.GetFontDefault();

            if (System.IO.File.Exists(mainFontPath))
            {
                // Load at high resolution for scaling
                commonFont = Raylib.LoadFontEx(mainFontPath, 96, null, 0);
            }

            // Assign to all standard fonts
            FontHuge = commonFont;
            FontTiny = commonFont;
            FontSmall = commonFont;
            FontMedium = commonFont;
            FontLarge = commonFont;

            // 2. Title Font (Keep Megamax)
            string titleFontPath = "pokemon-b-w.otf/MegamaxJonathanToo-YqOq2.ttf";
            if (System.IO.File.Exists(titleFontPath))
            {
                FontTitle = Raylib.LoadFontEx(titleFontPath, 96, null, 0);
            }
            else
            {
                FontTitle = Raylib.GetFontDefault();
            }

            // Set filter to Point for all to ensure crisp edges
            Raylib.SetTextureFilter(FontHuge.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontTiny.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontSmall.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontMedium.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontLarge.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontTitle.Texture, TextureFilter.Point);

            // Initialize Layout
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();

            // 1. Action Buttons
            ExitButtonRect = new Rectangle(20, 20, 100, 40);
            TerminalButtonRect = new Rectangle(20, sh / 2 - 40, 120, 80);
            TVButtonRect = new Rectangle(sw - 140, sh / 2 - 40, 120, 80);

            // 2. Bottom Panel Layout
            int panelH = (int)(sh * 0.3f);
            int panelY = sh - panelH;
            int panelW = sw;

            // Split: 75% Dialogue, 25% Options
            int dialogW = (int)(panelW * 0.75f);
            int optionsW = panelW - dialogW;
            int optionsX = dialogW;

            // Option Buttons (Stacked Vertically in Options Area)
            int btnW = optionsW - 40; // 20px padding each side
            int btnH = 50;
            int spacing = 20;

            // Center vertically in the options panel
            int totalBtnH = (btnH * 3) + (spacing * 2);
            int startY = panelY + (panelH - totalBtnH) / 2;

            OptionThoughtsRect = new Rectangle(optionsX + 20, startY, btnW, btnH);
            OptionActionRect = new Rectangle(optionsX + 20, startY + btnH + spacing, btnW, btnH);
            OptionRespondRect = new Rectangle(optionsX + 20, startY + (btnH + spacing) * 2, btnW, btnH);

            // Setup Render Texture for pixelated UI effect (Legacy/Menu only)
            UIBuffer = Raylib.LoadRenderTexture(VirtualWidth, VirtualHeight);

            // Initialize Terminal System (Loads Dialogue)
            TerminalSystem.Initialize();
        }

        public static void DrawStaticUI(bool dialogueActive)
        {
            // Draw Top Info Band
            DrawTopInfoBand();

            // Draw Action Buttons (Scene Interactors)
            DrawActionButtons();

            // Draw Bottom Panel (Dialogue + Player Options)
            DrawBottomPanel(dialogueActive);
        }

        private static void DrawTopInfoBand()
        {
            int screenW = Raylib.GetScreenWidth();
            int bandHeight = 40;

            Rectangle bandRect = new Rectangle(0, 0, screenW, bandHeight);

            // Background
            Raylib.DrawRectangleRec(bandRect, ColorEspresso);

            // Bottom border
            Raylib.DrawLineEx(
                new Vector2(0, bandHeight),
                new Vector2(screenW, bandHeight),
                2,
                ColorTan
            );

            // Left: Game title
            Raylib.DrawTextEx(FontSmall, "LifeSim", new Vector2(20, 10), 20, 1, ColorTan);

            // Center: Current room name
            string roomName = SceneSystem.GetCurrentSceneName();
            Vector2 roomNameSize = Raylib.MeasureTextEx(FontMedium, roomName, 24, 1);
            Raylib.DrawTextEx(FontMedium, roomName, new Vector2((screenW - roomNameSize.X) / 2, 8), 24, 1, ColorCream);

            // Right: Current time in PST (AM/PM format)
            System.DateTime now = System.DateTime.Now;
            System.TimeZoneInfo pstZone = System.TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            System.DateTime pstTime = System.TimeZoneInfo.ConvertTime(now, pstZone);
            string timeText = pstTime.ToString("h:mm tt") + " PST";  // e.g., "4:54 PM PST"

            Vector2 timeSize = Raylib.MeasureTextEx(FontSmall, timeText, 20, 1);
            Raylib.DrawTextEx(FontSmall, timeText, new Vector2(screenW - timeSize.X - 20, 10), 20, 1, ColorCream);
        }

        private static void DrawActionButtons()
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Exit Button (Top Left) - overhangs left
            Rectangle exitRect = new Rectangle(20, 50, 100, 40);
            DrawOverhangButton("EXIT", exitRect, true);

            // Terminal Button (Left Center) - overhangs left
            Rectangle terminalRect = new Rectangle(20, screenH / 2 - 40, 120, 80);
            DrawOverhangButton("TERMINAL", terminalRect, true);

            // TV Button (Right Center) - overhangs right
            Rectangle tvRect = new Rectangle(screenW - 140, screenH / 2 - 40, 120, 80);
            DrawOverhangButton("TV", tvRect, false);
        }

        // Overhang Button for Scene Interactions (Exit, Terminal, TV)
        // Returns true if clicked
        private static System.Collections.Generic.Dictionary<string, float> overhangAnimations = new System.Collections.Generic.Dictionary<string, float>();
        public static bool DrawOverhangButton(string label, Rectangle baseRect, bool overhangsLeft)
        {
            Vector2 mousePos = Raylib.GetMousePosition();

            // Extend rectangle to include overhang area for hover detection
            Rectangle extendedRect = baseRect;
            if (overhangsLeft)
            {
                extendedRect.X -= 15;
                extendedRect.Width += 15;
            }
            else // overhangs right
            {
                extendedRect.Width += 15;
            }

            bool isHovered = Raylib.CheckCollisionPointRec(mousePos, extendedRect);

            // Animation state
            if (!overhangAnimations.ContainsKey(label))
            {
                overhangAnimations[label] = 0f;
            }

            // Smooth lerp animation
            float targetOffset = isHovered ? 5f : 0f;
            overhangAnimations[label] += (targetOffset - overhangAnimations[label]) * 0.2f;
            float currentOffset = overhangAnimations[label];

            // Calculate final position with overhang and animation
            Rectangle drawRect = baseRect;
            if (overhangsLeft)
            {
                drawRect.X = baseRect.X - 15 - currentOffset;
            }
            else // overhangs right
            {
                drawRect.X = baseRect.X + currentOffset;
            }

            // Draw button with Cozy styling
            Color bg = isHovered ? ColorWarmToffee : ColorEspresso;
            Color border = isHovered ? ColorCream : ColorTan;
            Color textColor = isHovered ? ColorCharcoal : ColorCream;

            // Shadow
            Raylib.DrawRectangleRounded(
                new Rectangle(drawRect.X + 2, drawRect.Y + 2, drawRect.Width, drawRect.Height),
                0.2f, 10, new Color(0, 0, 0, 100)
            );

            // Background
            Raylib.DrawRectangleRounded(drawRect, 0.2f, 10, bg);

            // Border
            Raylib.DrawRectangleRoundedLines(drawRect, 0.2f, 10, border);

            // Text
            Vector2 textSize = Raylib.MeasureTextEx(FontMedium, label, 24, 1);
            Vector2 textPos = new Vector2(
                drawRect.X + (drawRect.Width - textSize.X) / 2,
                drawRect.Y + (drawRect.Height - textSize.Y) / 2
            );
            Raylib.DrawTextEx(FontMedium, label, textPos, 24, 1, textColor);

            // Check click (must be hovered AND in extended rect)
            bool clicked = isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
            return clicked;
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
            Rectangle dialogueRect = new Rectangle(0, panelY, dialogueW, panelH);

            // Semi-transparent background (allow NPC legs to show through)
            Color semiTransparentEspresso = new Color(
                (byte)ColorEspresso.R,
                (byte)ColorEspresso.G,
                (byte)ColorEspresso.B,
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

        public static void DrawSharpButton(string text, Rectangle rect)
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            bool hovered = Raylib.CheckCollisionPointRec(mousePos, rect);

            Color fillColor = hovered ? Color.White : Color.Black;
            Color textColor = hovered ? Color.Black : Color.White;
            Color borderColor = Color.White;

            Raylib.DrawRectangleRec(rect, fillColor);
            Raylib.DrawRectangleLinesEx(rect, 2, borderColor);

            // Center Text
            // Use FontMedium (Size 32)
            // Measure first
            Vector2 textSize = Raylib.MeasureTextEx(FontMedium, text, 24, 1);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );

            Raylib.DrawTextEx(FontMedium, text, textPos, 24, 1, textColor);
        }

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

        /// <summary>
        /// Draws a cozy button. Returns true if clicked.
        /// </summary>
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

        public static void DrawFade(float alpha)
        {
            if (alpha <= 0) return;
            if (alpha > 1) alpha = 1;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            Color fadeColor = new Color(0, 0, 0, (int)(alpha * 255));
            Raylib.DrawRectangle(0, 0, screenW, screenH, fadeColor);
        }

        public static void DrawPrompt(int gridX, int gridY)
        {
            // Legacy / Deprecated
        }

        public static void DrawPrompt(NPC npc)
        {
            // Legacy / Deprecated
        }

        public static void DrawDebugInfo(int gridX, int gridY)
        {
            string text = $"Tile: {gridX}, {gridY}";
            Raylib.DrawText(text, 10, 10, 20, Color.White);
        }
    }
}

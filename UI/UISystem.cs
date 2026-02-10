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

        // Draw Top Info Band
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

            // TopLeft: EXIT Button
            Rectangle exitBtnRect = new Rectangle(10, 5, 80, 30);
            bool exitHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), exitBtnRect);

            if (exitHover)
            {
                Raylib.DrawRectangleRec(exitBtnRect, new Color(255, 255, 255, 30)); // Subtle highlight
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Engine.CurrentState = Engine.GameState.Menu;
                }
            }

            Raylib.DrawTextEx(FontMedium, "EXIT", new Vector2(25, 8), 24, 1, exitHover ? Color.White : ColorTan);

            // Center: Current room name
            string roomName = SceneSystem.GetCurrentSceneName();
            Vector2 roomNameSize = Raylib.MeasureTextEx(FontMedium, roomName, 24, 1);
            Raylib.DrawTextEx(FontMedium, roomName, new Vector2((screenW - roomNameSize.X) / 2, 8), 24, 1, ColorCream);

            // Right: Current time in PST (AM/PM format)
            System.DateTime now = System.DateTime.Now;
            System.TimeZoneInfo pstZone = System.TimeZoneInfo.CreateCustomTimeZone("PST", new System.TimeSpan(-8, 0, 0), "Pacific Standard Time", "PST");
            System.DateTime pstTime = System.TimeZoneInfo.ConvertTime(now, pstZone);
            string timeText = pstTime.ToString("h:mm tt") + " PST";  // e.g., "4:54 PM PST"

            Vector2 timeSize = Raylib.MeasureTextEx(FontSmall, timeText, 20, 1);
            Raylib.DrawTextEx(FontSmall, timeText, new Vector2(screenW - timeSize.X - 20, 12), 20, 1, ColorTan);
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

        public static void DrawDebugInfo(int gridX, int gridY)
        {
            string text = $"Tile: {gridX}, {gridY}";
            Raylib.DrawText(text, 10, 10, 20, Color.White);
        }
    }
}

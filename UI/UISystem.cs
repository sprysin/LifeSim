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
            // Draw Action Buttons (Scene Interactors)
            DrawActionButtons();

            // Draw Bottom Panel (Dialogue + Player Options)
            DrawBottomPanel(dialogueActive);
        }

        private static void DrawActionButtons()
        {
            // Exit Button (Top Left)
            DrawSharpButton("EXIT", ExitButtonRect);

            // Terminal Button (Left Center)
            DrawSharpButton("TERMINAL", TerminalButtonRect);

            // TV Button (Right Center)
            DrawSharpButton("TV", TVButtonRect);
        }

        private static void DrawBottomPanel(bool dialogueActive)
        {
            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();
            int panelH = (int)(screenH * 0.3f);
            int panelY = screenH - panelH;
            int dialogW = (int)(screenW * 0.75f);
            int optionsX = dialogW;

            // 1. Panel Backgrounds

            // Dialogue Area
            Raylib.DrawRectangle(0, panelY, dialogW, panelH, Color.Black);
            Raylib.DrawRectangleLinesEx(new Rectangle(0, panelY, dialogW, panelH), 2, Color.White);

            // Options Area
            Raylib.DrawRectangle(optionsX, panelY, screenW - optionsX, panelH, Color.Black);
            Raylib.DrawRectangleLinesEx(new Rectangle(optionsX, panelY, screenW - optionsX, panelH), 2, Color.White);

            // 2. Draw Text (Dialogue) - Placeholder or Integration?
            // Existing 'DrawDialogue' might draw over this? 
            // We need to ensure text is drawn *here* or that DrawDialogue renders into this box.
            // For now, let's just draw the frames. 'DrawDialogue' logic handles the content.
            // But DrawDialogue uses UIBuffer... we need to fix that if we want high res text.
            // Let's assume for this step we just setup the containers.

            // 3. Draw Option Buttons
            DrawSharpButton("THOUGHTS", OptionThoughtsRect);
            DrawSharpButton("ACTION", OptionActionRect);
            DrawSharpButton("RESPOND", OptionRespondRect);
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

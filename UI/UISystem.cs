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


        public static RenderTexture2D UIBuffer;
        public const int VirtualWidth = 320;
        public const int VirtualHeight = 180;

        // Menu Background
        private static Texture2D bubbleTexture;

        public static void Initialize()
        {
            // Load High Res Master Font
            FontHuge = Raylib.LoadFontEx("pokemon-b-w.otf/pokemon-b-w.otf", 128, null, 0);
            Raylib.SetTextureFilter(FontHuge.Texture, TextureFilter.Point);

            // Keep Small fonts for logical measurements or unmigrated UI
            FontTiny = Raylib.LoadFontEx("pokemon-b-w.otf/pokemon-b-w.otf", 10, null, 0);
            FontSmall = Raylib.LoadFontEx("pokemon-b-w.otf/pokemon-b-w.otf", 12, null, 0);
            FontMedium = Raylib.LoadFontEx("pokemon-b-w.otf/pokemon-b-w.otf", 32, null, 0);
            FontLarge = Raylib.LoadFontEx("pokemon-b-w.otf/pokemon-b-w.otf", 40, null, 0);

            // Set filter to Point for all to ensure crisp edges
            Raylib.SetTextureFilter(FontTiny.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontSmall.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontMedium.Texture, TextureFilter.Point);
            Raylib.SetTextureFilter(FontLarge.Texture, TextureFilter.Point);


            // Setup Render Texture for pixelated UI effect
            UIBuffer = Raylib.LoadRenderTexture(VirtualWidth, VirtualHeight);
            Raylib.SetTextureFilter(UIBuffer.Texture, TextureFilter.Point);

            // Load Bubble Texture
            bubbleTexture = Raylib.LoadTexture("Tilesets/bubble.png");

            // Initialize Terminal System (Loads Dialogue)
            TerminalSystem.Initialize();
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
            int scale = 4;
            int tileSize = TileSystem.TileSize * scale;

            // Draw at location
            int x = gridX * tileSize + (tileSize / 2);
            int y = (gridY * tileSize) - 40; // Slightly higher

            string text = "X";
            int fontSize = 40;

            if (bubbleTexture.Id != 0)
            {
                // Draw Custom Bubble
                // Center texture
                float bubbleScale = 4.5f;
                float texX = x - ((bubbleTexture.Width * bubbleScale) / 2.0f);
                float texY = y - ((bubbleTexture.Height * bubbleScale) / 2.0f);

                Raylib.DrawTextureEx(bubbleTexture, new Vector2(texX, texY), 0.0f, bubbleScale, Color.White);

                // Draw Text centered on bubble
                Vector2 textSize = Raylib.MeasureTextEx(FontLarge, text, fontSize, 1.0f);


                // Adjust text position relative to bubble layout (assuming centered)
                int textX = x - (int)(textSize.X / 2);
                int textY = y - (int)(textSize.Y / 2);

                Raylib.DrawTextEx(FontLarge, text, new Vector2(textX, textY), fontSize, 1.0f, Color.Black);

            }
            else
            {
                // Fallback (Original procedural)
                y = (gridY * tileSize) - 30;
                Vector2 textSize = Raylib.MeasureTextEx(FontLarge, text, fontSize, 1.0f);

                int padding = 8;
                Rectangle bubbleRect = new Rectangle(
                    x - (textSize.X / 2) - padding,
                    y - (textSize.Y / 2) - padding,
                    textSize.X + (padding * 2),
                    textSize.Y + (padding * 2)
                );
                Raylib.DrawRectangleRounded(bubbleRect, 0.3f, 6, Color.White);
                Raylib.DrawRectangleRoundedLines(bubbleRect, 0.3f, 6, Color.Black);
                Raylib.DrawTextEx(FontLarge, text, new Vector2(x - textSize.X / 2, y - textSize.Y / 2), fontSize, 1.0f, Color.Black);

            }
        }

        public static void DrawPrompt(NPC npc)
        {
            DrawPrompt(npc.GridX, npc.GridY);
        }

        public static void DrawDebugInfo(int gridX, int gridY)
        {
            string text = $"Tile: {gridX}, {gridY}";
            Raylib.DrawText(text, 10, 10, 20, Color.White);
        }
    }
}

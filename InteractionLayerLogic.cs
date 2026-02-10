using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static class InteractionLayerLogic
    {
        private static Image InteractionLayer;
        private static bool HasInteractionLayer = false;
        private static int GridWidth;
        private static int GridHeight;
        private static int TileSize = TileSystem.TileSize;

        public static void Load(string interactionPath, int width, int height)
        {
            if (HasInteractionLayer)
            {
                Raylib.UnloadImage(InteractionLayer);
                HasInteractionLayer = false;
            }

            GridWidth = width;
            GridHeight = height;

            if (!string.IsNullOrEmpty(interactionPath) && System.IO.File.Exists(interactionPath))
            {
                InteractionLayer = Raylib.LoadImage(interactionPath);
                HasInteractionLayer = true;
            }
        }

        public static void Unload()
        {
            if (HasInteractionLayer)
            {
                Raylib.UnloadImage(InteractionLayer);
                HasInteractionLayer = false;
            }
        }

        public static Color GetInteractionColor(int gridX, int gridY)
        {
            if (!HasInteractionLayer) return Color.Blank;
            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight) return Color.Blank;

            int pixelX = gridX * TileSize + (TileSize / 2);
            int pixelY = gridY * TileSize + (TileSize / 2);

            if (pixelX < InteractionLayer.Width && pixelY < InteractionLayer.Height)
            {
                return Raylib.GetImageColor(InteractionLayer, pixelX, pixelY);
            }
            return Color.Blank;
        }

        public static bool IsWalkable(int gridX, int gridY, int currentSceneIndex)
        {
            // Noclip for Debug Room (CurrentSceneIndex == 0)
            if (currentSceneIndex == 0) return true;

            // Interaction Layer Check
            Color pixelColor = GetInteractionColor(gridX, gridY);

            // Check for BLOCKED #ed1c5c (R:237, G:28, B:92)
            if (pixelColor.R == 237 && pixelColor.G == 28 && pixelColor.B == 92)
            {
                return false;
            }

            // Check for TERMINAL #4DA6FFFF (Blocked)
            if (pixelColor.R == 77 && pixelColor.G == 166 && pixelColor.B == 255)
            {
                return false;
            }

            // Check for TV #386cdb (Blocked)
            if (pixelColor.R == 56 && pixelColor.G == 108 && pixelColor.B == 219)
            {
                return false;
            }

            // Check for DIARY #a8b6d3 (Blocked)
            // R:168, G:182, B:211
            if (pixelColor.R == 168 && pixelColor.G == 182 && pixelColor.B == 211)
            {
                return false;
            }

            return true;
        }

        public static bool IsTerminal(int gridX, int gridY)
        {
            Color pixelColor = GetInteractionColor(gridX, gridY);
            // #4DA6FFFF -> R:77, G:166, B:255
            return (pixelColor.R == 77 && pixelColor.G == 166 && pixelColor.B == 255);
        }

        public static bool IsTV(int gridX, int gridY)
        {
            Color pixelColor = GetInteractionColor(gridX, gridY);
            // #386cdb -> R:56, G:108, B:219
            return (pixelColor.R == 56 && pixelColor.G == 108 && pixelColor.B == 219);
        }

        public static bool IsDiary(int gridX, int gridY)
        {
            Color pixelColor = GetInteractionColor(gridX, gridY);
            // #a8b6d3 -> R:168, G:182, B:211
            return (pixelColor.R == 168 && pixelColor.G == 182 && pixelColor.B == 211);
        }

        public static bool IsExit(int gridX, int gridY, int currentSceneIndex)
        {
            // Special case for Debug Room (Scene 0) Red Tile at (5, 5)
            if (currentSceneIndex == 0)
            {
                if (gridX == 5 && gridY == 5) return true;
                return false;
            }

            Color pixelColor = GetInteractionColor(gridX, gridY);
            // #cc33cc -> R:204, G:51, B:204
            return (pixelColor.R == 204 && pixelColor.G == 51 && pixelColor.B == 204);
        }

        public static bool IsSitSpot(int gridX, int gridY)
        {
            Color pixelColor = GetInteractionColor(gridX, gridY);
            // #b3f237 -> R:179, G:242, B:55
            return (pixelColor.R == 179 && pixelColor.G == 242 && pixelColor.B == 55);
        }

        public static Texture2D GenerateForegroundTexture(string bgPath)
        {
            if (!HasInteractionLayer || !System.IO.File.Exists(bgPath))
            {
                return new Texture2D(); // Empty/Null texture
            }

            // Load background image
            Image bgImage = Raylib.LoadImage(bgPath);

            // Create blank image for foreground (same size as background)
            Image foregroundImage = Raylib.GenImageColor(bgImage.Width, bgImage.Height, Color.Blank);

            // Scan interaction layer for foreground markers (#e4a209)
            bool foundForeground = false;
            for (int y = 0; y < InteractionLayer.Height && y < bgImage.Height; y++)
            {
                for (int x = 0; x < InteractionLayer.Width && x < bgImage.Width; x++)
                {
                    Color interactionPixel = Raylib.GetImageColor(InteractionLayer, x, y);

                    // Check if pixel is foreground marker #e4a209 (R:228, G:162, B:9)
                    if (interactionPixel.R == 228 && interactionPixel.G == 162 && interactionPixel.B == 9)
                    {
                        // Copy background pixel to foreground
                        Color bgPixel = Raylib.GetImageColor(bgImage, x, y);
                        Raylib.ImageDrawPixel(ref foregroundImage, x, y, bgPixel);
                        foundForeground = true;
                    }
                }
            }

            Texture2D result = new Texture2D();

            // Only create texture if we found foreground pixels
            if (foundForeground)
            {
                result = Raylib.LoadTextureFromImage(foregroundImage);
            }

            // Cleanup
            Raylib.UnloadImage(foregroundImage);
            Raylib.UnloadImage(bgImage);

            return result;
        }
    }
}

using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static class TileSystem
    {
        // 16x16 pixels per tile (Standard 16-bit style)
        public const int TileSize = 16;

        public class Metatile
        {
            public int Width;
            public int Height;
            public int[] TileIndices;

            public Metatile(int w, int h, int[] indices)
            {
                Width = w;
                Height = h;
                TileIndices = indices;
            }
        }

        public class NPCSpawnData
        {
            public int X;
            public int Y;
            public string Name;
            public string Text;
            public string Portrait;
            public string SpritePath;
            public float Scale;

            public NPCSpawnData(int x, int y, string name, string text, string portrait, string spritePath = "", float scale = 1.0f)
            {
                X = x; Y = y; Name = name; Text = text; Portrait = portrait; SpritePath = spritePath; Scale = scale;
            }
        }

        public class SceneData
        {
            public string Name;
            public string BgPath;
            public string InteractionPath;
            public Vector2 PlayerSpawn; // Grid Coordinates
            public List<NPCSpawnData> NPCs = new List<NPCSpawnData>();
            public int ExitCount = 0;

            public SceneData(string name, string bg, string interaction, Vector2 spawn, int exits = 0)
            {
                Name = name; BgPath = bg; InteractionPath = interaction; PlayerSpawn = spawn; ExitCount = exits;
            }
        }

        // The master tileset texture
        public static Texture2D MasterTileset;
        // The room background (separate)
        public static Texture2D RoomBackground;

        // Debug
        public static bool ShowGrid = false;

        // Scene management
        public static List<SceneData> Scenes = new List<SceneData>();
        public static int CurrentSceneIndex = 0;

        private static Dictionary<string, Metatile> Metatiles = new Dictionary<string, Metatile>();

        // Collision Image
        private static Image InteractionLayer;
        private static bool HasInteractionLayer = false;

        // Foreground Layer (rendered on top of player)
        private static Texture2D ForegroundTexture;
        private static bool HasForeground = false;

        // Simple 10x10 room grid (default)
        public static int GridWidth = 10;
        public static int GridHeight = 10;
        private static int[,] RoomGrid = new int[10, 10];

        public static void Initialize()
        {
            if (!System.IO.Directory.Exists("Tilesets"))
            {
                System.IO.Directory.CreateDirectory("Tilesets");
            }

            string tilesetPath = "Tilesets/master.png";

            if (!System.IO.File.Exists(tilesetPath))
            {
                Image img = Raylib.GenImageChecked(64, 64, 8, 8, Color.Brown, Color.DarkGray);
                Raylib.ExportImage(img, tilesetPath);
                Raylib.UnloadImage(img);
            }

            MasterTileset = Raylib.LoadTexture(tilesetPath);

            // Define Scenes
            // Debug Room (formerly Living Room)
            SceneData debugRoom = new SceneData("Debug Room", "Tilesets/output.png", "", new Vector2(2, 2), 1); // Spawn (2,2)
            debugRoom.NPCs.Add(new NPCSpawnData(1, 3, "Testern", "Hello! I am Testern. Welcome to the Debug Room!", "Visual Novel Images/MC/main_char_full_body.png")); // Testern (1,3)

            // Kitchen
            SceneData kitchen = new SceneData("Kitchen", "Tilesets/kitchen.png", "Tilesets/interaction Layer_Kitchen.png", new Vector2(4, 5), 1);
            // Add Boogie
            kitchen.NPCs.Add(new NPCSpawnData(9, 4, "Boogie", "Leave me be.", "Visual Novel Images/MC/main_char_full_body.png", "Character_Sheets/boogie_sprite_sheet.png", 1.0f));

            // Living Room
            SceneData livingRoom = new SceneData("Living Room", "Tilesets/Living_room.png", "Tilesets/interaction Layer_Living_room.png", new Vector2(4, 5), 1);
            livingRoom.NPCs.Add(new NPCSpawnData(9, 4, "Boogie", "Leave me be.", "Visual Novel Images/MC/main_char_full_body.png", "Character_Sheets/boogie_sprite_sheet.png", 1.0f));

            Scenes.Add(debugRoom);
            Scenes.Add(kitchen);
            Scenes.Add(livingRoom);

            // Metatile Setup
            Metatiles["Rug"] = new Metatile(2, 2, new int[] { 18, 19, 26, 27 });
        }

        public static void LoadScene(int sceneIndex, Player? player = null, List<NPC>? npcs = null)
        {
            if (sceneIndex < 0 || sceneIndex >= Scenes.Count) return;

            CurrentSceneIndex = sceneIndex;
            SceneData data = Scenes[sceneIndex];

            // Unload previous background if it exists
            if (RoomBackground.Id != 0)
            {
                Raylib.UnloadTexture(RoomBackground);
                RoomBackground.Id = 0;
            }
            if (HasInteractionLayer)
            {
                Raylib.UnloadImage(InteractionLayer);
                HasInteractionLayer = false;
            }
            if (HasForeground)
            {
                Raylib.UnloadTexture(ForegroundTexture);
                HasForeground = false;
            }

            // Load new background
            if (System.IO.File.Exists(data.BgPath))
            {
                RoomBackground = Raylib.LoadTexture(data.BgPath);
                GridWidth = RoomBackground.Width / TileSize;
                GridHeight = RoomBackground.Height / TileSize;
            }

            // Load Interaction Layer
            if (!string.IsNullOrEmpty(data.InteractionPath) && System.IO.File.Exists(data.InteractionPath))
            {
                InteractionLayer = Raylib.LoadImage(data.InteractionPath);
                HasInteractionLayer = true;

                // Extract Foreground Layer
                ExtractForegroundLayer(data.BgPath);
            }

            // Reset NPCs if list provided
            if (npcs != null)
            {
                npcs.Clear();
                foreach (var n in data.NPCs)
                {
                    NPC npc = new NPC(n.X, n.Y, n.Name, n.Text, n.Portrait, n.SpritePath, n.Scale);
                    if (npc.Name == "Testern")
                    {
                        npc.SetConversation(new List<string> { "Now Scram!" });
                    }
                    npcs.Add(npc);
                }
            }

            // Position Player if provided
            if (player != null)
            {
                player.GridX = (int)data.PlayerSpawn.X;
                player.GridY = (int)data.PlayerSpawn.Y;
            }

            // Resize Grid
            RoomGrid = new int[GridWidth, GridHeight];

            // Initialize Grid (walls logic is rudimentary here, mostly relies on visual BG now)
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    // Clear grid
                    RoomGrid[x, y] = 0;
                }
            }
        }

        public static bool IsWalkable(int gridX, int gridY)
        {
            // Noclip for Debug Room (CurrentSceneIndex == 0)
            if (CurrentSceneIndex == 0) return true;

            // Grid Bounds Check
            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                return false;

            // Interaction Layer Check
            if (HasInteractionLayer)
            {
                // Check center of the tile
                int pixelX = gridX * TileSize + (TileSize / 2);
                int pixelY = gridY * TileSize + (TileSize / 2);

                if (pixelX < InteractionLayer.Width && pixelY < InteractionLayer.Height)
                {
                    Color pixelColor = Raylib.GetImageColor(InteractionLayer, pixelX, pixelY);

                    // Check for BLOCKED #ed1c5c (R:237, G:28, B:92)
                    if (pixelColor.R == 237 && pixelColor.G == 28 && pixelColor.B == 92)
                    {
                        return false;
                    }

                    // Check for TERMINAL #4DA6FFFF (Blocked)
                    // R:77, G:166, B:255
                    if (pixelColor.R == 77 && pixelColor.G == 166 && pixelColor.B == 255)
                    {
                        return false;
                    }

                    // Check for MAGENTA #D900FFFF (Exit) -> Walkable but triggers transition
                    // if it's an exit, it should be walkable.
                }
            }

            return true;
        }

        private static void ExtractForegroundLayer(string bgPath)
        {
            if (!HasInteractionLayer || !System.IO.File.Exists(bgPath))
            {
                HasForeground = false;
                return;
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

            // Only create texture if we found foreground pixels
            if (foundForeground)
            {
                ForegroundTexture = Raylib.LoadTextureFromImage(foregroundImage);
                HasForeground = true;
            }
            else
            {
                HasForeground = false;
            }

            // Cleanup
            Raylib.UnloadImage(foregroundImage);
            Raylib.UnloadImage(bgImage);
        }

        public static bool IsTerminal(int gridX, int gridY)
        {
            if (!HasInteractionLayer) return false;

            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                return false;

            int pixelX = gridX * TileSize + (TileSize / 2);
            int pixelY = gridY * TileSize + (TileSize / 2);

            if (pixelX < InteractionLayer.Width && pixelY < InteractionLayer.Height)
            {
                Color pixelColor = Raylib.GetImageColor(InteractionLayer, pixelX, pixelY);

                // #4DA6FFFF -> R:77, G:166, B:255
                if (pixelColor.R == 77 && pixelColor.G == 166 && pixelColor.B == 255)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsExit(int gridX, int gridY)
        {
            // Special case for Debug Room (Scene 0) Red Tile at (5, 5)
            if (CurrentSceneIndex == 0)
            {
                if (gridX == 5 && gridY == 5) return true;
                return false;
            }

            if (!HasInteractionLayer) return false;

            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                return false;

            int pixelX = gridX * TileSize + (TileSize / 2);
            int pixelY = gridY * TileSize + (TileSize / 2);

            if (pixelX < InteractionLayer.Width && pixelY < InteractionLayer.Height)
            {
                Color pixelColor = Raylib.GetImageColor(InteractionLayer, pixelX, pixelY);

                // #cc33cc -> R:204, G:51, B:204
                if (pixelColor.R == 204 && pixelColor.G == 51 && pixelColor.B == 204)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSitSpot(int gridX, int gridY)
        {
            if (!HasInteractionLayer) return false;

            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                return false;

            int pixelX = gridX * TileSize + (TileSize / 2);
            int pixelY = gridY * TileSize + (TileSize / 2);

            if (pixelX < InteractionLayer.Width && pixelY < InteractionLayer.Height)
            {
                Color pixelColor = Raylib.GetImageColor(InteractionLayer, pixelX, pixelY);
                // #b3f237 -> R:179, G:242, B:55
                if (pixelColor.R == 179 && pixelColor.G == 242 && pixelColor.B == 55)
                {
                    return true;
                }
            }

            return false;
        }

        public static void PlaceMetatile(string name, int startX, int startY)
        {
            if (!Metatiles.ContainsKey(name)) return;

            Metatile meta = Metatiles[name];

            for (int y = 0; y < meta.Height; y++)
            {
                for (int x = 0; x < meta.Width; x++)
                {
                    int gridX = startX + x;
                    int gridY = startY + y;

                    if (gridX >= 0 && gridX < GridWidth && gridY >= 0 && gridY < GridHeight)
                    {
                        RoomGrid[gridX, gridY] = meta.TileIndices[y * meta.Width + x];
                    }
                }
            }
        }

        public static void DrawBackground()
        {
            // If we have a custom BG, draw it
            if (RoomBackground.Id != 0)
            {
                // Draw it scaled 4x like everything else
                int scale = 4;
                Rectangle source = new Rectangle(0, 0, RoomBackground.Width, RoomBackground.Height);
                Rectangle dest = new Rectangle(0, 0, RoomBackground.Width * scale, RoomBackground.Height * scale);
                // Draw white (un-tinted)
                Raylib.DrawTexturePro(RoomBackground, source, dest, Vector2.Zero, 0f, Color.White);
            }
            else
            {
                // Fallback: Draw Rectangle? Or just nothing (let tiles draw).
                // Let's draw a black rect to cover void if needed, but Engine clears screen.
            }
        }

        public static void DrawRoom()
        {
            // If we have a background image, do we SKIP drawing the tiles?
            // "use this ... as the backround for now."
            // The user might still want collision/grid logic.
            // If I skip drawing the floor, I should still draw the "Rug" if it's an object?
            // Currently Rug is just tile indices. 
            // If I stop drawing tiles, I see nothing but the BG.
            // Let's assume the BG replaces the floor tiles.
            // For now, let's ONLY draw tiles if BG is missing, OR maybe draw tiles with transparency?
            // Let's assume BG replaces visuals.
            // Let's assume BG replaces visuals.
            if (RoomBackground.Id != 0)
            {
                // Debug Room Special Drawing
                if (CurrentSceneIndex == 0)
                {
                    int debugScale = 4;
                    int debugTileSize = TileSystem.TileSize * debugScale;
                    // Draw Red Exit Tile at (5, 5)
                    Raylib.DrawRectangle(5 * debugTileSize, 5 * debugTileSize, debugTileSize, debugTileSize, new Color(255, 0, 0, 150));
                }
                return;
            }

            int scale = 4;
            int scaledTileSize = TileSystem.TileSize * scale;

            int tilesPerRow = MasterTileset.Width / TileSize;

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int tileIndex = RoomGrid[x, y];

                    int tx = (tileIndex % tilesPerRow) * TileSize;
                    int ty = (tileIndex / tilesPerRow) * TileSize;

                    Rectangle source = new Rectangle(tx, ty, TileSize, TileSize);
                    Rectangle dest = new Rectangle(x * scaledTileSize, y * scaledTileSize, scaledTileSize, scaledTileSize);

                    Raylib.DrawTexturePro(MasterTileset, source, dest, Vector2.Zero, 0f, Color.White);
                }
            }
        }


        public static void DrawForeground()
        {
            if (!HasForeground) return;

            int scale = 4;
            Rectangle source = new Rectangle(0, 0, ForegroundTexture.Width, ForegroundTexture.Height);
            Rectangle dest = new Rectangle(0, 0, ForegroundTexture.Width * scale, ForegroundTexture.Height * scale);
            Raylib.DrawTexturePro(ForegroundTexture, source, dest, Vector2.Zero, 0f, Color.White);
        }


        public static void DrawGrid()
        {
            if (!ShowGrid) return;

            int scale = 4;
            int scaledTileSize = TileSystem.TileSize * scale;

            Color gridColor = new Color(0, 255, 0, 100); // Semi-transparent Green

            // Vertical Lines
            for (int i = 0; i <= GridWidth; i++)
            {
                Raylib.DrawLine(i * scaledTileSize, 0, i * scaledTileSize, GridHeight * scaledTileSize, gridColor);
            }

            // Horizontal Lines
            for (int j = 0; j <= GridHeight; j++)
            {
                Raylib.DrawLine(0, j * scaledTileSize, GridWidth * scaledTileSize, j * scaledTileSize, gridColor);
            }
        }
    }
}

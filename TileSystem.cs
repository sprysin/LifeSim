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
        // Interaction Logic moved to InteractionLayerLogic

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
                Image img = Raylib.GenImageChecked(64, 64, 8, 8, Color.White, Color.DarkGray);
                Raylib.ExportImage(img, tilesetPath);
                Raylib.UnloadImage(img);
            }

            MasterTileset = Raylib.LoadTexture(tilesetPath);

            // Define Scenes
            // Debug Room
            SceneData debugRoom = new SceneData("Debug Room", "Tilesets/output.png", "", new Vector2(2, 2), 1); // Spawn (2,2)
            debugRoom.NPCs.Add(new NPCSpawnData(1, 3, "Testern", "Hello! I am Testern. Welcome to the Debug Room!", Path.Combine("NPC_Data", "Visual Novel Images", "MC", "main_char_full_body.png"))); // Testern (1,3)

            // Kitchen
            SceneData kitchen = new SceneData("Kitchen", "Tilesets/kitchen.png", "Tilesets/interaction Layer_Kitchen.png", new Vector2(4, 5), 1);
            // Add Boogie
            kitchen.NPCs.Add(new NPCSpawnData(9, 4, "Boogie", "[No Response].", Path.Combine("NPC_Data", "Visual Novel Images", "MC", "main_char_full_body.png"), Path.Combine("NPC_Data", "Character_Sheets", "boogie_sprite_sheet.png"), 1.0f));

            // Living Room
            SceneData livingRoom = new SceneData("Living Room", "Tilesets/Living_room.png", "Tilesets/interaction Layer_Living_room.png", new Vector2(4, 5), 1);
            livingRoom.NPCs.Add(new NPCSpawnData(9, 4, "Boogie", "[No Response].", Path.Combine("NPC_Data", "Visual Novel Images", "MC", "main_char_full_body.png"), Path.Combine("NPC_Data", "Character_Sheets", "boogie_sprite_sheet.png"), 1.0f));

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

            // Unload Interaction Layer Logic
            InteractionLayerLogic.Unload();

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

            // Load Interaction Layer & Foreground
            if (!string.IsNullOrEmpty(data.InteractionPath) && System.IO.File.Exists(data.InteractionPath))
            {
                InteractionLayerLogic.Load(data.InteractionPath, GridWidth, GridHeight);

                // Get Foreground Texture from Logic
                Texture2D fg = InteractionLayerLogic.GenerateForegroundTexture(data.BgPath);
                if (fg.Id != 0)
                {
                    ForegroundTexture = fg;
                    HasForeground = true;
                }
                else
                {
                    HasForeground = false;
                }
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
            // Grid Bounds Check
            if (gridX < 0 || gridX >= GridWidth || gridY < 0 || gridY >= GridHeight)
                return false;

            return InteractionLayerLogic.IsWalkable(gridX, gridY, CurrentSceneIndex);
        }

        public static bool IsTerminal(int gridX, int gridY)
        {
            return InteractionLayerLogic.IsTerminal(gridX, gridY);
        }

        public static bool IsTV(int gridX, int gridY)
        {
            return InteractionLayerLogic.IsTV(gridX, gridY);
        }

        public static bool IsDiary(int gridX, int gridY)
        {
            return InteractionLayerLogic.IsDiary(gridX, gridY);
        }

        public static bool IsExit(int gridX, int gridY)
        {
            return InteractionLayerLogic.IsExit(gridX, gridY, CurrentSceneIndex);
        }

        public static bool IsSitSpot(int gridX, int gridY)
        {
            return InteractionLayerLogic.IsSitSpot(gridX, gridY);
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
                // Fallback: Draw Rectangle

            }
        }

        public static void DrawRoom()
        {
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

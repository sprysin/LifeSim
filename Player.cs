using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public class Player
    {
        public int GridX = 5;
        public int GridY = 5;

        private float moveCooldown = 0f;
        private const float MoveDelay = 0.2f;

        // Animation State
        private enum Direction { Down = 0, Left = 1, Right = 2, Up = 3 }
        private Direction currentDir = Direction.Down;
        private bool isMoving = false;

        // Sprite Sheet Details
        public Texture2D SpriteSheet { get { return spriteSheet; } }
        private Texture2D spriteSheet;
        private int frameWidth;
        private int frameHeight;

        // Gaps
        private const int GapX = 1;
        private const int GapY = 8;
        private const int SheetColumns = 16;
        private const int SheetRows = 4;

        // Animation Timing
        private int currentFrame = 0;
        private float frameTimer = 0f;
        private const float FrameSpeed = 0.15f;

        public Player()
        {
            // Load the sprite sheet
            string path = Path.Combine("NPC_Data", "Character_Sheets", "main_character Sprite sheet.png");
            if (System.IO.File.Exists(path))
            {
                spriteSheet = Raylib.LoadTexture(path);

                // Calculate Frame Size accounting for gaps
                frameWidth = (spriteSheet.Width - (SheetColumns - 1) * GapX) / SheetColumns;
                frameHeight = (spriteSheet.Height - (SheetRows - 1) * GapY) / SheetRows;
            }
            else
            {
                Image img = Raylib.GenImageChecked(32, 32, 8, 8, Color.Blue, Color.SkyBlue);
                spriteSheet = Raylib.LoadTextureFromImage(img);
                frameWidth = 8;
                frameHeight = 8;
                Raylib.UnloadImage(img);
            }
        }

        public void Update(List<NPC> npcs)
        {
            float dt = Raylib.GetFrameTime();
            moveCooldown -= dt;

            int dx = 0;
            int dy = 0;

            // Input Handling
            if (Raylib.IsKeyDown(KeyboardKey.Up)) { dy = -1; currentDir = Direction.Up; }
            else if (Raylib.IsKeyDown(KeyboardKey.Down)) { dy = 1; currentDir = Direction.Down; }
            else if (Raylib.IsKeyDown(KeyboardKey.Left)) { dx = -1; currentDir = Direction.Left; }
            else if (Raylib.IsKeyDown(KeyboardKey.Right)) { dx = 1; currentDir = Direction.Right; }

            // Movement Logic
            isMoving = (dx != 0 || dy != 0);

            if (isMoving && moveCooldown <= 0)
            {
                int newX = GridX + dx;
                int newY = GridY + dy;

                // Bounds and Walkability Check
                // Sprite is 1x2 tiles (16x32). We want to check collision only for the bottom tile (feet).
                // GridX/GridY now represents the Feet position.
                if (TileSystem.IsWalkable(newX, newY))
                {
                    // NPC Collision check (Skip if in Debug Room)
                    bool npcAtTarget = false;
                    if (TileSystem.CurrentSceneIndex != 0) // Only check NPC collision if not in debug room (scene 0)
                    {
                        foreach (var npc in npcs)
                        {
                            if (npc.GridX == newX && npc.GridY == newY)
                            {
                                npcAtTarget = true;
                                break;
                            }
                        }
                    }

                    if (!npcAtTarget)
                    {
                        GridX = newX;
                        GridY = newY;
                        moveCooldown = MoveDelay;
                    }
                }
            }

            // Animation Logic
            if (isMoving)
            {
                frameTimer += dt;
                if (frameTimer >= FrameSpeed)
                {
                    currentFrame++;
                    if (currentFrame >= 4) currentFrame = 0;
                    frameTimer = 0;
                }
            }
            else
            {
                currentFrame = 0;
                frameTimer = 0;
            }
        }

        // Returns true if interaction handled (e.g. terminal opened)
        // Returns the NPC if facing one, null otherwise.
        public NPC? CheckInteraction(List<NPC> npcs)
        {
            if (!Raylib.IsKeyPressed(KeyboardKey.X)) return null;

            int faceX = GridX;
            int faceY = GridY;

            switch (currentDir)
            {
                case Direction.Up: faceY--; break;
                case Direction.Down: faceY++; break;
                case Direction.Left: faceX--; break;
                case Direction.Right: faceX++; break;
            }

            // Check Terminal
            if (TileSystem.IsTerminal(faceX, faceY))
            {
                TerminalSystem.Open(npcs);
                return null; // Interaction handled by terminal
            }

            // Check TV
            if (TileSystem.IsTV(faceX, faceY))
            {
                TVSystem.Open();
                return null;
            }

            // Check NPCs
            foreach (var npc in npcs)
            {
                if (npc.GridX == faceX && npc.GridY == faceY)
                {
                    return npc;
                }
            }

            return null;
        }

        public bool IsFacingTerminal()
        {
            int faceX = GridX;
            int faceY = GridY;

            switch (currentDir)
            {
                case Direction.Up: faceY--; break;
                case Direction.Down: faceY++; break;
                case Direction.Left: faceX--; break;
                case Direction.Right: faceX++; break;
            }
            return TileSystem.IsTerminal(faceX, faceY);
        }

        public bool IsFacingTV()
        {
            int faceX = GridX;
            int faceY = GridY;

            switch (currentDir)
            {
                case Direction.Up: faceY--; break;
                case Direction.Down: faceY++; break;
                case Direction.Left: faceX--; break;
                case Direction.Right: faceX++; break;
            }
            return TileSystem.IsTV(faceX, faceY);
        }

        public void Draw()
        {
            int scale = 4;
            float scaledTileSize = TileSystem.TileSize * scale;

            // Draw Player Animation
            int srcX = currentFrame * (frameWidth + GapX);
            int srcY = (int)currentDir * (frameHeight + GapY);
            Rectangle source = new Rectangle(srcX, srcY, frameWidth, frameHeight);

            // DOUBLE SIZE Logic: Reverted to 2x as requested
            // 16x32 sprite scaled 2x -> 32x64 pixels.
            // TileSize 16 scaled 4x -> 64 pixels.
            // Sprite will appear hald-width relative to tile (if tile is 64).
            float destW = frameWidth * (scale / 2.0f);
            float destH = frameHeight * (scale / 2.0f);

            // Draw at grid position (Bottom-Anchored)
            // GridX/GridY is FEET. Anchor bottom of sprite to bottom of GridY tile.
            Rectangle dest = new Rectangle(GridX * scaledTileSize, (GridY + 1) * scaledTileSize - destH, destW, destH);

            Raylib.DrawTexturePro(spriteSheet, source, dest, Vector2.Zero, 0f, Color.White);
        }
        public (int x, int y) GetBehindPosition()
        {
            int backX = GridX;
            int backY = GridY;

            switch (currentDir)
            {
                case Direction.Up: backY++; break;    // Facing Up -> Back is Down
                case Direction.Down: backY--; break;  // Facing Down -> Back is Up
                case Direction.Left: backX++; break;  // Facing Left -> Back is Right
                case Direction.Right: backX--; break; // Facing Right -> Back is Left
            }
            return (backX, backY);
        }
    }
}

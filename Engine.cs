using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;

namespace LifeSim
{
    public class Engine
    {
        public enum GameState
        {
            Menu,
            DebugLocationMenu,
            Exploring,
            Dialogue,
            Terminal,
            TV
        }

        public static GameState CurrentState = GameState.Menu;
        private List<NPC> npcs = new List<NPC>();
        private int menuSelection = 0;
        private int debugLocationSelection = 0;

        // Transition State
        private float fadeAlpha = 0f;
        private bool isExiting = false;

        public void Run()
        {
            Raylib.InitWindow(1280, 720, "LifeSim - Visual Novel Edition");
            Raylib.SetTargetFPS(60);
            Raylib.SetExitKey(KeyboardKey.Null); // Disable default ESC to quit behavior

            // Initialize Systems
            TileSystem.Initialize();
            UISystem.Initialize();
            GeminiService.Initialize();
            Player player = new Player();

            // Setup Camera
            Camera2D camera = new Camera2D();
            camera.Zoom = 1.0f;

            while (!Raylib.WindowShouldClose())
            {
                // Update Logic
                if (CurrentState == GameState.Menu)
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Down)) menuSelection = (menuSelection + 1) % 3;
                    if (Raylib.IsKeyPressed(KeyboardKey.Up)) menuSelection = (menuSelection + 2) % 3;

                    if (Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.X))
                    {
                        if (menuSelection == 0) // Start (Living Room)
                        {
                            TileSystem.LoadScene(2, player, npcs); // Index 2 is Living Room
                            CurrentState = GameState.Exploring;
                            isExiting = false;
                            fadeAlpha = 0f;
                        }
                        else if (menuSelection == 1) // Debug Room -> Show Location Menu
                        {
                            CurrentState = GameState.DebugLocationMenu;
                            debugLocationSelection = 0;
                        }
                        else if (menuSelection == 2) // Quit
                        {
                            break;
                        }
                    }
                }
                else if (CurrentState == GameState.DebugLocationMenu)
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Down)) debugLocationSelection = (debugLocationSelection + 1) % 2;
                    if (Raylib.IsKeyPressed(KeyboardKey.Up)) debugLocationSelection = (debugLocationSelection + 1) % 2;

                    if (Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.X))
                    {
                        if (debugLocationSelection == 0) // Debug Room
                        {
                            TileSystem.LoadScene(0, player, npcs); // Index 0 is Debug Room
                        }
                        else // Kitchen
                        {
                            TileSystem.LoadScene(1, player, npcs); // Index 1 is Kitchen
                        }
                        CurrentState = GameState.Exploring;
                        isExiting = false;
                        fadeAlpha = 0f;
                    }

                    // Back to main menu with Escape
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        CurrentState = GameState.Menu;
                    }
                }
                else if (CurrentState == GameState.Exploring)
                {
                    if (!isExiting)
                    {
                        player.Update(npcs);

                        // Update NPCs
                        foreach (var npc in npcs)
                        {
                            npc.Update(Raylib.GetFrameTime(), player);
                        }

                        // Check for Room Exit
                        if (TileSystem.IsExit(player.GridX, player.GridY))
                        {
                            isExiting = true;
                        }

                        // Check Interaction
                        NPC? target = player.CheckInteraction(npcs);

                        if (target != null)
                        {
                            CurrentState = GameState.Dialogue;
                            target.UpdateDialogue(); // Refresh random phrase
                            UISystem.OpenDialogue(target.Name, target.DialogueText, target.GetCurrentPortraitPath(), target);
                        }
                        else if (TerminalSystem.IsOpen)
                        {
                            CurrentState = GameState.Terminal;
                        }
                        else if (TVSystem.IsOpen)
                        {
                            CurrentState = GameState.TV;
                        }
                        else if (Raylib.IsKeyPressed(KeyboardKey.V))
                        {
                            // Resume Dialogue with Follower
                            if (NPC.ActiveFollower != null)
                            {
                                // Stop following when resuming dialogue
                                var follower = NPC.ActiveFollower;
                                follower.SetFollow(false);

                                CurrentState = GameState.Dialogue;
                                follower.UpdateDialogue();
                                UISystem.OpenDialogue(follower.Name, follower.DialogueText, follower.GetCurrentPortraitPath(), follower);
                            }
                        }
                    }
                }
                else if (CurrentState == GameState.Dialogue)
                {
                    UISystem.Update();
                    UISystem.HandleInput();

                    if (!UISystem.IsOpen)
                    {
                        CurrentState = GameState.Exploring;
                    }
                }
                else if (CurrentState == GameState.Terminal)
                {
                    TerminalSystem.Update();
                    if (!TerminalSystem.IsOpen)
                    {
                        CurrentState = GameState.Exploring;
                    }
                }
                else if (CurrentState == GameState.TV)
                {
                    TVSystem.Update();
                    if (!TVSystem.IsOpen)
                    {
                        CurrentState = GameState.Exploring;
                    }
                }

                // Update Transition
                if (isExiting)
                {
                    fadeAlpha += Raylib.GetFrameTime() * 1.5f; // Fade over ~0.66 seconds
                    if (fadeAlpha >= 1.2f) // Overshoot slightly for pause at black
                    {
                        isExiting = false;
                        fadeAlpha = 0f;
                        CurrentState = GameState.Menu;
                    }
                }

                // Update Camera Center
                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                int scale = 4;
                float scaledTileSize = TileSystem.TileSize * scale;
                Vector2 playerPos = new Vector2(
                    player.GridX * scaledTileSize + (scaledTileSize / 2),
                    player.GridY * scaledTileSize + (scaledTileSize / 2)
                );

                camera.Offset = new Vector2(screenW / 2, screenH / 2);
                camera.Target = playerPos;

                // Draw
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                if (CurrentState == GameState.Menu)
                {
                    UISystem.DrawHomeMenu(menuSelection);
                }
                else if (CurrentState == GameState.DebugLocationMenu)
                {
                    UISystem.DrawDebugLocationMenu(debugLocationSelection);
                }
                else
                {
                    // Begin World Space
                    Raylib.BeginMode2D(camera);

                    // Draw Room/BG
                    TileSystem.DrawBackground();
                    TileSystem.DrawRoom();

                    // Draw NPCs
                    foreach (var npc in npcs)
                    {
                        npc.Draw(player.SpriteSheet);
                    }

                    // Draw Player
                    player.Draw();

                    // Draw Foreground Layer (on top of player/NPCs)
                    TileSystem.DrawForeground();

                    if (CurrentState == GameState.Exploring && !isExiting)
                    {
                        foreach (var npc in npcs)
                        {
                            if (npc.IsPlayerNearby(player))
                            {
                                UISystem.DrawPrompt(npc);
                            }
                        }

                        if (player.IsFacingTerminal())
                        {
                            // Prompt location: 1 tile UP from player (GridY - 1)
                            UISystem.DrawPrompt(player.GridX, player.GridY - 1);
                        }

                        if (player.IsFacingTV())
                        {
                            // Prompt location: 1 tile UP from player (GridY - 1)
                            UISystem.DrawPrompt(player.GridX, player.GridY - 1);
                        }
                    }

                    // Debug Grid (World Space)
                    TileSystem.DrawGrid();

                    Raylib.EndMode2D();

                    // Draw Debug Info (Top Left)
                    // Note: Debug Info usually screen space, so we should actually suspend mode2d or draw it later?
                    // UISystem.DrawDebugInfo uses DrawText which is screen space usually?
                    // Actually Raylib.DrawText is affected by Camera if inside Mode2D? Yes.
                    // Debug info "Tile: X, Y" at 10,10 should be Screen Space.
                    // So we DO need to EndMode2D for DebugInfo, but Re-Begin for Prompts?
                    // Draw Debug Info (Top Left)
                    UISystem.DrawDebugInfo(player.GridX, player.GridY);

                    // Mouse Tooltip (Grid Coordinates)
                    if (TileSystem.ShowGrid)
                    {
                        Vector2 mousePos = Raylib.GetMousePosition();
                        Vector2 worldPos = Raylib.GetScreenToWorld2D(mousePos, camera);
                        // Accessing outer scope debug variables if available, or just use literal 4
                        int tooltipGridX = (int)(worldPos.X / scaledTileSize);
                        int tooltipGridY = (int)(worldPos.Y / scaledTileSize);

                        // Only show if within bounds
                        if (tooltipGridX >= 0 && tooltipGridX < TileSystem.GridWidth && tooltipGridY >= 0 && tooltipGridY < TileSystem.GridHeight)
                        {
                            string tooltip = $"({tooltipGridX}, {tooltipGridY})";
                            Raylib.DrawText(tooltip, (int)mousePos.X + 10, (int)mousePos.Y - 20, 20, Color.Yellow);
                        }
                    }

                    // Draw UI / Dialogue (Screen Space)
                    if (CurrentState == GameState.Dialogue)
                    {
                        UISystem.DrawDialogue();
                    }
                    else if (CurrentState == GameState.Terminal)
                    {
                        TerminalSystem.Draw();
                    }
                    else if (CurrentState == GameState.TV)
                    {
                        TVSystem.Draw();
                    }
                }

                // Global Overlays (Fade)
                if (isExiting)
                {
                    UISystem.DrawFade(fadeAlpha);
                }

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}

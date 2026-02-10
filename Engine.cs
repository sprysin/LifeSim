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
            Scene,
            DebugLocationMenu,
            Exploring, // Keeping for compilation safety until full cleanup, but effectively deprecated
            Dialogue,
            Terminal,
            TV,
            Minigame20Questions,
            Diary
        }

        public static GameState CurrentState = GameState.Menu;
        public static List<NPC> ActiveNPCs = new List<NPC>();
        private int menuSelection = 0;
        private int debugLocationSelection = 0;

        // Transition State
        private float fadeAlpha = 0f;
        private bool isExiting = false;

        public void Run()
        {
            Raylib.InitWindow(1600, 900, "LifeSim - Static");
            Raylib.SetTargetFPS(60);
            Raylib.SetExitKey(KeyboardKey.Null);

            // Initialize Systems
            TileSystem.Initialize();
            UISystem.Initialize();
            GeminiService.Initialize();
            InputSystem.Initialize(); // New System

            while (!Raylib.WindowShouldClose())
            {
                // Update Logic
                if (CurrentState == GameState.Menu)
                {
                    // Keyboard Navigation
                    if (Raylib.IsKeyPressed(KeyboardKey.Down)) menuSelection = (menuSelection + 1) % 3;
                    if (Raylib.IsKeyPressed(KeyboardKey.Up)) menuSelection = (menuSelection + 2) % 3;

                    // Keyboard Confirmation
                    if (Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.X))
                    {
                        if (menuSelection == 0) // Start (Living Room)
                        {
                            TileSystem.LoadScene(2, ActiveNPCs); // Living Room

                            // Auto-Start Dialogue with Boogie
                            NPC? boogie = ActiveNPCs.Find(n => n.Name == "Boogie");
                            if (boogie != null)
                            {
                                boogie.UpdateDialogue(); // Ensure initial text is populated
                                UISystem.OpenDialogue(boogie.Name, boogie.DialogueText, boogie.GetCurrentPortraitPath(), boogie);
                                CurrentState = GameState.Dialogue;
                            }
                            else
                            {
                                CurrentState = GameState.Scene;
                            }

                            isExiting = false;
                            fadeAlpha = 0f;
                        }
                        else if (menuSelection == 1) // Debug Room
                        {
                            TileSystem.LoadScene(0, ActiveNPCs); // Debug Room
                            CurrentState = GameState.Scene;
                        }
                        else if (menuSelection == 2) // Quit
                        {
                            break;
                        }
                    }
                }
                else if (CurrentState == GameState.Scene)
                {
                    if (!isExiting)
                    {
                        // Debug Reset (R key)
                        if (Raylib.IsKeyPressed(KeyboardKey.R))
                        {
                            NPC? boogie = ActiveNPCs.Find(n => n.Name == "Boogie");
                            if (boogie != null)
                            {
                                boogie.UpdateDialogue();
                                UISystem.OpenDialogue(boogie.Name, boogie.DialogueText, boogie.GetCurrentPortraitPath(), boogie);
                                CurrentState = GameState.Dialogue;
                            }
                        }

                        // Update Input
                        InputSystem.Update();
                    }

                    // Update NPCs
                    foreach (var npc in ActiveNPCs)
                    {
                        npc.Update(Raylib.GetFrameTime());
                    }

                    // Check Interactions from InputSystem (Button Clicks handled there or state changes)
                    // For now, InputSystem will set GameStates directly or we query it.
                    // Let's rely on InputSystem to trigger changes or UISystem to show active buttons.

                    // Handle standard overrides
                    if (TerminalSystem.IsOpen) CurrentState = GameState.Terminal;
                    else if (TVSystem.IsOpen) CurrentState = GameState.TV;
                    else if (DiarySystem.IsOpen) CurrentState = GameState.Diary;
                }
                else if (CurrentState == GameState.Dialogue)
                {
                    // Update Input (for global buttons like Exit/Terminal)
                    InputSystem.Update();

                    UISystem.Update();
                    UISystem.HandleInput();

                    if (!UISystem.IsOpen)
                    {
                        CurrentState = GameState.Scene;
                    }
                }
                else if (CurrentState == GameState.Terminal)
                {
                    TerminalSystem.Update();
                    if (!TerminalSystem.IsOpen)
                    {
                        CurrentState = UISystem.IsOpen ? GameState.Dialogue : GameState.Scene;
                    }
                }
                else if (CurrentState == GameState.TV)
                {
                    TVSystem.Update();
                    if (!TVSystem.IsOpen)
                    {
                        // Check if a minigame was started from the TV menu
                        if (CurrentState != GameState.Minigame20Questions)
                        {
                            CurrentState = UISystem.IsOpen ? GameState.Dialogue : GameState.Scene;
                        }
                    }
                }
                else if (CurrentState == GameState.Diary)
                {
                    DiarySystem.Update();
                    if (!DiarySystem.IsOpen)
                    {
                        CurrentState = UISystem.IsOpen ? GameState.Dialogue : GameState.Scene;
                    }
                }
                else if (CurrentState == GameState.Minigame20Questions)
                {
                    MinigameManager.Update();
                    if (MinigameManager.ActiveMinigame != null)
                    {
                        TwentyQuestionsUI.HandleInput(MinigameManager.ActiveMinigame);
                    }
                    if (!MinigameManager.IsActive)
                    {
                        CurrentState = UISystem.IsOpen ? GameState.Dialogue : GameState.Scene;
                    }
                }

                // Update Transition
                if (isExiting)
                {
                    fadeAlpha += Raylib.GetFrameTime() * 1.5f;
                    if (fadeAlpha >= 1.2f)
                    {
                        isExiting = false;
                        fadeAlpha = 0f;
                        CurrentState = GameState.Menu;
                    }
                }

                // Draw
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                if (CurrentState == GameState.Menu)
                {
                    // Menu Mouse Logic
                    (int mouseSel, bool clicked) = UISystem.DrawHomeMenu(menuSelection);
                    if (mouseSel != menuSelection) menuSelection = mouseSel;

                    if (clicked) // Mouse Click Handling
                    {
                        if (mouseSel == 0) // Start
                        {
                            TileSystem.LoadScene(2, ActiveNPCs); // Living Room

                            // Auto-Start Dialogue with Boogie
                            NPC? boogie = ActiveNPCs.Find(n => n.Name == "Boogie");
                            if (boogie != null)
                            {
                                boogie.UpdateDialogue();
                                UISystem.OpenDialogue(boogie.Name, boogie.DialogueText, boogie.GetCurrentPortraitPath(), boogie);
                                CurrentState = GameState.Dialogue;
                            }
                            else
                            {
                                CurrentState = GameState.Scene;
                            }

                            isExiting = false;
                            fadeAlpha = 0f;
                        }
                        else if (mouseSel == 1) // Debug Room
                        {
                            TileSystem.LoadScene(0, ActiveNPCs);
                            CurrentState = GameState.Scene;
                        }
                        else if (mouseSel == 2) // Quit
                        {
                            break; // Break the game loop
                        }
                    }
                }
                else
                {
                    // Draw Static Scene (Background + NPC)
                    TileSystem.DrawStaticScene(ActiveNPCs);

                    // Draw UI Layers
                    if (CurrentState == GameState.Scene || CurrentState == GameState.Dialogue)
                    {
                        // Draw Permanent UI (Bottom Box, Action Buttons)
                        UISystem.DrawStaticUI(CurrentState == GameState.Dialogue);

                        // Draw Dialogue Text Overlay
                        if (CurrentState == GameState.Dialogue)
                        {
                            UISystem.DrawDialogue();
                        }
                    }

                    // Draw Overlays
                    if (CurrentState == GameState.Terminal) TerminalSystem.Draw();
                    else if (CurrentState == GameState.TV) TVSystem.Draw();
                    else if (CurrentState == GameState.Diary) DiarySystem.Draw();
                    else if (CurrentState == GameState.Minigame20Questions) MinigameManager.Draw();
                }

                // Global Fade
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

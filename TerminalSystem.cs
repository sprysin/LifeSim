using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace LifeSim
{
    public static class TerminalSystem
    {
        // Terminal State
        public static bool IsOpen = false;
        private static List<NPC>? terminalNPCs;
        private static int terminalSelection = 0;
        private static bool isEditingRule = false;
        private static int editingSelection = 0; // 0: Rule, 1: Mood
        private static bool summonRuleActive = false; // "Summon to 8,3"
        public static bool DebugShowMood = false; // New Toggle

        public static void Open(List<NPC> npcs)
        {
            IsOpen = true;
            terminalNPCs = npcs;
            terminalSelection = 0;
            isEditingRule = false;
            editingSelection = 0;
        }

        public static void Close()
        {
            IsOpen = false;
        }

        public static void Initialize()
        {
            DialogueManager.Initialize();
        }

        public static void Update()
        {
            if (!IsOpen) return;

            if (isEditingRule)
            {
                if (terminalNPCs == null)
                {
                    isEditingRule = false;
                    return;
                }

                var selectedNPC = terminalNPCs[terminalSelection];

                // Sub-menu Navigation
                if (Raylib.IsKeyPressed(KeyboardKey.Down)) editingSelection = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Up)) editingSelection = 0;

                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (editingSelection == 0)
                    {
                        // Toggle Summon Rule
                        summonRuleActive = !summonRuleActive;

                        // Apply Logic Immediately for now
                        if (summonRuleActive)
                        {
                            selectedNPC.GridX = 8;
                            selectedNPC.GridY = 3;
                        }
                    }
                    else if (editingSelection == 1)
                    {
                        // Cycle Mood
                        List<string> moods = DialogueManager.GetAvailableMoods(selectedNPC.Name);
                        if (moods.Count > 0)
                        {
                            int currentIdx = moods.IndexOf(selectedNPC.CurrentMood);
                            int nextIdx = (currentIdx + 1) % moods.Count;
                            selectedNPC.CurrentMood = moods[nextIdx];
                            selectedNPC.UpdateDialogue();
                        }
                    }
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Z))
                {
                    isEditingRule = false;
                }
            }
            else
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                {
                    terminalSelection++;
                    if (terminalNPCs != null && terminalSelection >= terminalNPCs.Count + 3) terminalSelection = 0; // +3 for Grid, Mood, Exit
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    terminalSelection--;
                    if (terminalSelection < 0 && terminalNPCs != null) terminalSelection = terminalNPCs.Count + 2; // Count+2 is Exit
                }

                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (terminalNPCs != null)
                    {
                        if (terminalSelection == terminalNPCs.Count)
                        {
                            // Toggle Grid
                            TileSystem.ShowGrid = !TileSystem.ShowGrid;
                        }
                        else if (terminalSelection == terminalNPCs.Count + 1)
                        {
                            // Toggle Mood HUD
                            DebugShowMood = !DebugShowMood;
                        }
                        else if (terminalSelection == terminalNPCs.Count + 2)
                        {
                            // Exit Button Selected
                            Close();
                        }
                        else if (terminalSelection < terminalNPCs.Count)
                        {
                            isEditingRule = true;
                        }
                    }
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Z))
                {
                    Close();
                }
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // 1. Darken Background
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 150));

            // 2. Render Terminal UI to Buffer (Low Res)
            Raylib.BeginTextureMode(UISystem.UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            int boxX = 20;
            int boxY = 20;
            int boxW = UISystem.VirtualWidth - 40;
            int boxH = UISystem.VirtualHeight - 40;
            Rectangle boxRect = new(boxX, boxY, boxW, boxH);

            // Terminal Background (Blueish Data feel)
            Raylib.DrawRectangleRec(boxRect, new Color(0, 20, 40, 240));
            Raylib.DrawRectangleLinesEx(boxRect, 1, new Color(100, 200, 255, 255));

            // Header
            Raylib.DrawTextEx(UISystem.FontSmall, "TERMINAL - NPC CONTROL", new Vector2(boxX + 10, boxY + 10), 12, 0, new Color(100, 255, 255, 255));
            Raylib.DrawLine(boxX + 10, boxY + 24, boxX + boxW - 10, boxY + 24, new Color(100, 200, 255, 100));

            if (terminalNPCs == null || terminalNPCs.Count == 0)
            {
                Raylib.DrawTextEx(UISystem.FontSmall, "No NPCs found.", new Vector2(boxX + 10, boxY + 30), 12, 0, Color.White);
            }
            else
            {
                int listStartY = boxY + 30;

                // Left Panel: NPC List
                for (int i = 0; i < terminalNPCs.Count; i++)
                {
                    string name = terminalNPCs[i].Name;
                    Color color = (i == terminalSelection && !isEditingRule) ? Color.Yellow : Color.White;
                    if (isEditingRule && i == terminalSelection) color = Color.Gray;

                    Raylib.DrawTextEx(UISystem.FontSmall, name, new Vector2(boxX + 10, listStartY + (i * 15)), 12, 0, color);

                    if (i == terminalSelection && !isEditingRule)
                    {
                        Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(boxX + 2, listStartY + (i * 15)), 12, 0, Color.Yellow);
                    }
                }



                // Toggle Grid Button
                int gridIndex = terminalNPCs.Count;
                int gridY = listStartY + (gridIndex * 15) + 5;
                Color gridColor = (gridIndex == terminalSelection) ? Color.Green : Color.Gray;
                string gridText = "[Toggle Grid]";
                if (TileSystem.ShowGrid) gridText = "[Hide Grid]";

                Raylib.DrawTextEx(UISystem.FontSmall, gridText, new Vector2(boxX + 10, gridY), 12, 0, gridColor);
                if (gridIndex == terminalSelection)
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(boxX + 2, gridY), 12, 0, Color.Green);
                }

                // Toggle Mood HUD Button
                int moodIndex = terminalNPCs.Count + 1;
                int menuMoodY = listStartY + (moodIndex * 15) + 5;
                Color moodColor = (moodIndex == terminalSelection) ? Color.Yellow : Color.Gray;
                string moodText = "[Show Mood]";
                if (DebugShowMood) moodText = "[Hide Mood]";

                Raylib.DrawTextEx(UISystem.FontSmall, moodText, new Vector2(boxX + 10, menuMoodY), 12, 0, moodColor);
                if (moodIndex == terminalSelection)
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(boxX + 2, menuMoodY), 12, 0, Color.Yellow);
                }

                // EXIT Button
                int exitIndex = terminalNPCs.Count + 2;
                int exitY = listStartY + (exitIndex * 15) + 10; // Extra spacing
                Color exitColor = (exitIndex == terminalSelection) ? Color.Red : Color.Gray;
                Raylib.DrawTextEx(UISystem.FontSmall, "[EXIT]", new Vector2(boxX + 10, exitY), 12, 0, exitColor);
                if (exitIndex == terminalSelection)
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(boxX + 2, exitY), 12, 0, Color.Red);
                }

                // Right Panel: Details / Rules
                int splitX = boxX + 100;
                Raylib.DrawLine(splitX, boxY + 24, splitX, boxY + boxH - 10, new Color(100, 200, 255, 50));

                if (isEditingRule)
                {
                    var selectedNPC = terminalNPCs[terminalSelection];

                    Raylib.DrawTextEx(UISystem.FontSmall, "EDITING RULES", new Vector2(splitX + 10, listStartY), 12, 0, Color.Red);

                    // 1. Summon Rule
                    int ruleY = listStartY + 15;
                    Color ruleHeaderColor = (editingSelection == 0) ? Color.Yellow : Color.White;
                    Raylib.DrawTextEx(UISystem.FontSmall, "Summon Rule", new Vector2(splitX + 10, ruleY), 12, 0, ruleHeaderColor);
                    if (editingSelection == 0) Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(splitX + 2, ruleY), 12, 0, Color.Yellow);

                    string status = summonRuleActive ? "Reset Summon" : "off";
                    Color ruleColor = summonRuleActive ? Color.Green : Color.Gray;
                    Raylib.DrawTextEx(UISystem.FontSmall, status, new Vector2(splitX + 10, ruleY + 12), 12, 0, ruleColor);


                    // 2. Mood Setting
                    int moodY = ruleY + 30; // Compact spacing
                    Color moodHeaderColor = (editingSelection == 1) ? Color.Yellow : Color.White;
                    Raylib.DrawTextEx(UISystem.FontSmall, "Mood", new Vector2(splitX + 10, moodY), 12, 0, moodHeaderColor);
                    if (editingSelection == 1) Raylib.DrawTextEx(UISystem.FontSmall, ">", new Vector2(splitX + 2, moodY), 12, 0, Color.Yellow);

                    string moodDisplay = $"[{selectedNPC.CurrentMood}]";
                    List<string> availableMoods = DialogueManager.GetAvailableMoods(selectedNPC.Name);
                    if (availableMoods.Count == 0) moodDisplay = "[ERROR: No Moods]";

                    Raylib.DrawTextEx(UISystem.FontSmall, moodDisplay, new Vector2(splitX + 10, moodY + 12), 12, 0, Color.White);

                    // Instructions
                    Raylib.DrawTextEx(UISystem.FontSmall, "X: Change | Z: Back", new Vector2(splitX + 10, moodY + 40), 12, 0, Color.LightGray);
                }
                else
                {
                    Raylib.DrawTextEx(UISystem.FontSmall, "Select NPC to", new Vector2(splitX + 10, listStartY), 12, 0, Color.Gray);
                    Raylib.DrawTextEx(UISystem.FontSmall, "edit rules.", new Vector2(splitX + 10, listStartY + 15), 12, 0, Color.Gray);
                }
            }

            Raylib.EndTextureMode();

            // Render Buffer
            Rectangle dest = new(0, 0, screenW, screenH);
            Rectangle flipSrc = new(0, 0, UISystem.UIBuffer.Texture.Width, -UISystem.UIBuffer.Texture.Height);
            Raylib.DrawTexturePro(UISystem.UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);
        }
    }
}

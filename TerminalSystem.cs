using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System; // Added for Enum

namespace LifeSim
{
    public static class TerminalSystem
    {
        // Terminal State
        public static bool IsOpen = false;
        private static List<NPC>? terminalNPCs;
        private static int terminalSelection = 0;
        private static bool isEditingRule = false;
        private static int editingSelection = 0; // 0: Mood, 1: Skin
        public static bool DebugShowMood = false; // New Toggle

        // Editing flags for Mouse interaction
        // The original code mixed keyboard state 'isEditingRule' with mouse flags 'isEditingMood/Skin'
        // I will adhere to 'isEditingRule' as the main state, but allow mouse to set specific modes.
        // Actually, previous refactor set 'isEditingMood' etc. but those fields were not defined in lines 1-140 of original file.
        // Let's check Step 748.
        // Lines 9-16: isEditingRule, editingSelection. NO isEditingMood defined.
        // So 'isEditingMood' used in my previous attempts was likely WRONG or intended to be local?
        // Ah, in Step 746 I used `if (isEditingMood)`.
        // I should map `isEditingMood` to `isEditingRule && editingSelection == 1`.
        // I will use properties or helper logic to keep it clean.

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
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                {
                    editingSelection++;
                    if (editingSelection > 1) editingSelection = 0; // 0: Mood, 1: Skin
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    editingSelection--;
                    if (editingSelection < 0) editingSelection = 1;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (editingSelection == 0)
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
                    else if (editingSelection == 1)
                    {
                        // Toggle Skin
                        if (selectedNPC.CurrentSkin == "Skin0") selectedNPC.CurrentSkin = "Skin1";
                        else selectedNPC.CurrentSkin = "Skin0";
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
                    if (terminalNPCs != null && terminalSelection >= terminalNPCs.Count + 3) terminalSelection = 0;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    terminalSelection--;
                    if (terminalSelection < 0 && terminalNPCs != null) terminalSelection = terminalNPCs.Count + 2;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.X))
                {
                    if (terminalNPCs != null)
                    {
                        if (terminalSelection == terminalNPCs.Count)
                        {
                            // Show Mood Debug option removed (was ShowGrid)
                        }
                        else if (terminalSelection == terminalNPCs.Count + 1)
                        {
                            DebugShowMood = !DebugShowMood;
                        }
                        else if (terminalSelection == terminalNPCs.Count + 2)
                        {
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

            // 2. Main Terminal Panel
            int panelW = 800;
            int panelH = 600;
            int panelX = (screenW - panelW) / 2;
            int panelY = (screenH - panelH) / 2;
            Rectangle mainRect = new Rectangle(panelX, panelY, panelW, panelH);
            UISystem.DrawCozyPanel(mainRect, "TERMINAL");

            // 3. Characters Header
            int headerY = panelY + 60;
            Raylib.DrawTextEx(UISystem.FontMedium, "CHARACTERS", new Vector2(panelX + 20, headerY), 24, 1, UISystem.ColorTan);

            // 4. NPC List
            int contentY = headerY + 35;
            int itemHeight = 30;
            int contentX = panelX + 20;
            int contentW = panelW - 40;

            if (terminalNPCs == null || terminalNPCs.Count == 0)
            {
                Raylib.DrawTextEx(UISystem.FontMedium, "No NPCs found.", new Vector2(contentX + 20, contentY), 24, 0, UISystem.ColorCream);
            }
            else
            {
                for (int i = 0; i < terminalNPCs.Count; i++)
                {
                    int y = contentY + (i * itemHeight);
                    if (y > panelY + panelH - 80) break;

                    bool isSelected = (i == terminalSelection && !isEditingRule);
                    Rectangle itemRect = new Rectangle(contentX, y, contentW, itemHeight - 2);

                    // Check for mouse click on NPC name to open edit overlay
                    Vector2 mousePos = Raylib.GetMousePosition();
                    bool isHovered = Raylib.CheckCollisionPointRec(mousePos, itemRect);

                    if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        terminalSelection = i;
                        isEditingRule = true;
                    }

                    // Selection Highlight
                    if (isSelected || isHovered)
                    {
                        Raylib.DrawRectangleRounded(itemRect, 0.2f, 5, isHovered ? UISystem.ColorWarmToffee : UISystem.ColorWarmToffee);
                    }
                    else if (isEditingRule && i == terminalSelection)
                    {
                        Raylib.DrawRectangleRounded(itemRect, 0.2f, 5, new Color(100, 100, 100, 100));
                    }

                    // Name
                    Color nameColor = (isSelected || isHovered) ? UISystem.ColorCharcoal : UISystem.ColorCream;
                    Raylib.DrawTextEx(UISystem.FontMedium, terminalNPCs[i].Name, new Vector2(contentX + 10, y + 5), 20, 1, nameColor);

                    // Status (mood on right)
                    string status = terminalNPCs[i].CurrentMood.ToString();
                    Vector2 statusSize = Raylib.MeasureTextEx(UISystem.FontSmall, status, 16, 1);
                    Color statusColor = (isSelected || isHovered) ? UISystem.ColorCharcoal : UISystem.ColorTan;
                    Raylib.DrawTextEx(UISystem.FontSmall, status, new Vector2(contentX + contentW - statusSize.X - 10, y + 8), 16, 1, statusColor);
                }
            }

            // 5. Buttons (Bottom) - Show Mood and Exit only
            int btnY = panelY + panelH - 45;
            int btnH = 35;
            int btnW = 120;
            int startBtnX = panelX + 20;

            // Mood Button
            string moodText = DebugShowMood ? "Hide Mood" : "Show Mood";
            bool moodSelected = (terminalSelection == (terminalNPCs?.Count ?? 0));
            if (UISystem.DrawCozyButton(new Rectangle(startBtnX, btnY, btnW, btnH), moodText, moodSelected))
            {
                DebugShowMood = !DebugShowMood;
            }

            // Exit Button
            bool exitSelected = (terminalSelection == (terminalNPCs?.Count ?? 0) + 1);
            Rectangle exitRect = new Rectangle(panelX + panelW - btnW - 20, btnY, btnW, btnH);
            if (UISystem.DrawCozyButton(exitRect, "EXIT", exitSelected))
            {
                Close();
            }

            // 5. Edit Overlay
            if (isEditingRule)
            {
                DrawEditOverlay(screenW, screenH);
            }
        }

        private static void DrawEditOverlay(int width, int height)
        {
            // Dim
            Raylib.DrawRectangle(0, 0, width, height, new Color(0, 0, 0, 200));

            // Edit Panel
            int panelW = 400;
            int panelH = 300;
            Rectangle panelRect = new Rectangle((width - panelW) / 2, (height - panelH) / 2, panelW, panelH);

            string title = "EDIT NPC";
            if (terminalNPCs != null && terminalSelection < terminalNPCs.Count)
            {
                title = "EDIT: " + terminalNPCs[terminalSelection].Name;
            }

            UISystem.DrawCozyPanel(panelRect, title);

            if (terminalNPCs == null) return;
            var selectedNPC = terminalNPCs[terminalSelection];

            int contentX = (int)panelRect.X + 30;
            int contentY = (int)panelRect.Y + 60;
            int btnH = 40;

            // 1. Mood
            string moodText = $"Mood: {selectedNPC.CurrentMood}";
            bool moodSelected = (editingSelection == 0);
            Rectangle moodRect = new Rectangle(contentX, contentY, panelW - 60, btnH);
            if (UISystem.DrawCozyButton(moodRect, moodText, moodSelected))
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
                editingSelection = 0;
            }

            // 2. Skin
            string skinText = $"Skin: {selectedNPC.CurrentSkin}";
            bool skinSelected = (editingSelection == 1);
            Rectangle skinRect = new Rectangle(contentX, contentY + (btnH + 20), panelW - 60, btnH);
            if (UISystem.DrawCozyButton(skinRect, skinText, skinSelected))
            {
                if (selectedNPC.CurrentSkin == "Skin0") selectedNPC.CurrentSkin = "Skin1";
                else selectedNPC.CurrentSkin = "Skin0";
                editingSelection = 1;
            }

            // 3. Force Memory (Debug)
            string memText = "Force Memory (Last 2)";
            bool memSelected = (editingSelection == 2);
            Rectangle memRect = new Rectangle(contentX, contentY + (btnH + 20) * 2, panelW - 60, btnH);
            if (UISystem.DrawCozyButton(memRect, memText, memSelected))
            {
                // Fire and forget (async)
                _ = selectedNPC.GenerateDiaryEntry(2);
                editingSelection = 2;
            }

            Raylib.DrawTextEx(UISystem.FontSmall, "Press Z to Return", new Vector2(contentX, contentY + (btnH + 20) * 3 + 10), 16, 1, UISystem.ColorTan);
        }
    }
}

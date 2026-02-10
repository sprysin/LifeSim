using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static class DiarySystem
    {
        public static bool IsOpen { get; private set; } = false;

        private static NPC? currentNPC;
        private static List<DiaryEntry> entries = new List<DiaryEntry>();
        private static int selectedIndex = -1;
        private static Vector2 scrollPosition = Vector2.Zero;

        // UI Layout Constants
        private const int PanelW = 1100;
        private const int PanelH = 700;
        private const int ListPanelW = 300;
        private const int ContentPanelW = PanelW - ListPanelW;

        public static void Initialize()
        {
            // No assets to load yet
        }

        public static void Open()
        {
            // For now, default to Boogie or ActiveFollower
            currentNPC = NPC.ActiveFollower ?? Engine.ActiveNPCs.Find(n => n.Name == "Boogie");

            if (currentNPC != null)
            {
                currentNPC.LoadDiary(); // Ensure fresh data
                entries = currentNPC.DiaryEntries.OrderByDescending(e => e.Created).ToList();
            }
            else
            {
                entries.Clear();
            }

            // Select most recent if available
            selectedIndex = entries.Count > 0 ? 0 : -1;
            scrollPosition = Vector2.Zero;
            IsOpen = true;
        }

        public static void Close()
        {
            IsOpen = false;
        }

        public static void Update()
        {
            if (!IsOpen) return;

            // Simple keyboard navigation for list
            if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                if (entries.Count > 0)
                {
                    selectedIndex++;
                    if (selectedIndex >= entries.Count) selectedIndex = 0;
                }
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                if (entries.Count > 0)
                {
                    selectedIndex--;
                    if (selectedIndex < 0) selectedIndex = entries.Count - 1;
                }
            }

            // Delete Entry
            if (Raylib.IsKeyPressed(KeyboardKey.Delete) && selectedIndex >= 0 && selectedIndex < entries.Count)
            {
                DeleteCurrentEntry();
            }

            // Exit
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Z))
            {
                Close();
            }
        }

        private static void DeleteCurrentEntry()
        {
            if (currentNPC != null && selectedIndex >= 0 && selectedIndex < entries.Count)
            {
                var entryToRemove = entries[selectedIndex];
                currentNPC.DiaryEntries.Remove(entryToRemove);
                currentNPC.SaveDiary();

                // Refresh local list
                entries = currentNPC.DiaryEntries.OrderByDescending(e => e.Created).ToList();

                // Adjust selection
                if (selectedIndex >= entries.Count) selectedIndex = entries.Count - 1;
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // 1. Darken Background
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 180));

            // 2. Main Panel Geometry
            int panelX = (screenW - PanelW) / 2;
            int panelY = (screenH - PanelH) / 2;

            // 3. Draw Panels
            // Left Panel (Reader) - Transparent Grey
            Rectangle contentRect = new Rectangle(panelX, panelY, ContentPanelW, PanelH);
            Raylib.DrawRectangleRec(contentRect, new Color(20, 20, 20, 240));
            Raylib.DrawRectangleLinesEx(contentRect, 1, UISystem.ColorTan);

            // Right Panel (List) - Distinct Style
            Rectangle listRect = new Rectangle(panelX + ContentPanelW, panelY, ListPanelW, PanelH);
            Raylib.DrawRectangleRec(listRect, UISystem.ColorEspresso);
            Raylib.DrawRectangleLinesEx(listRect, 1, UISystem.ColorTan);

            // Header for List
            Raylib.DrawTextEx(UISystem.FontSmall, "MEMORIES", new Vector2(listRect.X + 15, listRect.Y + 15), 20, 1, UISystem.ColorTan);

            // 4. Draw Entry List
            int itemH = 60; // Taller for title
            int startY = (int)listRect.Y + 50;

            // Scissor Mode for List Scrolling could be added here, simplified for now
            for (int i = 0; i < entries.Count; i++)
            {
                int yPos = startY + (i * itemH);
                if (yPos > listRect.Y + listRect.Height - 60) break; // Clip bottom

                Rectangle itemRect = new Rectangle(listRect.X + 5, yPos, listRect.Width - 10, itemH - 2);
                bool isSelected = (i == selectedIndex);
                bool isHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), itemRect);

                if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    selectedIndex = i;
                }

                Color bgColor = isSelected ? UISystem.ColorWarmToffee : (isHovered ? new Color(255, 255, 255, 20) : Color.Blank);
                Color textColor = isSelected ? UISystem.ColorCharcoal : UISystem.ColorCream;

                if (isSelected || isHovered)
                {
                    Raylib.DrawRectangleRounded(itemRect, 0.2f, 4, bgColor);
                }

                // Show Title logic
                string title = entries[i].Summary;
                if (title.Length > 22) title = title.Substring(0, 20) + "..."; // Truncate

                Raylib.DrawTextEx(UISystem.FontSmall, title, new Vector2(itemRect.X + 10, itemRect.Y + 12), 18, 1, textColor);

                // Show Date logic (smaller below)
                string dateStr = entries[i].Created.ToString("MMM dd HH:mm");
                Raylib.DrawTextEx(UISystem.FontTiny, dateStr, new Vector2(itemRect.X + 10, itemRect.Y + 36), 10, 1, isSelected ? UISystem.ColorEspresso : Color.Gray);
            }

            // 5. Draw Content (Reader)
            if (selectedIndex >= 0 && selectedIndex < entries.Count)
            {
                var entry = entries[selectedIndex];

                // Title
                Raylib.DrawTextEx(UISystem.FontMedium, entry.Summary, new Vector2(contentRect.X + 40, contentRect.Y + 40), 32, 1, UISystem.ColorWarmToffee);

                // Date Subtitle
                string fullDate = entry.Created.ToString("dddd, MMMM dd, yyyy h:mm tt");
                Raylib.DrawTextEx(UISystem.FontSmall, fullDate, new Vector2(contentRect.X + 40, contentRect.Y + 80), 20, 1, Color.Gray);

                Raylib.DrawLineEx(new Vector2(contentRect.X + 40, contentRect.Y + 110), new Vector2(contentRect.X + contentRect.Width - 40, contentRect.Y + 110), 1, Color.Gray);

                // Body Content (Wrapped)
                string body = entry.Content;
                float maxWidth = contentRect.Width - 80;
                Vector2 startPos = new Vector2(contentRect.X + 40, contentRect.Y + 130);

                DrawTextWrapped(body, startPos, maxWidth, UISystem.FontSmall, 22, 1, UISystem.ColorCream);
            }
            else
            {
                Raylib.DrawTextEx(UISystem.FontSmall, "No memories found.", new Vector2(contentRect.X + 40, contentRect.Y + 60), 24, 1, Color.Gray);
            }

            // Delete Hint
            if (selectedIndex >= 0)
            {
                Raylib.DrawTextEx(UISystem.FontSmall, "[DEL] Delete Entry", new Vector2(listRect.X + 15, listRect.Y + listRect.Height - 30), 16, 1, Color.Gray);
            }
        }

        private static void DrawTextWrapped(string text, Vector2 pos, float maxWidth, Font font, float fontSize, float spacing, Color color)
        {
            string[] words = text.Split(' ');
            string currentLine = "";
            float yOffset = 0;

            foreach (string word in words)
            {
                string testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
                Vector2 size = Raylib.MeasureTextEx(font, testLine, fontSize, spacing);

                if (size.X > maxWidth)
                {
                    // Draw current line
                    Raylib.DrawTextEx(font, currentLine, new Vector2(pos.X, pos.Y + yOffset), fontSize, spacing, color);
                    currentLine = word; // Start new line
                    yOffset += fontSize + 5; // Line spacing
                }
                else
                {
                    currentLine = testLine;
                }
            }

            // Draw last line
            if (!string.IsNullOrEmpty(currentLine))
            {
                Raylib.DrawTextEx(font, currentLine, new Vector2(pos.X, pos.Y + yOffset), fontSize, spacing, color);
            }
        }
    }
}

using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Constants used for Text Animation
        private const float TypeSpeed = 0.05f;

        // Mood Shake Constants
        private const float MoodShakeDuration = 0.3f;
        private const float MoodShakeIntensity = 5.0f;
        private const float MoodShakeSpeed = 30.0f;

        public static Vector2 GetShakeOffset()
        {
            if (moodShakeTimer > 0)
            {
                float offset = (float)Math.Sin(Raylib.GetTime() * MoodShakeSpeed) * MoodShakeIntensity * (moodShakeTimer / MoodShakeDuration);
                return new Vector2(offset, 0); // Horizontal shake
            }
            return Vector2.Zero;
        }

        public static void DrawDialogue()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            // Note: Background Panel and Option Buttons are drawn by UISystem.DrawBottomPanel()
            // We only need to draw the TEXT content here.

            DrawTextContent(screenW, screenH);
        }

        private static void DrawTextContent(int screenW, int screenH)
        {
            if (showChatLog)
            {
                DrawChatLogContent(screenW, screenH);
                return;
            }

            // Calculate Layout Match with DrawBottomPanel
            // Panel is now 75% width, left-aligned (not centered)
            int panelH = (int)(screenH * 0.3f);
            int panelY = screenH - panelH;
            int dialogW = (int)(screenW * 0.75f);  // 75% of screen width
            int dialogX = 0;  // Left-aligned, not centered

            // Text Area Padding
            int padding = 40;
            // ⬇️ TEXT WRAPPING: Adjust maxWidth here if text needs more/less space
            Rectangle textArea = new Rectangle(dialogX + padding, panelY + padding, dialogW - (padding * 2), panelH - (padding * 2));

            // 1. Name Tag
            if (!string.IsNullOrEmpty(currentName))
            {
                Vector2 nameSize = Raylib.MeasureTextEx(FontLarge, currentName, 40, 0);

                // Name Tag Position
                int nameX = dialogX + 40;
                int nameY = panelY - 20; // Slight overlap upwards

                // Cozy Name Tag
                Rectangle nameRect = new Rectangle(nameX - 20, nameY, nameSize.X + 40, nameSize.Y + 10);

                // Background
                Raylib.DrawRectangleRounded(nameRect, 0.4f, 10, ColorCharcoal);
                Raylib.DrawRectangleRoundedLines(nameRect, 0.4f, 10, ColorTan);

                // Text
                Vector2 namePos = new Vector2(nameX, nameY + 5);
                Raylib.DrawTextEx(FontLarge, currentName, namePos, 40, 0, ColorTan);
            }

            // 2. Main Text Content
            if (isWaitingForAI)
            {
                DrawAIThinking(textArea);
            }
            else if (showTextInput)
            {
                DrawTextInputContent(textArea);
            }
            else
            {
                DrawMainDialogue(textArea);
            }

            // 3. Mood HUD (Debug)
            if (TerminalSystem.DebugShowMood && currentDialogueNPC != null)
            {
                string hudText = $"Mood: {currentDialogueNPC.CurrentMood}";
                Raylib.DrawTextEx(FontMedium, hudText, new Vector2(20, 20), 32, 0, Color.Yellow);
            }
        }

        private static void DrawMainDialogue(Rectangle textArea)
        {
            string visibleText = currentText.Substring(0, charIndex);

            // Font Settings
            Font font = FontMedium;
            float fontSize = 32;
            float spacing = 2.0f;
            float lineHeight = fontSize + 8;

            Vector2 cursor = new Vector2(textArea.X, textArea.Y);
            float maxW = textArea.Width;
            float startX = textArea.X;

            // Simple Word Wrap
            string[] words = visibleText.Split(' ');
            string lineBuffer = "";
            bool firstWord = true;

            foreach (var word in words)
            {
                string[] subWords = word.Split('\n');
                for (int i = 0; i < subWords.Length; i++)
                {
                    string part = subWords[i];

                    if (i > 0) // Explicit Newline
                    {
                        Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, ColorCream);
                        cursor.X = startX;
                        cursor.Y += lineHeight;
                        lineBuffer = "";
                        firstWord = true;
                    }

                    string separator = firstWord ? "" : " ";
                    string testLine = lineBuffer + separator + part;
                    Vector2 size = Raylib.MeasureTextEx(font, testLine, fontSize, spacing);

                    if (size.X > maxW)
                    {
                        Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, ColorCream);
                        cursor.X = startX;
                        cursor.Y += lineHeight;
                        lineBuffer = part;
                        firstWord = false;
                    }
                    else
                    {
                        lineBuffer = testLine;
                        firstWord = false;
                    }
                }
            }

            // Draw remaining buffer
            if (!string.IsNullOrEmpty(lineBuffer))
            {
                Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, ColorCream);
            }
        }

        private static void DrawAIThinking(Rectangle textArea)
        {
            int dotCount = (int)thinkingDots + 1;
            string dots = new string('.', dotCount);
            Raylib.DrawTextEx(FontLarge, "Thinking" + dots, new Vector2(textArea.X, textArea.Y), 40, 1, Color.Yellow);
        }

        private static void DrawTextInputContent(Rectangle textArea)
        {
            // ⬇️ TEXT WRAPPING FOR PLAYER INPUT (matches NPC dialogue wrapping)
            string display = inputText;

            // Font Settings (same as NPC dialogue)
            Font font = FontMedium;
            float fontSize = 32;
            float spacing = 2.0f;
            float lineHeight = fontSize + 8;
            float maxW = textArea.Width;

            Vector2 cursor = new Vector2(textArea.X, textArea.Y);
            float startX = textArea.X;

            // Prompt symbol
            Raylib.DrawTextEx(font, "> ", cursor, fontSize, spacing, Color.White);
            Vector2 promptSize = Raylib.MeasureTextEx(font, "> ", fontSize, spacing);
            cursor.X += promptSize.X;
            startX = cursor.X;

            // Simple Word Wrap (same logic as NPC dialogue)
            string[] words = display.Split(' ');
            string lineBuffer = "";
            bool firstWord = true;

            foreach (var word in words)
            {
                string[] subWords = word.Split('\n');
                for (int i = 0; i < subWords.Length; i++)
                {
                    string part = subWords[i];

                    if (i > 0) // Explicit Newline
                    {
                        Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, Color.White);
                        cursor.X = startX;
                        cursor.Y += lineHeight;
                        lineBuffer = "";
                        firstWord = true;
                    }

                    string separator = firstWord ? "" : " ";
                    string testLine = lineBuffer + separator + part;
                    Vector2 size = Raylib.MeasureTextEx(font, testLine, fontSize, spacing);

                    if (size.X > maxW - promptSize.X)
                    {
                        Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, Color.White);
                        cursor.X = startX;
                        cursor.Y += lineHeight;
                        lineBuffer = part;
                        firstWord = false;
                    }
                    else
                    {
                        lineBuffer = testLine;
                        firstWord = false;
                    }
                }
            }

            // Draw remaining buffer
            if (!string.IsNullOrEmpty(lineBuffer))
            {
                Raylib.DrawTextEx(font, lineBuffer, cursor, fontSize, spacing, Color.White);
                cursor.X += Raylib.MeasureTextEx(font, lineBuffer, fontSize, spacing).X;
            }

            // Blinking Cursor
            if ((int)(Raylib.GetTime() * 2) % 2 == 0)
            {
                Raylib.DrawRectangle((int)cursor.X + 2, (int)cursor.Y, 10, (int)fontSize, Color.Yellow);
            }

            // Instructions at bottom
            Raylib.DrawTextEx(FontSmall, "ENTER to send | ESC to cancel", new Vector2(textArea.X, textArea.Y + textArea.Height - 25), 20, 0, Color.Gray);
        }

        private static void DrawChatLogContent(int screenW, int screenH)
        {
            // Full screen overlay
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 240));

            int padding = 100;
            Rectangle logRect = new Rectangle(padding, padding, screenW - (padding * 2), screenH - (padding * 2));
            // Assuming DrawCozyPanel is a new method or was intended to be used here.
            // If not, the original line was: Raylib.DrawRectangleLinesEx(logRect, 2, Color.White);
            // For now, I'll keep the user's provided line. If it causes a compile error, it needs to be defined or reverted.
            UISystem.DrawCozyPanel(logRect, "CHAT LOG");

            // Simple content rendering
            float cursorY = logRect.Y + 80;
            float lineHeight = 30;

            foreach (var entry in conversationHistory)
            {
                if (cursorY > logRect.Y + logRect.Height - 50) break;

                string line = $"{entry.Name}: {entry.Text}";
                Raylib.DrawTextEx(FontMedium, line, new Vector2(logRect.X + 20, cursorY), 24, 0, ColorCream);
                cursorY += lineHeight;
            }
        }
    }
}

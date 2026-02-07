using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace LifeSim
{
    public static partial class UISystem
    {
        // Constants
        private const float TypeSpeed = 0.05f;
        private const int BoxX = 10;
        private const int BoxY = 110;
        private const int FullBoxW = 300;
        private const int BoxH = 60;
        private const int GridSize = 10;
        private const float ScrollSpeed = 5.0f;
        private const int NameTagPadding = 4;
        private const int CloseBtnSize = 12;
        private const int OptionSpacing = 16;
        private const int MaxInputLength = 100;

        // Text Layout Constants
        private const int DialogueFontSize = 10;
        private const float TextSpacing = 1.0f;
        private const int MaxTextWidth = 240; // Approx max width in pixels
        private const int MaxLinesPerBox = 3;
        private const int LineHeightPadding = 2;

        // Mood Shake Constants
        private const float MoodShakeDuration = 0.3f;
        private const float MoodShakeIntensity = 5.0f;
        private const float MoodShakeSpeed = 30.0f;

        private static void DrawGrid(int x, int y, int w, int h, float offset, Color gridColor)
        {
            // Vertical lines (scrolling)
            for (int i = -1; i <= w / GridSize + 1; i++)
            {
                float lineX = x + (i * GridSize) + offset;
                if (lineX >= x && lineX <= x + w)
                {
                    Raylib.DrawLine((int)lineX, y, (int)lineX, y + h, gridColor);
                }
            }

            // Horizontal lines (static)
            for (int j = 0; j <= h / GridSize; j++)
            {
                int lineY = y + (j * GridSize);
                Raylib.DrawLine(x, lineY, x + w, lineY, gridColor);
            }
        }

        private static int GetBoxWidth() => showOptionsMenu ? (int)(FullBoxW * 0.75f) : FullBoxW;


        private static void DrawPortrait(int screenW, int screenH)
        {
            Raylib.DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 100));

            float scale = currentPortrait.Height > screenH * 1.5f ? (float)screenH / currentPortrait.Height : 1.0f;
            float scaledW = currentPortrait.Width * scale;
            float scaledH = currentPortrait.Height * scale;
            float posX = (screenW - scaledW) / 2;
            float basePosY = (screenH * 0.8f) - (scaledH / 1.8f);

            // Apply Mood Shake
            float shakeOffsetY = 0f;
            if (moodShakeTimer > 0)
            {
                shakeOffsetY = (float)Math.Sin(Raylib.GetTime() * MoodShakeSpeed) * MoodShakeIntensity * scale;
            }

            Raylib.DrawTextureEx(currentPortrait, new Vector2(posX, basePosY + shakeOffsetY), 0f, scale, Color.White);
        }

        private static void DrawDialogueBuffer()
        {
            int boxW = GetBoxWidth();
            float offset = ((float)Raylib.GetTime() * ScrollSpeed) % GridSize;
            Color gridColor = new(200, 200, 200, 30);

            Raylib.BeginTextureMode(UIBuffer);
            Raylib.ClearBackground(Color.Blank);

            if (showChatLog)
            {
                // Draw Full Screen Log
                Rectangle logRect = new Rectangle(5, 5, VirtualWidth - 10, VirtualHeight - 10);
                Raylib.DrawRectangleRec(logRect, new Color(0, 0, 0, 240));
                DrawGrid(5, 5, VirtualWidth - 10, VirtualHeight - 10, offset, gridColor);
                Raylib.DrawRectangleLinesEx(logRect, 1, Color.White);
            }
            else
            {
                // Main dialogue box
                Rectangle boxRect = new Rectangle(BoxX, BoxY, boxW, BoxH);
                Raylib.DrawRectangleRec(boxRect, new Color(0, 0, 0, 220));
                DrawGrid(BoxX, BoxY, boxW, BoxH, offset, gridColor);
                Raylib.DrawRectangleLinesEx(boxRect, 1, Color.White);

                // Options panel
                if (showOptionsMenu)
                {
                    int panelX = BoxX + boxW + 5;
                    int panelW = FullBoxW - boxW - 5;
                    Rectangle panelRect = new Rectangle(panelX, BoxY, panelW, BoxH);
                    Raylib.DrawRectangleRec(panelRect, new Color(0, 0, 0, 220));
                    DrawGrid(panelX, BoxY, panelW, BoxH, offset, gridColor);
                    Raylib.DrawRectangleLinesEx(panelRect, 1, Color.White);
                }

                // 1. Name Tag Box
                if (!string.IsNullOrEmpty(currentName))
                {
                    Vector2 nameSize = Raylib.MeasureTextEx(FontSmall, currentName, 12, 0);
                    Rectangle nameRect = new Rectangle(BoxX, BoxY - 18, nameSize.X + (NameTagPadding * 2), nameSize.Y + 3);
                    Raylib.DrawRectangleRec(nameRect, Color.Black);
                    Raylib.DrawRectangleLinesEx(nameRect, 1, Color.White);
                }

                // [Z] Close Button Box (Top Right)
                int closeBtnX = BoxX + boxW - CloseBtnSize - 4;
                int closeBtnY = BoxY + 4;
                Rectangle closeBtnRect = new Rectangle(closeBtnX, closeBtnY, CloseBtnSize, CloseBtnSize);
                Raylib.DrawRectangleRec(closeBtnRect, Color.Black);
                Raylib.DrawRectangleLinesEx(closeBtnRect, 1, Color.White);
            }

            // Text input box
            if (showTextInput && !showChatLog)
            {
                Rectangle inputRect = new Rectangle(BoxX + 7, BoxY + 21, boxW - 16, 22);
                Raylib.DrawRectangleRec(inputRect, new Color(30, 30, 30, 255));
                Raylib.DrawRectangleLinesEx(inputRect, 1, Color.Yellow);
            }

            Raylib.EndTextureMode();
        }

        public static void DrawDialogue()
        {
            if (!IsOpen) return;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            if (hasPortrait)
            {
                DrawPortrait(screenW, screenH);
                DrawDialogueBuffer();

                Rectangle dest = new Rectangle(0, 0, screenW, screenH);
                Rectangle flipSrc = new Rectangle(0, 0, UIBuffer.Texture.Width, -UIBuffer.Texture.Height);
                Raylib.DrawTexturePro(UIBuffer.Texture, flipSrc, dest, Vector2.Zero, 0f, Color.White);

                DrawTextContent(screenW, screenH);
            }
        }

        private static void DrawTextContent(int screenW, int screenH)
        {
            float scaleX = (float)screenW / VirtualWidth;
            float scaleY = (float)screenH / VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);
            float offX = (screenW - (VirtualWidth * scale)) / 2;
            float offY = (screenH - (VirtualHeight * scale)) / 2;

            if (showChatLog)
            {
                DrawChatLogContent(offX, offY, scale);
                return;
            }

            int boxW = GetBoxWidth();

            // Name tag
            if (!string.IsNullOrEmpty(currentName))
            {
                Vector2 nameScreenPos = new Vector2(
                    offX + ((BoxX + NameTagPadding) * scale),
                    offY + ((BoxY - 18 + 2.5f) * scale)
                );
                Raylib.DrawTextEx(FontHuge, currentName, nameScreenPos, 12 * scale, 0, Color.White);
            }

            // Close button
            int closeBtnX = BoxX + boxW - CloseBtnSize - 4;
            Vector2 closeScreenPos = new Vector2(
                offX + ((closeBtnX + 3) * scale),
                offY + ((BoxY + 4 + 2) * scale)
            );
            Raylib.DrawTextEx(FontHuge, "Z", closeScreenPos, 10 * scale, 0, Color.White);

            // Main content
            if (isWaitingForAI)
            {
                DrawAIThinking(offX, offY, scale);
            }
            else if (showTextInput)
            {
                DrawTextInputContent(boxW, offX, offY, scale);
            }
            else
            {
                DrawMainDialogue(boxW, offX, offY, scale);
            }

            // Options menu
            if (showOptionsMenu)
            {
                DrawOptionsMenuText(boxW, offX, offY, scale);
            }

            // Mood HUD
            if (TerminalSystem.DebugShowMood && currentDialogueNPC != null)
            {
                string hudText = $"Mood: {currentDialogueNPC.CurrentMood}";
                Vector2 hudPos = new Vector2(screenW - (200 * scale), 20 * scale);
                Raylib.DrawTextEx(FontHuge, hudText, hudPos, 20 * scale - 25, 0, Color.Yellow);
            }
        }

        private static void DrawChatLogContent(float offX, float offY, float scale)
        {
            // Draw Title
            Vector2 titlePos = new Vector2(offX + (10 * scale), offY + (10 * scale));
            Raylib.DrawTextEx(FontHuge, "Chat Log", titlePos, 12 * scale, 1, Color.Yellow);

            // Define content area
            float contentX = 15;
            float contentY = 30; // Start below title
            float contentW = VirtualWidth - 30;
            float contentH = VirtualHeight - 40;

            Raylib.BeginScissorMode((int)(offX + (contentX * scale)), (int)(offY + (contentY * scale)), (int)(contentW * scale), (int)(contentH * scale));

            float cursorY = 0;
            float fontSize = DialogueFontSize * scale;
            float spacing = TextSpacing * scale;
            float lineHeight = fontSize + LineHeightPadding * scale;

            // Calculate total height first for clamping scroll
            float totalHeight = 0;
            foreach (var entry in conversationHistory)
            {
                string header = $"{entry.Name}:";
                totalHeight += lineHeight + 5 * scale; // Header + padding

                // Measure body text height
                string body = entry.Text;
                float maxW = contentW * scale;
                List<string> lines = SplitTextIntoPages(body, maxW / scale);

                foreach (var page in lines)
                {
                    string[] subLines = page.Split('\n');
                    totalHeight += subLines.Length * lineHeight;
                }
                totalHeight += 10 * scale; // Spacing between messages
            }

            chatLogContentHeight = totalHeight;

            // Clamp Scroll
            float maxScroll = Math.Max(0, chatLogContentHeight - (contentH * scale));
            chatLogScroll = Math.Clamp(chatLogScroll, 0, maxScroll);

            // Draw Content
            cursorY = -chatLogScroll;

            foreach (var entry in conversationHistory)
            {
                float startEntryY = cursorY;

                // Draw Name
                Color nameColor = entry.Name == "Player" ? Color.SkyBlue : Color.Orange;
                Vector2 namePos = new Vector2(offX + (contentX * scale), offY + (contentY * scale) + cursorY);
                Raylib.DrawTextEx(FontHuge, $"{entry.Name}:", namePos, fontSize, spacing, nameColor);

                cursorY += lineHeight + 5 * scale;

                // Draw Text
                string body = entry.Text;
                float maxW = contentW * scale;
                List<string> pages = SplitTextIntoPages(body, maxW / scale);

                foreach (var page in pages)
                {
                    string[] lines = page.Split('\n');
                    foreach (var line in lines)
                    {
                        Vector2 linePos = new Vector2(offX + (contentX * scale), offY + (contentY * scale) + cursorY);
                        Raylib.DrawTextEx(FontHuge, line, linePos, fontSize, spacing, Color.White);
                        cursorY += lineHeight;
                    }
                }

                cursorY += 10 * scale; // Spacing between messages
            }

            Raylib.EndScissorMode();

            // Draw Scrollbar if needed
            if (chatLogContentHeight > contentH * scale)
            {
                float scrollRatio = chatLogScroll / maxScroll;
                float barH = (contentH * scale) * ((contentH * scale) / chatLogContentHeight);
                if (barH < 20 * scale) barH = 20 * scale;

                float barY = (contentY * scale) + (scrollRatio * ((contentH * scale) - barH));
                float barX = (contentX + contentW - 2) * scale;

                Raylib.DrawRectangle((int)(offX + barX), (int)(offY + barY), (int)(2 * scale), (int)barH, new Color(255, 255, 255, 150));
            }
        }

        private static void DrawAIThinking(float offX, float offY, float scale)
        {
            int dotCount = (int)thinkingDots + 1;
            string dots = new string('.', dotCount);
            Vector2 dotPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 8) * scale));
            Raylib.DrawTextEx(FontHuge, dots, dotPos, 16 * scale, 1, Color.Yellow);
        }

        private static void DrawTextInputContent(int boxW, float offX, float offY, float scale)
        {
            string displayText = inputText;
            if ((int)(Raylib.GetTime() * 2) % 2 == 0) displayText += "_";

            float maxInputWidth = (boxW - 16) - 8;
            while (Raylib.MeasureTextEx(FontTiny, displayText, 10, 0).X > maxInputWidth && displayText.Length > 1)
            {
                displayText = displayText.Substring(1);
            }

            Vector2 inputPos = new Vector2(offX + ((BoxX + 12) * scale), offY + ((BoxY + 30) * scale));
            Raylib.DrawTextEx(FontHuge, displayText, inputPos, 10 * scale, 0, Color.White);

            Vector2 hintPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 50) * scale));
            Raylib.DrawTextEx(FontHuge, "ENTER to send | ESC to cancel", hintPos, 10 * scale, 0, new Color(150, 150, 150, 255));
        }

        private static void DrawMainDialogue(int boxW, float offX, float offY, float scale)
        {
            string visibleText = currentText.Substring(0, charIndex);

            // --- TEXT WRAPPING CONFIGURATION ---
            // 'boxW' is the width of the dialogue box in virtual pixels.
            // '- 10' accounts for the left margin padding.
            // '- 10' accounts for right margin padding to prevent text touching the border.
            // '* scale' converts this virtual width into screen pixel width for the wrapping calculation.
            // To adjust wrapping width: modify the subtraction values here.
            float maxTextWidth = (boxW - 10 - 10) * scale;

            float fontSize = DialogueFontSize * scale; // Use Constant
            float spacing = TextSpacing * scale;
            float lineHeight = fontSize + LineHeightPadding * scale; // Spacing between lines

            Vector2 startPos = new Vector2(offX + ((BoxX + 10) * scale), offY + ((BoxY + 8) * scale));
            Vector2 currentPos = startPos;
            float leftMarginX = startPos.X; // Capture absolute left margin for wrapping

            // Parse text for asterisk-wrapped actions
            bool inAction = false;
            string currentSegment = "";

            foreach (char c in visibleText)
            {
                if (c == '*')
                {
                    // Draw accumulated segment before switching mode
                    if (currentSegment.Length > 0)
                    {
                        DrawTextSegment(ref currentPos, currentSegment, maxTextWidth, leftMarginX, fontSize, spacing, lineHeight, inAction ? Color.Yellow : Color.White);
                        currentSegment = "";
                    }
                    inAction = !inAction; // Toggle action mode
                }
                else if (c == '\n')
                {
                    // Handle explicit line break from pagination
                    if (currentSegment.Length > 0)
                    {
                        DrawTextSegment(ref currentPos, currentSegment, maxTextWidth, leftMarginX, fontSize, spacing, lineHeight, inAction ? Color.Yellow : Color.White);
                        currentSegment = "";
                    }
                    // Force carriage return
                    currentPos.X = leftMarginX;
                    currentPos.Y += lineHeight;
                }
                else
                {
                    currentSegment += c;
                }
            }

            // Draw remaining segment
            if (currentSegment.Length > 0)
            {
                DrawTextSegment(ref currentPos, currentSegment, maxTextWidth, leftMarginX, fontSize, spacing, lineHeight, inAction ? Color.Yellow : Color.White);
            }
        }

        private static void DrawTextSegment(ref Vector2 position, string segment, float maxWidth, float leftMarginX, float fontSize, float spacing, float lineHeight, Color color)
        {
            string[] words = segment.Split(' ');
            string lineBuffer = "";
            bool firstWord = true;

            foreach (var word in words)
            {
                string separator = firstWord ? "" : " ";
                string testLine = lineBuffer + separator + word;
                Vector2 size = Raylib.MeasureTextEx(FontHuge, testLine, fontSize, spacing);

                float currentLineEndX = position.X + size.X;
                float rightLimitX = leftMarginX + maxWidth;

                if (currentLineEndX > rightLimitX && lineBuffer.Length > 0)
                {
                    // Draw current line and move to next
                    Raylib.DrawTextEx(FontHuge, lineBuffer, position, fontSize, spacing, color);
                    position.X = leftMarginX; // Reset to margin
                    position.Y += lineHeight;
                    lineBuffer = word;
                    firstWord = false;
                }
                else
                {
                    lineBuffer = testLine;
                    firstWord = false;
                }
            }

            // Draw remaining text and update position
            if (lineBuffer.Length > 0)
            {
                Vector2 size = Raylib.MeasureTextEx(FontHuge, lineBuffer, fontSize, spacing);
                Raylib.DrawTextEx(FontHuge, lineBuffer, position, fontSize, spacing, color);

                // Move cursor for inline continuation
                // Use MeasureTextEx for consistency
                position.X += size.X;
            }
        }

        private static void DrawOptionsMenuText(int boxW, float offX, float offY, float scale)
        {
            int panelX = BoxX + boxW + 5;
            int optionStartY = BoxY + 8;

            for (int i = 0; i < optionLabels.Length; i++)
            {
                bool isSelected = (i == optionSelection);
                Color textColor = isSelected ? Color.Yellow : Color.White;
                string label = optionLabels[i].Length > 10 ? string.Concat(optionLabels[i].AsSpan(0, 8), "..") : optionLabels[i];

                int virtualTextY = optionStartY + (i * OptionSpacing);
                Vector2 pos = new Vector2(offX + ((panelX + 6) * scale), offY + (virtualTextY * scale));
                Raylib.DrawTextEx(FontHuge, label, pos, 12 * scale, 1, textColor);

                if (isSelected)
                {
                    Vector2 arrowPos = new Vector2(offX + ((panelX + 2) * scale), offY + (virtualTextY * scale));
                    Raylib.DrawTextEx(FontHuge, ">", arrowPos, 12 * scale, 1, Color.Yellow);
                }
            }
        }
    }
}

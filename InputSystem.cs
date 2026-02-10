using Raylib_cs;
using System.Numerics;

namespace LifeSim
{
    public static class InputSystem
    {
        public static void Initialize()
        {
            // Initialization if needed
        }

        public static void Update()
        {
            // Global Input Handling for Static Overlay

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mousePos = Raylib.GetMousePosition();
                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                // Scene Interactors (Overhang Buttons)
                // Exit Button (Top Left)
                Rectangle exitRect = new Rectangle(20, 50, 100, 40);
                if (Raylib.CheckCollisionPointRec(mousePos, exitRect))
                {
                    Engine.CurrentState = Engine.GameState.Menu;
                }
                // Terminal Button (Left Center)
                else if (Raylib.CheckCollisionPointRec(mousePos, new Rectangle(20, screenH / 2 - 40, 120, 80)))
                {
                    // Open Terminal
                    TerminalSystem.Open(Engine.ActiveNPCs);
                    Engine.CurrentState = Engine.GameState.Terminal;
                }
                // TV Button (Right Center)
                else if (Raylib.CheckCollisionPointRec(mousePos, new Rectangle(screenW - 140, screenH / 2 - 40, 120, 80)))
                {
                    // Open TV
                    TVSystem.Open();
                    Engine.CurrentState = Engine.GameState.TV;
                }

                // Player Options (THOUGHTS/ACTION/RESPOND)
                // Note: These buttons are now in a vertical side panel (right 25% of dialogue area)
                // TODO: Update these collision rects to match new side panel layout
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.OptionThoughtsRect))
                {
                    // Trigger "Thoughts"
                    // TODO: connect to Dialogue System or Thoughts Logic
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.OptionActionRect))
                {
                    // Trigger "Action"
                    // TODO: open action menu?
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.OptionRespondRect))
                {
                    // Trigger "Respond"
                    // Only works if in active conversation?
                }
            }
        }
    }
}

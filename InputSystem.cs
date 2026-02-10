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

                // Scene Interactors
                if (Raylib.CheckCollisionPointRec(mousePos, UISystem.ExitButtonRect))
                {
                    Engine.CurrentState = Engine.GameState.Menu;
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.TVButtonRect))
                {
                    // Open TV
                    TVSystem.Open();
                    Engine.CurrentState = Engine.GameState.TV;
                }
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.TerminalButtonRect))
                {
                    // Open Terminal
                    TerminalSystem.Open(Engine.ActiveNPCs);
                    Engine.CurrentState = Engine.GameState.Terminal;
                }

                // Player Options
                else if (Raylib.CheckCollisionPointRec(mousePos, UISystem.OptionThoughtsRect))
                {
                    // Trigger "Thoughts"
                    // TODO: connect to Dialogue System or Thoughts Logic
                    // For now, maybe just show a thought bubble?
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

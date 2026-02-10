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

                // Scene Interactors (Overhang Buttons) are now handled in UISystem.DrawRibbonButton

                // Player Options (THOUGHTS/ACTION/RESPOND)
                // Note: These buttons are now in a vertical side panel (right 25% of dialogue area)
                // TODO: Update these collision rects to match new side panel layout
                if (Raylib.CheckCollisionPointRec(mousePos, UISystem.OptionThoughtsRect))
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

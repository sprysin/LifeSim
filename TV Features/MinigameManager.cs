using Raylib_cs;

namespace LifeSim
{
    public enum MinigameType
    {
        TwentyQuestions
    }

    public static class MinigameManager
    {
        public static TwentyQuestionsGame? ActiveMinigame { get; private set; } = null;
        public static bool IsActive => ActiveMinigame != null;

        public static void StartMinigame(MinigameType type, NPC npc)
        {
            switch (type)
            {
                case MinigameType.TwentyQuestions:
                    ActiveMinigame = new TwentyQuestionsGame(npc);
                    ActiveMinigame.Start();
                    Engine.CurrentState = Engine.GameState.Minigame20Questions;
                    break;
            }
        }

        public static void Update()
        {
            if (ActiveMinigame == null) return;

            ActiveMinigame.Update();

            if (ActiveMinigame.IsFinished && ActiveMinigame.DismissPressed)
            {
                EndMinigame();
            }
        }

        public static void Draw()
        {
            ActiveMinigame?.Draw();
        }

        public static void EndMinigame()
        {
            ActiveMinigame = null;
            Engine.CurrentState = Engine.GameState.Exploring;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LifeSim
{
    public class TwentyQuestionsGame
    {
        public enum GamePhase
        {
            RoleSelect,
            PickingWord,
            WaitingForQuestion,
            AIAnswering,
            PlayerGuessing,
            PlayerWin,
            NPCWin,
            GaveUp,
            ShowingResult // NPC reacts via dialogue overlay
        }

        public GamePhase Phase { get; private set; } = GamePhase.RoleSelect;
        public NPC Npc { get; }
        public string SecretWord { get; private set; } = "";
        public int QuestionsAsked { get; private set; } = 0;
        public const int MaxQuestions = 20;
        public List<(string Question, string Answer)> QuestionLog { get; } = new();
        public string CurrentAIResponse { get; private set; } = "";
        public bool IsFinished => Phase == GamePhase.ShowingResult;
        public bool DismissPressed { get; set; } = false;
        public bool IsWaitingForAI { get; private set; } = false;
        public string StatusMessage { get; private set; } = "";

        // Result dialogue state
        public string ResultReaction { get; private set; } = "";
        public int ResultCharIndex { get; set; } = 0;
        public float ResultTypeTimer { get; set; } = 0f;
        public GamePhase ResultOutcome { get; private set; } = GamePhase.PlayerWin;

        private Task<string>? pendingAITask = null;

        public TwentyQuestionsGame(NPC npc)
        {
            Npc = npc;
        }

        public void Start()
        {
            Phase = GamePhase.RoleSelect;
            StatusMessage = "Choose your role!";
        }

        public void SelectRole_AskQuestions()
        {
            Phase = GamePhase.PickingWord;
            StatusMessage = $"{Npc.Name} is thinking of something...";
            IsWaitingForAI = true;

            string characterContext = "";
            if (Npc.CharacterData != null)
            {
                characterContext = $"You are {Npc.CharacterData.Name}. {Npc.CharacterData.Personality}";
            }

            // Category Selection for Variety
            string[] categories = { "Food", "Animal", "Object", "Location", "Concept/Abstract" };
            string selectedCategory = categories[new Random().Next(categories.Length)];
            Console.WriteLine($"[20Q] selected category: {selectedCategory}");

            string systemPrompt = $@"{characterContext}
 
                 You are playing 20 Questions. You need to pick a single word or short phrase (a noun) for the other player to guess. 
 
                 Rules:
                 1. Pick a word that fits the category: **{selectedCategory}**.
                 2. Pick something interesting and not too obvious, but solvable.
                 3. Respond with ONLY the word or short phrase. Nothing else. No quotes, no explanation.
                 4. Keep it to 1-3 words maximum.
                 5. Never say the word to the player unless they guess the world";

            string userPrompt = $"Pick a unique {selectedCategory} for 20 Questions. Reply with ONLY the word.";

            // Use High Temperature (1.2) and TopP (0.99) for maximum variety
            pendingAITask = GeminiService.SendRawPromptAsync(systemPrompt, userPrompt, 20, 1.2f, 0.98f);
        }

        public void Update()
        {
            if (pendingAITask != null && pendingAITask.IsCompleted)
            {
                string result = pendingAITask.Result ?? "";
                pendingAITask = null;
                IsWaitingForAI = false;
                HandleAIResponse(result.Trim());
            }
        }

        private void HandleAIResponse(string response)
        {
            switch (Phase)
            {
                case GamePhase.PickingWord:
                    SecretWord = response.Trim('.', '!', '"', '\'', ' ').ToLower();
                    if (string.IsNullOrWhiteSpace(SecretWord))
                    {
                        SecretWord = "Myself";
                    }
                    Console.WriteLine($"[20Q] Secret word: {SecretWord}");
                    Phase = GamePhase.WaitingForQuestion;
                    StatusMessage = "Ask a yes/no question!";
                    break;

                case GamePhase.AIAnswering:
                    CurrentAIResponse = response;
                    if (QuestionLog.Count > 0)
                    {
                        var last = QuestionLog[^1];
                        QuestionLog[^1] = (last.Question, response);
                    }
                    Phase = GamePhase.WaitingForQuestion;
                    StatusMessage = $"Q: {QuestionsAsked}/{MaxQuestions} — Ask another question!";
                    if (QuestionsAsked >= MaxQuestions)
                    {
                        TransitionToResult(GamePhase.NPCWin);
                    }
                    break;

                case GamePhase.PlayerGuessing:
                    string upper = response.Trim().ToUpper();
                    if (upper.StartsWith("CORRECT"))
                    {
                        TransitionToResult(GamePhase.PlayerWin);
                    }
                    else
                    {
                        CurrentAIResponse = "Nope, that's not it!";
                        Phase = GamePhase.WaitingForQuestion;
                        StatusMessage = $"Q: {QuestionsAsked}/{MaxQuestions} — Keep guessing!";
                        if (QuestionsAsked >= MaxQuestions)
                        {
                            TransitionToResult(GamePhase.NPCWin);
                        }
                    }
                    break;

                // AI reaction arrived for the result screen
                case GamePhase.PlayerWin:
                case GamePhase.NPCWin:
                case GamePhase.GaveUp:
                    ResultReaction = response;
                    ResultCharIndex = 0;
                    ResultTypeTimer = 0f;
                    Phase = GamePhase.ShowingResult;
                    break;
            }
        }

        private void TransitionToResult(GamePhase outcome)
        {
            ResultOutcome = outcome;
            Phase = outcome; // Temporarily set to win/loss/gaveup while waiting for AI
            IsWaitingForAI = true;

            // Mood Randomization Logic
            var rng = new Random();
            if (outcome == GamePhase.PlayerWin)
            {
                // Player won -> NPC Grumpy or Sad
                Npc.CurrentMood = rng.Next(2) == 0 ? "grumpy" : "sad";
            }
            else if (outcome == GamePhase.NPCWin || outcome == GamePhase.GaveUp)
            {
                // Player lost -> NPC Energized or Bashful
                Npc.CurrentMood = rng.Next(2) == 0 ? "energized" : "bashful";
            }

            // Force portrait update immediately for the result screen
            // (CurrentMood is already set, next Draw call will pick it up)

            if (outcome == GamePhase.PlayerWin)
                StatusMessage = $"VICTORY! Secret word: {SecretWord}";
            else if (outcome == GamePhase.NPCWin)
                StatusMessage = $"{Npc.Name} WINS! Secret word: {SecretWord}";
            else if (outcome == GamePhase.GaveUp)
                StatusMessage = $"You gave up! Secret word: {SecretWord}";

            // Request AI reaction
            string characterContext = "";
            if (Npc.CharacterData != null)
            {
                characterContext = CharacterLoader.BuildSystemPrompt(Npc.CharacterData, Npc.CurrentMood);
            }

            string resultContext = outcome switch
            {
                GamePhase.PlayerWin => $"The player correctly guessed your secret word \"{SecretWord}\" after {QuestionsAsked} questions. You lost! You feel {Npc.CurrentMood}.",
                GamePhase.NPCWin => $"The player failed to guess your secret word \"{SecretWord}\" after {QuestionsAsked} questions. You won! You feel {Npc.CurrentMood}.",
                GamePhase.GaveUp => $"The player gave up trying to guess your secret word \"{SecretWord}\" after {QuestionsAsked} questions. They surrendered! You feel {Npc.CurrentMood}.",
                _ => ""
            };

            string systemPrompt = $@"{characterContext}

            You just finished playing 20 Questions with the player. {resultContext}

            React to the result in character. Keep your response to 1-2 short sentences. Be expressive and show personality! (just write dialogue, no actions or mentions of what location your in)";

            string userPrompt = "React to the game result in character:";

            pendingAITask = GeminiService.SendRawPromptAsync(systemPrompt, userPrompt, 100);
        }

        public void GiveUp()
        {
            if (Phase != GamePhase.WaitingForQuestion) return;
            TransitionToResult(GamePhase.GaveUp);
        }

        public void SubmitQuestion(string question)
        {
            if (Phase != GamePhase.WaitingForQuestion) return;

            QuestionsAsked++;
            QuestionLog.Add((question, "..."));
            Phase = GamePhase.AIAnswering;
            IsWaitingForAI = true;
            StatusMessage = $"{Npc.Name} is thinking...";

            string characterContext = "";
            if (Npc.CharacterData != null)
            {
                characterContext = CharacterLoader.BuildSystemPrompt(Npc.CharacterData, Npc.CurrentMood);
            }

            string systemPrompt = $@"{characterContext}

                You are playing 20 Questions with the player. The secret word/phrase you picked is: ""{SecretWord}""

                Rules:
                1. Answer the player's yes/no question honestly but without giving the answer away.
                2. Stay in character with your personality (do not repeat yourself. Do NOT talk about not wanting to play or being bored/hungry).
                3. Keep your answer to 1-2 short sentences.
                4. You may be playfully evasive but don't lie about factual yes/no answers.
                5. If the ""{SecretWord}"" is a food (e.g Pizza or Lemonade), you must answer honestly if the player asks if its a food.
                6. Do NOT reveal the secret word or say ""{SecretWord}"" under any circumstances.";

            string history = "";
            foreach (var (q, a) in QuestionLog)
            {
                if (a != "...") history += $"Q: {q}\nA: {a}\n";
            }

            string userPrompt = $@"Previous questions:
                {history}
                Player's question #{QuestionsAsked}: {question}

                Answer this yes/no question about ""{SecretWord}"" in character:";

            pendingAITask = GeminiService.SendRawPromptAsync(systemPrompt, userPrompt, 100);
        }

        public void SubmitGuess(string guess)
        {
            if (Phase != GamePhase.WaitingForQuestion) return;

            QuestionsAsked++;
            Phase = GamePhase.PlayerGuessing;
            IsWaitingForAI = true;
            StatusMessage = $"{Npc.Name} is checking your answer...";

            string systemPrompt = $@"You are judging a guess in a game of 20 Questions.
                The secret word/phrase is: ""{SecretWord}""
                The player's guess is: ""{guess}""

                Rules:
                1. If the guess matches or is essentially the same thing (e.g., synonyms, plural/singular), respond with exactly: CORRECT
                2. If the guess is wrong, respond with exactly: INCORRECT
                3. Respond with ONLY one of those two words. Nothing else.";

            string userPrompt = $"Is \"{guess}\" the same as \"{SecretWord}\"? Reply CORRECT or INCORRECT only.";

            pendingAITask = GeminiService.SendRawPromptAsync(systemPrompt, userPrompt, 10);
        }

        public void Draw()
        {
            TwentyQuestionsUI.Draw(this);
        }
    }
}

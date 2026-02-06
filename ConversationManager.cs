using System.Collections.Generic;

namespace LifeSim
{
    public class ConversationManager
    {
        private const int MaxHistorySize = 15;
        private List<ChatMessage> history = new List<ChatMessage>();
        private string npcName;

        public ConversationManager(string npcName)
        {
            this.npcName = npcName;
        }

        public void AddUserMessage(string text)
        {
            history.Add(new ChatMessage("user", text));
            TrimHistory();
        }

        public void AddModelMessage(string text)
        {
            history.Add(new ChatMessage("model", text));
            TrimHistory();
        }

        public List<ChatMessage> GetHistory()
        {
            return new List<ChatMessage>(history);
        }

        public void Clear()
        {
            history.Clear();
        }

        public int Count => history.Count;

        private void TrimHistory()
        {
            // Keep only the last MaxHistorySize messages
            while (history.Count > MaxHistorySize)
            {
                history.RemoveAt(0);
            }
        }

        public string ShiftMood(string currentMood, string sentiment)
        {
            // Mood transition logic based on sentiment
            // sentiment: "positive", "negative", "neutral"

            // Define mood transitions
            // Positive sentiment tends toward: energized, bashful
            // Negative sentiment tends toward: grumpy, sad
            // Neutral keeps or slowly resets

            switch (sentiment)
            {
                case "positive":
                    return currentMood switch
                    {
                        "grumpy" => "neutral",      // Softens grumpiness
                        "sad" => "neutral",         // Cheers up
                        "neutral" => "energized",   // Gets excited
                        "energized" => "energized", // Stays excited
                        "bashful" => "bashful",     // Stays bashful
                        _ => "energized"
                    };

                case "negative":
                    return currentMood switch
                    {
                        "energized" => "neutral",   // Deflates
                        "bashful" => "sad",         // Gets sad
                        "neutral" => "grumpy",      // Gets annoyed
                        "grumpy" => "grumpy",       // Stays grumpy
                        "sad" => "sad",             // Stays sad
                        _ => "grumpy"
                    };

                case "neutral":
                default:
                    // Slowly drift back to neutral
                    return currentMood switch
                    {
                        "grumpy" => "neutral",
                        "energized" => "neutral",
                        _ => currentMood // Keep current mood
                    };
            }
        }
    }
}

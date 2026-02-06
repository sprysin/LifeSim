using System.Collections.Generic;

namespace LifeSim
{
    public class ConversationManager
    {
        private const int MaxHistorySize = 15;
        private List<ChatMessage> history = new List<ChatMessage>();
        private string npcName;

        public MoodWeights MoodWeights { get; set; }

        public ConversationManager(string npcName, MoodWeights? customWeights = null)
        {
            this.npcName = npcName;
            MoodWeights = customWeights ?? new MoodWeights(); // Use default if not provided
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
    }
}

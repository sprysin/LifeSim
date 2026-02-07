using System.Collections.Generic;

namespace LifeSim
{
    public class MoodWeights
    {
        public Dictionary<string, float> Weights { get; set; }

        public MoodWeights()
        {
            // Default weights - neutral is heavily favored
            Weights = new Dictionary<string, float>
            {
                { "neutral", 3.0f },      // Most likely
                { "energized", 1.0f },    // Normal likelihood
                { "grumpy", 0.9f },       // Normal likelihood
                { "sad", 0.8f },          // Slightly less likely
                { "bashful", 0.8f },      // Slightly less likely
                { "shocked", 0.5f },      // Purely reactive
                { "spooked", 0.5f }       // Purely reactive
            };
        }

        public MoodWeights(Dictionary<string, float> customWeights)
        {
            Weights = new Dictionary<string, float>(customWeights);
        }

        public string GetWeightsDescription()
        {
            // Format weights for AI prompt
            var descriptions = new List<string>();

            foreach (var mood in Weights.Keys)
            {
                float weight = Weights[mood];
                string likelihood;

                if (weight >= 3.0f) likelihood = "highly favored";
                else if (weight >= 1.5f) likelihood = "more likely";
                else if (weight >= 1.0f) likelihood = "normal";
                else if (weight >= 0.5f) likelihood = "less likely";
                else likelihood = "rarely";

                descriptions.Add($"  - {mood} (weight: {weight:F1}) - {likelihood}");
            }

            descriptions.Add("  - no_change - keep current mood");

            return string.Join("\n", descriptions);
        }
    }
}

using UnityEngine;

public static class TitleGenerator
{
    private static readonly string[] Adjectives = new[]
    {
        "Lost", "Hidden", "Secret", "Ancient", "Forgotten", "Bright", "Dark",
        "Silent", "Golden", "Broken", "Whispering", "Eternal", "Frozen", "Burning",
        "Shattered", "Endless", "Curious", "Wandering", "Sleeping", "Forgotten"
    };

    private static readonly string[] Nouns = new[]
    {
        "Library", "Book", "Page", "Chapter", "Story", "Dream", "Memory",
        "Garden", "Door", "Key", "Clock", "Mirror", "Ship", "Castle",
        "Song", "Letter", "Map", "Treasure", "Shadow", "Flame"
    };

    private static readonly string[] Prepositions = new[]
    {
        "of Time", "of Stars", "of Silence", "of the Forest", "of Dreams",
        "of Wonder", "of Magic", "of the Ocean", "of Shadows", "of Light",
        "of Dust", "of Rain", "of Winter", "of Dawn", "of Midnight"
    };

    public static string GenerateTitle()
    {
        string adj = Adjectives[Random.Range(0, Adjectives.Length)];
        string noun = Nouns[Random.Range(0, Nouns.Length)];
        string prep = Prepositions[Random.Range(0, Prepositions.Length)];
        return $"The {adj} {noun} {prep}";
    }

    public static Color GenerateColor()
    {
        float h = Random.Range(0f, 1f);
        float s = Random.Range(0.4f, 0.9f);
        float v = Random.Range(0.5f, 0.95f);
        return Color.HSVToRGB(h, s, v);
    }
}

namespace ResumeChat.Rag.Classification;

public sealed record ThreatResult(bool IsThreat, int ThreatScore = 0)
{
    public static ThreatResult Safe() => new(false);
    public static ThreatResult Threat(int score = 10) => new(true, score);
}

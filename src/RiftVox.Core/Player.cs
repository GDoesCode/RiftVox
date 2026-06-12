namespace RiftVox.Core;

public class Player
{
    public string SummonerName { get; set; } = string.Empty;
    public string ChampionName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty; // Returns "ORDER" (Blue team) or "CHAOS" (Red team)
    public bool IsDead { get; set; }

    // We will use these properties later when Option 1 (Vision) starts updating them!
    public int CurrentX { get; set; } = 0;
    public int CurrentY { get; set; } = 0;
}
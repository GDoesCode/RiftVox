namespace RiftVox.Core;

public class Player
{
    public string SummonerName { get; set; } = string.Empty;
    public string ChampionName { get; set; } = string.Empty;
        
    /// <summary>Returns "ORDER" (Blue team) or "CHAOS" (Red team).</summary>
    public string Team { get; set; } = string.Empty;
    public bool IsDead { get; set; }

    /// <summary>Horizontal pixel tracking offset relative to the isolated minimap frame.</summary>
    public int CurrentX { get; set; } = 0;

    /// <summary>Vertical pixel tracking offset relative to the isolated minimap frame.</summary>
    public int CurrentY { get; set; } = 0;
}
﻿namespace LichessNET.Entities.Social;

public class CrossTable
{
    public int TotalGames { get; set; }
    public Dictionary<string, double> Scores { get; set; }
    public Matchup? CurrentMatchup { get; set; }
}
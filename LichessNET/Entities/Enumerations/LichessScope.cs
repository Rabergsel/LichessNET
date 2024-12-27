using System.Runtime.Serialization;

namespace LichessNET.Entities.Enumerations;

public enum LichessScope
{
    // User account
    [EnumMember(Value = "email:read")] EmailRead,

    [EnumMember(Value = "preference:read")]
    PreferenceRead,

    [EnumMember(Value = "preference:write")]
    PreferenceWrite,

    // Interactions
    [EnumMember(Value = "follow:read")] FollowRead,
    [EnumMember(Value = "follow:write")] FollowWrite,
    [EnumMember(Value = "msg:write")] MsgWrite,

    // Play games
    [EnumMember(Value = "challenge:read")] ChallengeRead,

    [EnumMember(Value = "challenge:write")]
    ChallengeWrite,
    [EnumMember(Value = "challenge:bulk")] ChallengeBulk,

    [EnumMember(Value = "tournament:write")]
    TournamentWrite,

    // Teams
    [EnumMember(Value = "team:read")] TeamRead,
    [EnumMember(Value = "team:write")] TeamWrite,
    [EnumMember(Value = "team:lead")] TeamLead,

    // Puzzles
    [EnumMember(Value = "puzzle:read")] PuzzleRead,
    [EnumMember(Value = "racer:write")] RacerWrite,

    // Studies & Broadcasts
    [EnumMember(Value = "study:read")] StudyRead,
    [EnumMember(Value = "study:write")] StudyWrite,

    // External play
    [EnumMember(Value = "board:play")] BoardPlay,
    [EnumMember(Value = "bot:play")] BotPlay,

    // External engine
    [EnumMember(Value = "engine:read")] EngineRead,
    [EnumMember(Value = "engine:write")] EngineWrite
}
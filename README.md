![lichessnet](https://github.com/user-attachments/assets/8b5dfc90-7b65-4650-8537-59309f16aff6)
# LichessNET


| Forks                                                                                          | Stars  |
| -------------                                                                                  | ------------- |
| ![GitHub forks](https://img.shields.io/github/forks/rabergsel/LichessNET?style=for-the-badge)  | ![GitHub Repo stars](https://img.shields.io/github/stars/rabergsel/LichessNET?style=for-the-badge)  |


[![Doxygen Action](https://github.com/Rabergsel/LichessNET/actions/workflows/main.yml/badge.svg)](https://github.com/Rabergsel/LichessNET/actions/workflows/main.yml)
## [Documentation](https://rabergsel.github.io/LichessNET/index.html)


LichessNET is a C# library for interacting with the Lichess API. It allows you to manage games, challenges, and other Lichess features programmatically.

## Features

- Download games from Lichess
- Download games per user and tournaments
- Retrieve game information
- Accept and decline challenges
- Challenge users to games
- Get User information
- Authorize with the Lichess Token

- Too see where this trip is going, you may want to have a look to the Milestones: https://github.com/Rabergsel/LichessNET/milestones

## Installation

To install LichessNET, you can clone the repository and build the project using your preferred IDE (e.g., JetBrains Rider, Visual Studio, ...), or download it from [NuGet](https://www.nuget.org/packages/LichessNET)

| Downloads                                                       | Version  |
| -------------                                                   | ------------- |
| ![NuGet Downloads](https://img.shields.io/nuget/dt/LichessNET)  | ![NuGet Version](https://img.shields.io/nuget/v/LichessNET)  |


## Usage
To use LichessNET in your project, you will have to include the `LichessNET.API` namespace:

Without Lichess Token
```C# Without Lichess Token
using LichessNET.API;
var client = new LichessAPIClient();
```

With Lichess Token
```C# With Lichess Token
using LichessNET.API;
var client = new LichessApiClient(/* YOUR OPTIONAL LICHESS TOKEN */);
//If your token became invalid, the client will fall back to not using a token.
```

## Code Snippets

<details>
  <summary>Access games from Lichess</summary>
  By Game ID:
  
  ```C#
    var game = await client.GetGameAsync("cFcjVWzn");
  ```

  Download from the monthly database:
  ```C#
     var database = new DatabaseClient();
     await database.DownloadMonthlyDatabase(2015, 1, ChessVariant.Atomic, "2015-01", false);
  ```

You can find the Game ID of a game in the URL of the game.
</details>

<details>
  <summary>Challenges</summary>

```C#
  bool accepted = await client.AcceptChallengeAsync("challengeId");
  bool declined = await client.DeclineChallengeAsync("challengeId");
```
  
</details>

## Contributions are welcome

I welcome contributions of all kinds to LichessNET! Whether you're fixing bugs, adding features, improving documentation, or submitting issues, your input is highly appreciated. As you can see, there is still a lot of things to do. To contribute, simply fork the repository, make your changes, and submit a pull request. Thank you for helping make LichessNET better!

## Projects

If you have any cool projects that were made with this package, please let me know via an issue, so I can display it here.

## Currently supported Endpoints:
- [x] Fetching account data
- [x] Changing account settings
- [x] Fetch user profiles
- [x] Leaderboards
- [x] Downloading games by ID
- [x] Downloading games by User
- [x] Live updating chess games
- [x] Managing follows
- [x] Get ongoing games
- [x] Cloud evaluation access
- [x] Challenge management
- [x] Get puzzles
- [ ] Upload games to lichess
- [ ] Lichess TV
- [ ] Joining/Leaving a team
- [ ] Managing Team
- [ ] Board events streaming
- [ ] Queue for game
- [ ] Chatting
- [ ] Game management
- [ ] Bot endpoints
- [ ] Bulk pairings
- [ ] Arena tournaments
- [ ] Swiss tournaments
- [ ] Get current simuls
- [ ] Export studies
- [ ] Manage studies
- [ ] Send a private message
- [ ] Broadcasts
- [ ] External engine
- [ ] Opening explorer
- [ ] Tablebases
- [ ] OAuth2 (?)

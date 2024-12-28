using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;
using Microsoft.Extensions.Logging;
using Vertical.SpectreLogger;
using ZstdNet;

namespace LichessNET.Database;

/// <summary>
/// A client handling accesses to the Lichess database and downloading big files.
/// You can get access to the monthly databases.
/// </summary>
public class DatabaseClient
{
    private readonly ILogger _logger;

    public DatabaseClient()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder
            .AddSpectreConsole());
        _logger = loggerFactory.CreateLogger<DatabaseClient>();
    }

    /// <summary>
    /// Downloads a monthly database from database.lichess.org
    /// </summary>
    /// <param name="year">The year of the database. This must be 2013 or higher.</param>
    /// <param name="month">The month of the database</param>
    /// <param name="filename">The filename without extensions, as those will be appended later</param>
    /// <param name="forceDownload">Download even though the file already exists</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid date was requested</exception>
    public async Task DownloadMonthlyDatabase(int year, int month, string filename, bool forceDownload = false)
    {
        LogKnownIssues(year, month);

        if (File.Exists(filename + ".pgn") & !forceDownload)
        {
            _logger.LogInformation("File already exists, it probably already contains the requested data.");
            return;
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");
        }

        if (year < 2013)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be 2013 or later");
        }

        string url =
            $"https://database.lichess.org/standard/lichess_db_standard_rated_{year}-{month.ToString().PadLeft(2, '0')}.pgn.zst";

        _logger.LogInformation($"Requesting database for {year}-{month.ToString().PadLeft(2, '0')}");

        using (var client = new HttpClientDownloadWithProgress(url, filename + ".zst"))
        {
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                _logger.LogInformation(
                    $"Downloading database for {year}-{month.ToString().PadLeft(2, '0')}: {progressPercentage}% ({totalBytesDownloaded.ToSIPrefix()}B/{totalFileSize?.ToSIPrefix()}B)");
            };

            await client.StartDownload();
        }

        using (var inputStream = new FileStream(filename + ".zst", FileMode.Open, FileAccess.Read))
        using (var outputStream = new FileStream(filename + ".pgn", FileMode.Create, FileAccess.Write))
        using (var decompressionStream = new DecompressionStream(inputStream))
        {
            await decompressionStream.CopyToAsync(outputStream);
        }

        //Deleting the compressed file to save space
        File.Delete(filename + ".zst");

        _logger.LogInformation($"File saved to {filename}.pgn");
    }

    /// <summary>
    /// Downloads the monthly variant database from database.lichess.org
    /// </summary>
    /// <param name="year">The year of the database</param>
    /// <param name="month">The month of the database</param>
    /// <param name="variant">The variant of which to download the database</param>
    /// <param name="filename"></param>
    /// <param name="forceDownload"></param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid date or variant was requested</exception>
    public async Task DownloadMonthlyDatabase(int year, int month, ChessVariant variant, string filename,
        bool forceDownload = false)
    {
        if (variant == ChessVariant.Racer || variant == ChessVariant.Storm || variant == ChessVariant.Streak)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), "This variant is not supported by the database.");
        }

        if (File.Exists(filename + ".pgn") & !forceDownload)
        {
            _logger.LogInformation("File already exists, it probably already contains the requested data.");
            return;
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");
        }

        if (year < 2013)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be 2013 or later");
        }

        string url =
            $"https://database.lichess.org/{variant.GetEnumMemberValue()}/lichess_db_{variant.GetEnumMemberValue()}_rated_{year}-{month.ToString().PadLeft(2, '0')}.pgn.zst";

        _logger.LogInformation($"Requesting database for {year}-{month.ToString().PadLeft(2, '0')}");

        using (var client = new HttpClientDownloadWithProgress(url, filename + ".zst"))
        {
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                _logger.LogInformation(
                    $"Downloading database for {year}-{month.ToString().PadLeft(2, '0')}: {progressPercentage}% ({totalBytesDownloaded.ToSIPrefix()}B/{totalFileSize?.ToSIPrefix()}B)");
            };

            await client.StartDownload();
        }

        using (var inputStream = new FileStream(filename + ".zst", FileMode.Open, FileAccess.Read))
        using (var outputStream = new FileStream(filename + ".pgn", FileMode.Create, FileAccess.Write))
        using (var decompressionStream = new DecompressionStream(inputStream))
        {
            await decompressionStream.CopyToAsync(outputStream);
        }

        //Deleting the compressed file to save space
        File.Delete(filename + ".zst");

        _logger.LogInformation($"File saved to {filename}.pgn");
    }

    private void LogKnownIssues(int year, int month)
    {
        var knownIssues = new Dictionary<(int year, int month), string>
        {
            { (2023, 11), "Some Chess960 rematches were recorded with invalid castling rights in their starting FEN." },
            { (2022, 12), "Some Antichess games were recorded with bullet ratings." },
            {
                (2021, 3),
                "Some games have incorrect results due to a database outage in the aftermath of a datacenter fire."
            },
            {
                (2021, 2),
                "Some games were resigned even after the game ended. In variants, additional moves could be played after the end of the game."
            },
            {
                (2020, 12),
                "Many variant games have been mistakenly analyzed using standard NNUE, leading to incorrect evaluations."
            },
            {
                (2021, 1),
                "Many variant games have been mistakenly analyzed using standard NNUE, leading to incorrect evaluations."
            },
            {
                (2020, 7),
                "Many games, especially variant games, may have incorrect evaluations in the opening (up to 15 plies)."
            },
            {
                (2020, 8),
                "Many games, especially variant games, may have incorrect evaluations in the opening (up to 15 plies)."
            },
            { (2016, 12), "Many games may have incorrect evaluations." },
            { (2016, 6), "Some players were able to play themselves in rated games." },
            { (2016, 8), "7 games with illegal castling moves were recorded." }
        };

        if (knownIssues.TryGetValue((year, month), out var issue))
        {
            _logger.LogWarning($"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: {issue}");
        }
        else if (year < 2016)
        {
            _logger.LogWarning(
                $"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: In some cases, mate may not be forced in the number of moves given by the evaluations.");
        }
        else if (year == 2020 && month <= 12)
        {
            _logger.LogWarning(
                $"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: Some exports are missing the redundant (but strictly speaking mandatory) Round tag (always -), Date tag (see UTCDate & UTCTime instead), and black move numbers after comments. This may be fixed by a future re-export.");
        }
    }
}
﻿using Microsoft.Extensions.Logging;
using TokenBucket;
using Vertical.SpectreLogger;

namespace LichessNET.API;

/// <summary>
/// The APIRatelimitController class manages and enforces rate limiting
/// for API requests to prevent abuse and ensure fair usage of system resources.
/// </summary>
public class ApiRatelimitController
{
    private static ILogger _logger = null!;

    private readonly Dictionary<string, ITokenBucket> _buckets = new();

    private readonly ITokenBucket _defaultBucket = TokenBuckets.Construct().WithCapacity(5)
        .WithFixedIntervalRefillStrategy(5, TimeSpan.FromSeconds(15)).Build();

    private int _pipedRequests;

    private DateTime _rateLimitedUntil = DateTime.MinValue;

    public ApiRatelimitController()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder
            .AddSpectreConsole());

        _logger = loggerFactory.CreateLogger("APIRateLimitController");
    }


    public int PipedRequests
    {
        get => _pipedRequests;
        internal set
        {
            _pipedRequests = value;
            if (PipedRequests > 5)
                _logger.LogWarning(
                    $"Currently there are {PipedRequests} requests in queue. Either the API is blocking requests, or the client is sending too many requests.");
        }
    }

    public void ReportBlock(int seconds = 60)
    {
        _logger.LogWarning("API Call reported Ratelimit block for " + seconds + " seconds");
        _rateLimitedUntil = DateTime.Now.AddSeconds(seconds);
    }

    public void RegisterBucket(string endpointUrl, ITokenBucket bucket)
    {
        _buckets.Add(endpointUrl, bucket);
    }

    public void Consume()
    {
        PipedRequests++;
        if (_rateLimitedUntil > DateTime.Now)
        {
            _logger.LogWarning("Endpoint blocked due to ratelimit. Waiting for " +
                               (_rateLimitedUntil - DateTime.Now).Milliseconds + " ms.");
            Thread.Sleep((_rateLimitedUntil - DateTime.Now).Milliseconds);
        }

        _defaultBucket.Consume();
        PipedRequests--;
    }

    public void Consume(string endpointUrl, bool consumedefaultBucket)
    {
        PipedRequests++;
        if (_rateLimitedUntil > DateTime.Now)
        {
            _logger.LogWarning("Endpoint call to " + endpointUrl + " blocked due to ratelimit. Waiting for " +
                               (_rateLimitedUntil - DateTime.Now).Milliseconds + " ms.");
            Thread.Sleep((_rateLimitedUntil - DateTime.Now).Milliseconds);
        }

        if (consumedefaultBucket) _defaultBucket.Consume();
        if (_buckets.TryGetValue(endpointUrl, out var bucket)) bucket.Consume();

        PipedRequests--;
    }
}
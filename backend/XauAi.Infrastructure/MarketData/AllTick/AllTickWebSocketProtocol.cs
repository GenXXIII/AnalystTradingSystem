using System.Globalization;
using System.Text.Json;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed record AllTickTradeTick(DateTimeOffset TimestampUtc, decimal Price, decimal? Volume);

internal sealed record AllTickOrderBookTick(DateTimeOffset TimestampUtc, decimal Bid, decimal Ask);

internal static class AllTickWebSocketProtocol
{
    public static string Subscription(int commandId, string symbol) => JsonSerializer.Serialize(new
    {
        cmd_id = commandId,
        seq_id = Random.Shared.Next(1, int.MaxValue),
        trace = Guid.NewGuid().ToString("N"),
        data = new
        {
            symbol_list = new[]
            {
                commandId == 22002
                    ? new Dictionary<string, object> { ["code"] = symbol, ["depth_level"] = 1 }
                    : new Dictionary<string, object> { ["code"] = symbol }
            }
        }
    });

    public static string Heartbeat() => JsonSerializer.Serialize(new
    {
        cmd_id = 22000,
        seq_id = Random.Shared.Next(1, int.MaxValue),
        trace = Guid.NewGuid().ToString("N"),
        data = new { }
    });

    public static bool TryReadTrade(string json, out AllTickTradeTick? tick)
    {
        tick = null;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!TryReadCommand(root, 22998, out var data)
            || !TryDecimal(data, "price", out var price)
            || !TryTimestamp(data, out var timestamp))
        {
            return false;
        }

        TryDecimal(data, "volume", out var volume);
        tick = new AllTickTradeTick(timestamp, price, volume);
        return price > 0 && volume >= 0;
    }

    public static bool TryReadOrderBook(string json, out AllTickOrderBookTick? tick)
    {
        tick = null;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!TryReadCommand(root, 22999, out var data)
            || !TryTimestamp(data, out var timestamp)
            || !TryFirstPrice(data, "bids", out var bid)
            || !TryFirstPrice(data, "asks", out var ask))
        {
            return false;
        }

        tick = new AllTickOrderBookTick(timestamp, bid, ask);
        return bid > 0 && ask >= bid;
    }

    public static bool IsRejected(string json, out int commandId)
    {
        commandId = 0;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("ret", out var returnCode)
            || !TryInt32(returnCode, out var value)
            || value == 200)
        {
            return false;
        }

        if (root.TryGetProperty("cmd_id", out var command) && TryInt32(command, out var parsed))
        {
            commandId = parsed;
        }

        return true;
    }

    private static bool TryReadCommand(JsonElement root, int expected, out JsonElement data)
    {
        data = default;
        return root.TryGetProperty("cmd_id", out var command)
            && TryInt32(command, out var commandId)
            && commandId == expected
            && root.TryGetProperty("data", out data);
    }

    private static bool TryFirstPrice(JsonElement data, string name, out decimal price)
    {
        price = 0;
        return data.TryGetProperty(name, out var values)
            && values.ValueKind == JsonValueKind.Array
            && values.GetArrayLength() > 0
            && TryDecimal(values[0], "price", out price);
    }

    private static bool TryTimestamp(JsonElement data, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (!data.TryGetProperty("tick_time", out var value)
            || !TryInt64(value, out var unix))
        {
            return false;
        }

        try
        {
            timestamp = AllTickHttpClient.FromUnixTimestamp(unix);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool TryDecimal(JsonElement data, string name, out decimal value)
    {
        value = 0;
        if (!data.TryGetProperty(name, out var property))
        {
            return false;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                property.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value),
            _ => false
        };
    }

    private static bool TryInt32(JsonElement value, out int result)
    {
        result = 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out result),
            JsonValueKind.String => int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }

    private static bool TryInt64(JsonElement value, out long result)
    {
        result = 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt64(out result),
            JsonValueKind.String => long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }
}

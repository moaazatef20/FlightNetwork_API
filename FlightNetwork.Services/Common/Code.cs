namespace FlightNetwork.Services.Common;

/// <summary>
/// Airport and airline codes are stored upper-case (IATA/ICAO convention) and matched with exact
/// string equality in Cypher, so a lower-case lookup from a caller would silently miss.
/// </summary>
internal static class Code
{
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}

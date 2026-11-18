namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents country information.
/// </summary>
/// <param name="CountryCode">The numeric country code.</param>
/// <param name="CountryName">The country name.</param>
public record CountryInfo(int? CountryCode, string? CountryName);
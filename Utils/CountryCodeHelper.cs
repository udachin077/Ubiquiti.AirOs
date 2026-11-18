namespace Ubiquiti.AirOs.Utils;

/// <summary>
/// Helper class for converting between country codes and country names.
/// </summary>
public static class CountryCodeHelper
{
    private static readonly Dictionary<int, string> CountryCodes = new()
    {
        { 0, "Select Your Country" },
        { 32, "Argentina" },
        { 51, "Armenia" },
        { 533, "Aruba" },
        { 36, "Australia" },
        { 40, "Austria" },
        { 31, "Azerbaijan" },
        { 48, "Bahrain" },
        { 52, "Barbados" },
        { 112, "Belarus" },
        { 56, "Belgium" },
        { 84, "Belize" },
        { 68, "Bolivia" },
        { 70, "Bosnia and Herzegovina" },
        { 76, "Brazil" },
        { 96, "Brunei Darussalam" },
        { 100, "Bulgaria" },
        { 116, "Cambodia" },
        { 124, "Canada" },
        { 2124, "Canada (Licensed)" },
        { 152, "Chile" },
        { 156, "China" },
        { 170, "Colombia" },
        { 188, "Costa Rica" },
        { 191, "Croatia" },
        { 196, "Cyprus" },
        { 203, "Czech Republic" },
        { 208, "Denmark" },
        { 214, "Dominican Republic" },
        { 218, "Ecuador" },
        { 818, "Egypt" },
        { 222, "El Salvador" },
        { 233, "Estonia" },
        { 246, "Finland" },
        { 250, "France" },
        { 268, "Georgia" },
        { 276, "Germany" },
        { 300, "Greece" },
        { 304, "Greenland" },
        { 308, "Grenada" },
        { 316, "Guam" },
        { 320, "Guatemala" },
        { 332, "Haiti" },
        { 340, "Honduras" },
        { 344, "Hong Kong" },
        { 348, "Hungary" },
        { 352, "Iceland" },
        { 356, "India" },
        { 360, "Indonesia" },
        { 368, "Iraq" },
        { 372, "Ireland" },
        { 376, "Israel" },
        { 380, "Italy" },
        { 388, "Jamaica" },
        { 400, "Jordan" },
        { 398, "Kazakhstan" },
        { 404, "Kenya" },
        { 410, "Korea Republic" },
        { 414, "Kuwait" },
        { 428, "Latvia" },
        { 422, "Lebanon" },
        { 511, "Licensed" },
        { 438, "Liechtenstein" },
        { 440, "Lithuania" },
        { 442, "Luxembourg" },
        { 446, "Macau" },
        { 807, "Macedonia" },
        { 458, "Malaysia" },
        { 470, "Malta" },
        { 484, "Mexico" },
        { 492, "Monaco" },
        { 499, "Montenegro" },
        { 504, "Morocco" },
        { 524, "Nepal" },
        { 528, "Netherlands" },
        { 530, "Netherlands Antilles" },
        { 554, "New Zealand" },
        { 566, "Nigeria" },
        { 578, "Norway" },
        { 512, "Oman" },
        { 586, "Pakistan" },
        { 591, "Panama" },
        { 598, "Papua New Guinea" },
        { 600, "Paraguay" },
        { 604, "Peru" },
        { 608, "Philippines" },
        { 616, "Poland" },
        { 620, "Portugal" },
        { 630, "Puerto Rico (U.S. territory)" },
        { 634, "Qatar" },
        { 642, "Romania" },
        { 643, "Russia" },
        { 646, "Rwanda" },
        { 652, "Saint Barthelemy" },
        { 682, "Saudi Arabia" },
        { 688, "Serbia" },
        { 702, "Singapore" },
        { 703, "Slovakia" },
        { 705, "Slovenia" },
        { 710, "South Africa" },
        { 724, "Spain" },
        { 144, "Sri Lanka" },
        { 748, "Swaziland" },
        { 752, "Sweden" },
        { 756, "Switzerland" },
        { 158, "Taiwan" },
        { 764, "Thailand" },
        { 780, "Trinidad and Tobago" },
        { 788, "Tunisia" },
        { 792, "Turkey" },
        { 804, "Ukraine" },
        { 784, "United Arab Emirates" },
        { 826, "United Kingdom" },
        { 840, "United States" },
        { 858, "Uruguay" },
        { 860, "Uzbekistan" },
        { 862, "Venezuela" },
        { 704, "Viet Nam" }
    };

    /// <summary>
    /// Gets the country name for a given numeric country code.
    /// </summary>
    /// <param name="code">The numeric country code.</param>
    /// <returns>The country name, or <see langword="null"/> if not found.</returns>
    public static string? GetCountryName(int code)
    {
        return CountryCodes.TryGetValue(code, out var name) ? name : null;
    }

    /// <summary>
    /// Gets the numeric country code for a given country name.
    /// </summary>
    /// <param name="name">The country name (case-insensitive).</param>
    /// <returns>The numeric country code, or <see langword="null"/> if not found.</returns>
    public static int? GetCountryCode(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var kvp in CountryCodes)
        {
            if (string.Equals(kvp.Value, name, StringComparison.OrdinalIgnoreCase))
                return kvp.Key;
        }

        foreach (var kvp in CountryCodes)
        {
            if (kvp.Value.Contains(name, StringComparison.OrdinalIgnoreCase))
                return kvp.Key;
        }

        return null;
    }

    /// <summary>
    /// Gets all available country codes and names.
    /// </summary>
    /// <returns>A read-only dictionary of country codes and names.</returns>
    public static IReadOnlyDictionary<int, string> GetAllCountries()
    {
        return CountryCodes;
    }
}
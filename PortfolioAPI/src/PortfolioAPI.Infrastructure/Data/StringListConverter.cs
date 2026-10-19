using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data
{
    /// <summary>
    /// Converts the <c>List&lt;string&gt;</c> collections on Project/Experience
    /// into a comma-separated column (and back), so no schema-level array support
    /// is required from the database provider.
    /// </summary>
    public static class StringListConverter
    {
        public static ValueConverter<List<string>, string> CommaSeparated { get; } = new(
            list => string.Join(", ", list ?? new List<string>()),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(item => item.Trim())
                       .Where(item => item.Length > 0)
                       .ToList());
    }

    /// <summary>JSON column converters for structured lists (Project metrics, credential highlights).</summary>
    public static class JsonListConverter
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static ValueConverter<List<ProjectMetric>, string> MetricsJson { get; } = new(
            list => JsonSerializer.Serialize(list ?? new List<ProjectMetric>(), Options),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<ProjectMetric>()
                : JsonSerializer.Deserialize<List<ProjectMetric>>(value, Options) ?? new List<ProjectMetric>());

        public static ValueConverter<List<string>, string> HighlightsJson { get; } = new(
            list => JsonSerializer.Serialize(list ?? new List<string>(), Options),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(value, Options) ?? new List<string>());
    }
}

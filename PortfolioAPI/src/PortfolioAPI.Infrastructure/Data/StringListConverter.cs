using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PortfolioAPI.Core.Entities;

namespace PortfolioAPI.Infrastructure.Data
{
    /// <summary>
    /// Converts the <c>List&lt;string&gt;</c> collections on Project/Experience
    /// into a comma-separated column (and back), so no schema-level array support
    /// is required from the database provider.
    ///
    /// Two variants exist because EF's HasConversion infers the model type from
    /// the property and enforces nullability: the non-null variant is used on
    /// required list properties (Project/Experience.Technologies), the nullable
    /// variant on optional ones (Project.Badges, Credential.Stack) where null
    /// means "none". The lambdas are identical; only the compile-time
    /// nullability annotation differs.
    /// </summary>
    public static class StringListConverter
    {
        public static ValueConverter<List<string>, string> CommaSeparated { get; } = new(
            list => string.Join(", ", list),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(item => item.Trim())
                       .Where(item => item.Length > 0)
                       .ToList());

        public static ValueConverter<List<string>?, string> CommaSeparatedOrNull { get; } = new(
            list => string.Join(", ", list ?? new List<string>()),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(item => item.Trim())
                       .Where(item => item.Length > 0)
                       .ToList());

        /// <summary>
        /// Deep-copying comparer for the required List&lt;string&gt; properties.
        /// Without an explicit comparer EF falls back to REFERENCE equality for
        /// these mutable classes (warning EF10620): the change-tracking snapshot
        /// aliases the live instance, so an in-place mutation
        /// (<c>project.Technologies.Add("Vue")</c>) was silently never persisted —
        /// a bug the old full-row <c>DbSet.Update</c> happened to mask.
        /// </summary>
        public static ValueComparer<List<string>> ListComparer { get; } = new(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            c => (c ?? new List<string>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            c => (c ?? new List<string>()).ToList());

        /// <summary>Null-tolerant twin of <see cref="ListComparer"/> for the optional list properties.</summary>
        public static ValueComparer<List<string>?> ListComparerOrNull { get; } = new(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            c => (c ?? new List<string>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            c => (c ?? new List<string>()).ToList());
    }

    /// <summary>JSON column converters for structured lists (Project metrics, credential highlights).</summary>
    public static class JsonListConverter
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static ValueConverter<List<ProjectMetric>?, string> MetricsJson { get; } = new(
            list => JsonSerializer.Serialize(list ?? new List<ProjectMetric>(), Options),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<ProjectMetric>()
                : JsonSerializer.Deserialize<List<ProjectMetric>>(value, Options) ?? new List<ProjectMetric>());

        public static ValueConverter<List<string>?, string> HighlightsJson { get; } = new(
            list => JsonSerializer.Serialize(list ?? new List<string>(), Options),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(value, Options) ?? new List<string>());

        /// <summary>
        /// Snapshot clones each metric (they are mutable POCOs) so in-place
        /// edits are detected; equality is member-wise so replacing the list
        /// with equal-valued metrics is not seen as a change.
        /// </summary>
        public static ValueComparer<List<ProjectMetric>?> MetricsComparer { get; } = new(
            (a, b) => MetricSequenceEqual(a, b),
            c => (c ?? new List<ProjectMetric>())
                .Aggregate(0, (hash, m) => HashCode.Combine(hash, m == null ? string.Empty : m.LabelEn, m == null ? string.Empty : m.Value)),
            c => (c ?? new List<ProjectMetric>())
                .Select(m => m == null
                    ? new ProjectMetric()
                    : new ProjectMetric { LabelEn = m.LabelEn, LabelAr = m.LabelAr, Value = m.Value }).ToList());

        private static bool MetricSequenceEqual(List<ProjectMetric>? a, List<ProjectMetric>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            var left = a ?? new List<ProjectMetric>();
            var right = b ?? new List<ProjectMetric>();
            if (left.Count != right.Count) return false;
            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i]?.LabelEn, right[i]?.LabelEn, StringComparison.Ordinal)
                    || !string.Equals(left[i]?.LabelAr, right[i]?.LabelAr, StringComparison.Ordinal)
                    || !string.Equals(left[i]?.Value, right[i]?.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

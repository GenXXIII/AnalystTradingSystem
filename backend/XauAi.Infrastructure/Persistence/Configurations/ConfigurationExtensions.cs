using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace XauAi.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> UtcConverter =
        new(value => value.ToUniversalTime(), value => value.ToUniversalTime());

    private static readonly ValueConverter<DateTimeOffset?, DateTimeOffset?> NullableUtcConverter =
        new(
            value => value.HasValue ? value.Value.ToUniversalTime() : value,
            value => value.HasValue ? value.Value.ToUniversalTime() : value);

    public static PropertyBuilder<DateTimeOffset> IsUtcTimestamp(
        this PropertyBuilder<DateTimeOffset> property) =>
        property.HasConversion(UtcConverter).HasColumnType("datetimeoffset(7)");

    public static PropertyBuilder<DateTimeOffset?> IsUtcTimestamp(
        this PropertyBuilder<DateTimeOffset?> property) =>
        property.HasConversion(NullableUtcConverter).HasColumnType("datetimeoffset(7)");
}

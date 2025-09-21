using System.ComponentModel.DataAnnotations;

namespace ThirteenIsh.Database.Entities;

/// <summary>
/// An entity that has a Name and also a derived NameUpper property.
/// When deriving from this, create suitable unique indexes over both Name and NameUpper.
/// </summary>
public class SearchableNamedEntityBase : EntityBase
{
    /// <summary>
    /// The name.
    /// </summary>
    [MaxLength(40)]
    public required string Name { get; set; }

    /// <summary>
    /// The name in upper case, for searching.
    /// (The `private set { }` is required so EF Core maps it. See
    /// https://stackoverflow.com/questions/52228618/does-ef-core-provide-a-way-to-map-a-get-only-property-to-database )
    /// </summary>
    public string NameUpper { get => Name.ToUpperInvariant(); private set { } }
}

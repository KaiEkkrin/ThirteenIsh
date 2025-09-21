using System.ComponentModel.DataAnnotations;

namespace ThirteenIsh.Database.Entities;

public class EntityBase
{
    public long Id { get; set; }

    /// <summary>
    /// The concurrency token -- see https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=data-annotations
    /// </summary>
    [Timestamp]
    public uint Version { get; set; }
}

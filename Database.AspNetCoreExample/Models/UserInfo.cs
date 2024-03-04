using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#pragma warning disable CS8618

namespace Database.AspNetCoreExample.Models;

/// <summary>
/// Информация о пользователе.
/// </summary>
[Table("user_info", Schema = "public")]
public sealed class UserInfo : IEquatable<UserInfo>
{
    /// <summary>Идентификтаор пользователя.</summary>
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>Логин пользователя.</summary>
    [Column("login", TypeName = "varchar(100)"), Required]
    public string Login { get; set; }

    /// <summary>Дата\время создания пользователя.</summary>
    [Column("created_on"), Required]
    public DateTime CreationDate { get; set; }

    /// <inheritdoc />
    public bool Equals(UserInfo? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Login == other.Login && CreationDate.Equals(other.CreationDate);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is UserInfo other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Login, CreationDate);
}

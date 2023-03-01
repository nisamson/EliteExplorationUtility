using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace EEU.Model;

public class CodexEntry {
    [Column(TypeName = "NVARCHAR(MAX)")] public string Value { get; set; }

    [AllowNull]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    [Column(TypeName = "VARBINARY(64)")]
    public byte[] ValueHash { get; }
}

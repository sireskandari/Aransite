using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Domain.Entities.Auth;
public class RefreshToken
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;      // opaque random
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime? RevokedUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }    // rotation chain
    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}

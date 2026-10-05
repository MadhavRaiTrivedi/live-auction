using System.ComponentModel.DataAnnotations;

namespace LiveAuction.Api.Http;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PositiveAmountAttribute : RangeAttribute
{
    public PositiveAmountAttribute()
        : base(1, long.MaxValue)
    {
        ErrorMessage = "{0} must be at least 1 paisa.";
    }
}

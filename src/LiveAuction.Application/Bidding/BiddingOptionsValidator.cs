using Microsoft.Extensions.Options;

namespace LiveAuction.Application.Bidding;

internal sealed class BiddingOptionsValidator : IValidateOptions<BiddingOptions>
{
    public ValidateOptionsResult Validate(string? name, BiddingOptions options)
    {
        if (options.MaxConcurrencyAttempts < 1)
        {
            return ValidateOptionsResult.Fail("Bidding:MaxConcurrencyAttempts must be at least 1.");
        }

        try
        {
            options.ToRules();
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException exception)
        {
            return ValidateOptionsResult.Fail($"Bidding:IncrementTiers is invalid. {exception.Message}");
        }
    }
}

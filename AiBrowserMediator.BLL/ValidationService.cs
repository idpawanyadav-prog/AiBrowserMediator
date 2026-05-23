using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.BLL;

public sealed class ValidationService(IBrowserSession browserSession) : IValidationService
{
    public async Task<bool> HasExpectedValueAsync(LocatorDto locator, string controlType, string expectedValue, CancellationToken cancellationToken = default)
        => await browserSession.MatchesValueAsync(locator, controlType, expectedValue, cancellationToken);
}

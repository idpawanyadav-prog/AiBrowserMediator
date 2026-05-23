using AiBrowserMediator.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AiBrowserMediator.API.Controllers;

[ApiController]
[Route("capabilities")]
public sealed class CapabilitiesController(
    ICapabilityRegistry capabilityRegistry,
    IStrategyFunctionExecutor strategyFunctionExecutor) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<CapabilitySummaryDto>> GetAll()
        => Ok(capabilityRegistry.GetSummaries());

    [HttpGet("{name}")]
    public ActionResult<CapabilityDescriptorDto> GetOne(string name)
    {
        var descriptor = capabilityRegistry.GetDescriptor(name);
        return descriptor is null ? NotFound() : Ok(descriptor);
    }

    [HttpGet("functions")]
    public ActionResult<IReadOnlyList<StrategyFunctionInfoDto>> GetFunctions(
        [FromQuery] string? controlType,
        [FromQuery] string? locatorType,
        [FromQuery] string? action)
        => Ok(strategyFunctionExecutor.GetFunctions(controlType, locatorType, action));
}

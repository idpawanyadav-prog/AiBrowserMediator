using System.Text.RegularExpressions;
using AiBrowserMediator.Contracts;

namespace AiBrowserMediator.BLL;

public sealed partial class PageDescriptionService(IBrowserSession browserSession) : IPageDescriptionService
{
    public async Task<PageDescriptionDto> DescribeAsync(CancellationToken cancellationToken = default)
    {
        var html = await browserSession.GetPageSourceAsync(cancellationToken);
        var controls = ControlRegex().Matches(html)
            .Select(match =>
            {
                var tag = match.Groups["tag"].Value.ToLowerInvariant();
                var attrs = match.Groups["attrs"].Value;
                var inputType = GetAttribute(attrs, "type");
                return new PageControlDto(
                    Tag: tag,
                    ControlType: InferControlType(tag, inputType),
                    Id: GetAttribute(attrs, "id"),
                    Name: GetAttribute(attrs, "name"),
                    InputType: inputType,
                    Disabled: HasAttribute(attrs, "disabled"),
                    ReadOnly: HasAttribute(attrs, "readonly"));
            })
            .ToArray();

        return new PageDescriptionDto(controls);
    }

    private static string InferControlType(string tag, string? inputType)
        => tag switch
        {
            "textarea" => "textbox",
            "select" => "combobox",
            "input" when string.Equals(inputType, "checkbox", StringComparison.OrdinalIgnoreCase) => "checkbox",
            "input" when string.Equals(inputType, "radio", StringComparison.OrdinalIgnoreCase) => "radio",
            "input" when string.Equals(inputType, "submit", StringComparison.OrdinalIgnoreCase) => "button",
            "input" when string.Equals(inputType, "button", StringComparison.OrdinalIgnoreCase) => "button",
            "input" => "textbox",
            _ => "generic"
        };

    private static string? GetAttribute(string attrs, string name)
    {
        var match = Regex.Match(attrs, $@"\b{name}\s*=\s*[""'](?<value>[^""']*)[""']", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static bool HasAttribute(string attrs, string name)
        => Regex.IsMatch(attrs, $@"\b{name}\b", RegexOptions.IgnoreCase);

    [GeneratedRegex("<(?<tag>input|textarea|select)\\b(?<attrs>[^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex ControlRegex();
}

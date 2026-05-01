using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Frontend.TagHelpers;

[HtmlTargetElement("avatar")]
public class AvatarTagHelper : TagHelper
{
    public string Initials { get; set; } = "A";
    public string Size { get; set; } = "sm";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        var className = Size == "lg" ? "avatar-lg" : "avatar";
        output.Attributes.SetAttribute("class", className);
        output.Content.SetContent(Initials);
    }
}

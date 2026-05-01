using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Frontend.TagHelpers;

[HtmlTargetElement("pill")]
public class PillTagHelper : TagHelper
{
    public string Type { get; set; } = "soft"; // success, danger, warn, info, violet, soft
    public bool Dot { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.Attributes.SetAttribute("class", $"pill pill-{Type}");
        
        if (Dot)
        {
            output.PreContent.SetHtmlContent("<span class=\"dot\"></span>");
        }
    }
}

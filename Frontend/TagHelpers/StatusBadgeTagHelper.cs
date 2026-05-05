using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Frontend.TagHelpers;

[HtmlTargetElement("status-badge")]
public class StatusBadgeTagHelper : TagHelper
{
    public int Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        
        if (Value == 1)
        {
            output.Attributes.SetAttribute("class", "pill pill-success");
            output.Content.SetContent("Đang hoạt động");
        }
        else
        {
            output.Attributes.SetAttribute("class", "pill pill-danger");
            output.Content.SetContent("Đã khóa");
        }
        output.PreContent.SetHtmlContent("<span class=\"dot\"></span>");
    }
}

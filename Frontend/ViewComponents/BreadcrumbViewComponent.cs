using Microsoft.AspNetCore.Mvc;

namespace Frontend.ViewComponents;

public record BreadcrumbItem(string Text, string? Url);

public class BreadcrumbViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var raw = ViewBag.Breadcrumb as (string text, string? url)[] ?? [];
        var items = raw.Select(x => new BreadcrumbItem(x.text, x.url)).ToArray();
        return View(items);
    }
}

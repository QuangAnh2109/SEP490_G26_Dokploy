using Microsoft.AspNetCore.Mvc;

namespace Frontend.ViewComponents;

public class PagerViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int current, int total, int pageSize)
    {
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        if (totalPages <= 1) return Content("");

        var model = new { Current = current, Total = total, PageSize = pageSize, TotalPages = totalPages };
        return View(model);
    }
}

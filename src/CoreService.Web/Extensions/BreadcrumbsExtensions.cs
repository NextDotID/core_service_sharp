namespace CoreService.Web.Extensions;

using MudBlazor;

public static class BreadcrumbsExtensions
{
    public static void SetItems(this List<BreadcrumbItem> breadcrumbs, params BreadcrumbItem[] items)
    {
        breadcrumbs.Clear();
        breadcrumbs.AddRange(items);
    }

    public static void AppendItem(this List<BreadcrumbItem> breadcrumbs, string text, string href, string? icon = null)
    {
        var last = breadcrumbs.LastOrDefault();
        if (last is not null)
        {
            // This makes URL jump lost its cascadence.
            var enabledLast = new BreadcrumbItem(last.Text, last.Href, false, last.Icon);
            breadcrumbs[^1] = enabledLast;
        }

        // Always append disabled item as it being the last one.
        breadcrumbs.Add(new BreadcrumbItem(text, href, true, icon));
    }
}

namespace NXM.Tensai.Back.OKR.Application;

public static class PaginatedListMapper
{
    public static PaginatedListResult<TDestination> ToApplicationPaginatedListResult<TSource, TDestination>(this PaginatedList<TSource> source, Func<TSource, TDestination> mapFunction)
    {
        var mappedItems = source.Items.Select(mapFunction).ToList();
        return new PaginatedListResult<TDestination>(
            mappedItems,
            source.Items.Count,
            source.PageIndex,
            source.TotalPages
        );
    }
}

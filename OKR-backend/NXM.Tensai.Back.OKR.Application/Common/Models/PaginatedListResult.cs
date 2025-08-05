namespace NXM.Tensai.Back.OKR.Application;

public class PaginatedListResult<T>
{
    public List<T> Items { get; private set; }
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public PaginatedListResult(List<T> items, int count, int pageIndex, int totalPages)
    {
        PageIndex = pageIndex;
        TotalPages = totalPages;

        Items = items;
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

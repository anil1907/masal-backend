namespace Core.Persistence.Paging;

public class Paginate<T> : IPaginate<T>
{
    public Paginate(IEnumerable<T> source, int index, int size, int from)
    {
        if (from > index)
        {
            throw new ArgumentException($"indexFrom: {from} > pageIndex: {index}, must indexFrom <= pageIndex");
        }

        Index = index;
        Size = size;
        From = from;
        Count = source.Count();
        Pages = (int)Math.Ceiling(Count / (double)Size);

        switch (source)
        {
            case IQueryable<T> source2:
                Items = source2.Skip((Index - From) * Size).Take(Size).ToList();
                break;
            case T[] array:
                Items = array.Skip((Index - From) * Size).Take(Size).ToList();
                break;
            default:
                var arraySource = source.ToArray();
                Items = arraySource.Skip((Index - From) * Size).Take(Size).ToList();
                break;
        }
    }

    public Paginate() => Items = Array.Empty<T>();

    public int From { get; set; }

    public int Index { get; set; }

    public int Size { get; set; }

    public int Count { get; set; }

    public int Pages { get; set; }

    public IList<T> Items { get; set; }

    public bool HasPrevious => Index - From > 0;

    public bool HasNext => Index - From + 1 < Pages;
}
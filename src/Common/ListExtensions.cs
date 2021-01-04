using System.Collections.Generic;

public static class ListExtensions
{
    public static T AddAndReturn<T>(this List<T> list, T item)
    {
        list.Add(item);
        return item;
    }
}

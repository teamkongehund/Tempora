using System;
using System.Collections.Generic;

namespace Tempora.Classes.DataHelpers;
public static class CloneUtility
{
    public static List<T> CloneList<T>(List<T> originalList) where T : ICloneable
    {
        List<T> clonedList = [];

        foreach (T item in originalList)
        {
            var clonedItem = (T)item.Clone();
            clonedList.Add(clonedItem);
        }

        return clonedList;
    }
}
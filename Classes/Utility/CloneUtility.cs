using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuTimer.Classes.Utility;
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

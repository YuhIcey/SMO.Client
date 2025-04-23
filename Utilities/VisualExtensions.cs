using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace SMOClient.Utilities
{
    public static class VisualExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
} 
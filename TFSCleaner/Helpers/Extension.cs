using System;
using System.Collections.Generic;
using System.Windows;

namespace SR.TFSCleaner.Helpers
{
    public static class ExtensionsMethods
    {
        public static void AddOnUi<T>(this ICollection<T> collection, T item)
        {
            Action<T> addMethod = collection.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }
    }
}

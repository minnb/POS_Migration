using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Helpers
{
    public static class LinqHelper
    {
        public static List<T> GroupItems<T>(List<T> items)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var keyProperties = properties.Where(p => p.PropertyType != typeof(double) && p.PropertyType != typeof(int)).ToList();
            var sumProperty = properties.FirstOrDefault(p => p.PropertyType == typeof(double) || p.PropertyType == typeof(int)) ?? throw new ArgumentException("No summable property found.");
            
            var groupedItems = items
                .GroupBy(item => string.Join(",", keyProperties.Select(p => p.GetValue(item)?.ToString())))
                .Select(group =>
                {
                    var newItem = Activator.CreateInstance<T>();
                    foreach (var prop in keyProperties)
                    {
                        var value = group.First().GetType().GetProperty(prop.Name).GetValue(group.First());
                        newItem.GetType().GetProperty(prop.Name).SetValue(newItem, value);
                    }
                    var sumValue = group.Sum(item => Convert.ToDouble(sumProperty.GetValue(item)));
                    newItem.GetType().GetProperty(sumProperty.Name).SetValue(newItem, sumValue);
                    return newItem;
                })
                .ToList();

            return groupedItems;
        }

    }
}

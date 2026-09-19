using Myra.Graphics2D.UI;
using System; 

namespace Myra.Utility
{
    internal static class CloneHelper
    {
        /// <summary>
        /// Creates a deep copy of this widget with all its properties and attached properties.
        /// </summary>
        /// <returns>A new widget instance that is a copy of this widget.</returns>
        public static T Clone<T>(this Widget original) where T : Widget, new()
        {
            // Firstly try to use parameterless constructor
            var result = new T();

            result.CopyFrom(original);

            // Copy attached properties
            foreach (var pair in original.AttachedPropertiesValues)
            {
                result.AttachedPropertiesValues[pair.Key] = pair.Value;
            }

            return result;
        }
    }
}

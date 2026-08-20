using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Equivalent to Myra.MonoGame.Markup.Binding
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// Value from the source is set on the target.
        /// </summary>
        OneWay,
        /// <summary>
        /// The value from the source is set on the target and vice versa.
        /// </summary>
        TwoWay,
        /// <summary>
        /// Value from the target is set on the source
        /// </summary>
        OneWayFromTarget,
        /// <summary>
        /// Value from the source is set on the target at start, then no longer updated.
        /// </summary>
        OneTime
    }
}

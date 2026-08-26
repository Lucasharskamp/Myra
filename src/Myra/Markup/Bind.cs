using System;

namespace Myra.Markup
{
    /// <summary>
    /// How a binding in <see cref="Bind"/> must be executed
    /// </summary>
    public enum BindingMode
    {

        /// <summary>
        /// Value from the source is set on the target at start, then no longer updated.
        /// </summary>
        OneTime,
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
        OneWayFromTarget
    }

    /// <summary>
    /// For binding code-behind properties to element properties using "x:Bind" in XAML
    /// </summary>
    public sealed class Bind
    {
        /// <summary>
        /// Default constructor for <see cref="Bind"/>
        /// </summary>
        public Bind(string path, BindingMode mode)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Binding path cannot be null or empty.", nameof(path));

            Path = path;
            Mode = mode;
        }

        /// <summary>
        /// Name of the code-behind property to reference
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// How to bind the code-behind property to the entity.
        /// </summary>
        public BindingMode Mode { get; }

        /// <inheritdoc/>
        public override string ToString()
            => Mode == BindingMode.OneWay
                ? $"{{Binding {Path}}}"
                : $"{{Binding {Path}, Mode={Mode}}}";
    }
}

using Mono.Cecil;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Types
{ 
    public sealed class MyraCecilEvent : IXamlEventInfo
    {
        private readonly EventDefinition _event;

        private readonly MyraCecilType _declaringType;


        public object Id =>
            _event;


        public string Name =>
            _event.Name;


        public IXamlType DeclaringType =>
            _declaringType;


        public IXamlMethod? Add =>
            _event.AddMethod == null
                ? null
                : new MyraCecilMethod(
                    _event.AddMethod,
                    _event.Resolve().EventType,
                    null);


        public EventDefinition EventDefinition =>
            _event;


        public MyraCecilEvent(
            EventDefinition @event,
            MyraCecilType declaringType)
        {
            _event = @event;
            _declaringType = declaringType;
        }


        public bool Equals(
            IXamlEventInfo? other)
        {
            return other is MyraCecilEvent e &&
                   e._event.FullName == _event.FullName;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlEventInfo other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _event.FullName.GetHashCode();
        }
    }
}

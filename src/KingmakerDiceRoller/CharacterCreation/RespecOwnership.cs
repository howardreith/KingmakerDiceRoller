using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Transient proof from a supported player selector and the exact native copy callback.
    // The original entity survives native PopulateObject; its descriptor must be reread afterwards.
    public sealed class RespecOwnership
    {
        private readonly Func<bool> ownerIsCurrent;
        private readonly Func<object> finalDescriptor;
        private readonly Action<object> refreshDerived;

        public RespecOwnership(object controller, object source, object originalEntity,
            object callback, string provider, Func<bool> ownerIsCurrent, Func<object> finalDescriptor, Action<object> refreshDerived = null)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            OriginalEntity = originalEntity ?? throw new ArgumentNullException(nameof(originalEntity));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.ownerIsCurrent = ownerIsCurrent ?? throw new ArgumentNullException(nameof(ownerIsCurrent));
            this.finalDescriptor = finalDescriptor ?? throw new ArgumentNullException(nameof(finalDescriptor));
            this.refreshDerived = refreshDerived;
        }

        public object Controller { get; }
        public object Source { get; }
        public object OriginalEntity { get; }
        public object Callback { get; }
        public string Provider { get; }
        public object CopyContext => Callback is Delegate ? ((Delegate)Callback).Target : Callback;
        public bool Closed { get; private set; }
        public bool IsCurrent { get { try { return !Closed && ownerIsCurrent(); } catch { return false; } } }
        public object ReadFinalDescriptor() => IsCurrent ? finalDescriptor() : null;
        public bool Owns(object controller, object source) => IsCurrent &&
            ReferenceEquals(Controller, controller) && ReferenceEquals(Source, source);
        public void RefreshDerivedStats(object state) { if (IsCurrent) refreshDerived?.Invoke(state); }
        public void Close() { Closed = true; }
    }
}

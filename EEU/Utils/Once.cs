using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace EEU.Utils;

public class Once {

    private class AlreadyInitializedException : Exception {
        public AlreadyInitializedException(): base("Cannot change initializer after initialization is complete.") {}
    }
    
    protected object locker = new();
    private volatile bool initialized;
    protected Action init;

   public Once(Action doIt) {
        init = doIt;
    }

    public static Once Create(Action doIt) {
        return new Once(doIt);
    }

    public void Init() {
        if (initialized) {
            return;
        }
        lock (locker) {
            // Repeated in case we didn't grab the lock first
            if (initialized) {
                return;
            }

            init();
            initialized = true;
        }
    }

    protected Action Initializer {
        get => init;
        set {
            if (Initialized) {
                throw new AlreadyInitializedException();
            }

            init = value;
        }
    }

    public bool Initialized => initialized;
}

public class Once<TElem>: Once {
    private TElem? elem;

    public Once(Func<TElem> a) : base(() => { }) {
        void RealAction() {
            elem = a();
        }
        Initializer = RealAction;
    }

    [MemberNotNull(nameof(elem))]
    public new void Init() {
        base.Init();
        Assert.NotNull(elem);
    }

    [MemberNotNullWhen(true, nameof(elem))]
    public new bool Initialized => base.Initialized;

    public TElem Elem {
        get {
            Init();
            return elem;
        }
    }

}

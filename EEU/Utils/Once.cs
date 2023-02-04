namespace EEU.Utils;

public class Once {
    private object locker = new();
    private bool initialized;
    private readonly Action init;

    public Once(Action doIt) {
        init = doIt;
    }

    public void Init() {
        lock (locker) {
            if (initialized) {
                return;
            }

            init();
            initialized = true;
        }
    }


}

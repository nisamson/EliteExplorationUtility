namespace EEU.Utils.Units;

public abstract class Memory {
    protected ulong NumBytes { get; set; }

    public class Bytes : Memory {
        public const ulong RequisiteBytes = 1;
    }

    public class KiBytes : Memory {
        public const ulong RequisiteBytes = 1024;
    }

    public class MiBytes : Memory {
        public const ulong RequisiteBytes = KiBytes.RequisiteBytes * 1024;
    }

    public class GiBytes : Memory {
        public const ulong RequisiteBytes = MiBytes.RequisiteBytes * 1024;
    }
}

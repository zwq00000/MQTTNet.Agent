using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace MQTTnet.Agent;

/// <summary>
/// Generates sequential Guids based on the MongoDB ObjectId specification only uses a 16 byte value in order to be Guid compatible.
/// The additional bytes are taken up by using a 64 bit time value rather than the 32 bit Unix epoch
/// </summary>
internal static class SequentialGuid {

    private static readonly byte[] StaticMachinePid;
    private static int _staticIncrement;

    private readonly static int pid;

    /// <summary>
    /// Static constructor initializes the three needed variables
    /// </summary>
    static SequentialGuid() {
        _staticIncrement = new Random().Next();
        StaticMachinePid = new byte[5];
        using (var algorithm = MD5.Create()) {
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
            // use first 3 bytes of hash
            for (var i = 0; i < 3; i++)
                StaticMachinePid[i] = hash[i];
        }

        try {
            pid = Process.GetCurrentProcess().Id;
            // use low order two bytes only
            StaticMachinePid[3] = (byte)(pid >> 8);
            StaticMachinePid[4] = (byte)pid;
        } catch (SecurityException) { }
    }

    /// <summary>
    /// 新建顺序Guid
    /// </summary>
    /// <returns></returns>
    public static Guid NewGuid() {
        return NewGuid(DateTime.Now);
    }

    /// <summary>
    /// 生成顺序 Guid
    /// 0000yyyy-MMdd-HHmm-xxxx-xxxxssssss
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    internal static Guid NewGuid(DateTime time) {
        // only use low order 3 bytes
        var increment = Interlocked.Increment(ref _staticIncrement) & 0x00ffffff;

        return new Guid(ToHexNum(time.Year),
            (short)(ToHexNum(time.Month) << 8 | ToHexNum(time.Day)),
            (short)(ToHexNum(time.Hour) << 8 | ToHexNum(time.Minute)),
            (byte)StaticMachinePid[0],
            (byte)StaticMachinePid[1],
            (byte)StaticMachinePid[2],
            (byte)StaticMachinePid[3],
            (byte)StaticMachinePid[4],
            (byte)(increment >> 16),
            (byte)(increment >> 8),
            (byte)increment
        );
    }

    /// <summary>
    /// 计算 16进制形式的十进制数值，如 toHexNum(2020)= 0x2020
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    private static int ToHexNum(int val) {
        return (val / 10000 % 10 << 16) | (val / 1000 % 10 << 12) | (val / 100 % 10 << 8) | (val / 10 % 10 << 4) | val % 10;
    }

    /// <summary>
    /// Gets a sequential Guid for a given tick value based on ObjectId spec
    /// </summary>
    /// <param name="timestamp">Should be the system Ticks value you wish to provide in your Guid</param>
    /// <returns>Guid</returns>
    internal static Guid NewGuid(long timestamp) {
        // only use low order 3 bytes
        var increment = Interlocked.Increment(ref _staticIncrement) & 0x00ffffff;

        var d = new byte[8];
        Array.Copy(StaticMachinePid, d, 5);
        d[5] = (byte)(increment >> 16);
        d[6] = (byte)(increment >> 8);
        d[7] = (byte)increment;
        return new Guid(
            (int)(timestamp >> 32),
            (short)(timestamp >> 16),
            (short)timestamp,
            d
        );
    }
}

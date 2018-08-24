using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using libPSARC.Interop;

namespace libPSARC {

    [ByteOrder( Endian.Big )]
    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0x01, Size = 0x05 )]
    public struct UInt40 {
        public byte high;
        public uint low;

        public ulong Value => (ulong) (high << 32) + low;

        public static implicit operator ulong( UInt40 tVal ) => tVal.Value;

        public static explicit operator UInt40( ulong val )
                => new UInt40 { high = (byte) ( ( val >> 32 ) & 0xFF ), low = (uint) val };

        public override string ToString() => ((ulong) this).ToString();

    }

}

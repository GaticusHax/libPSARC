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
        public Byte High;
        public UInt32 Low;

        public UInt64 Value => (UInt64) (High << 32) + Low;

        public static implicit operator UInt64( UInt40 tVal ) => tVal.Value;

        public static explicit operator UInt40( UInt64 val ) {
            return new UInt40 { High = (Byte) ((val >> 32) & 0xFF), Low = (UInt32) val };
        }

        public override string ToString() => ((UInt64) this).ToString();

    }

}

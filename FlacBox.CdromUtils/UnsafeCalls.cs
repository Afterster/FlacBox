using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Text;

namespace FlacBox.CdromUtils
{
    static class UnsafeCalls
    {
        [DllImport("Kernel32.dll")]
        internal extern static uint GetDriveType(string drive);

        internal const uint CdromDriveType = 5;
        internal const uint GenericRead = 0x80000000;
        internal const uint FileShareRead = 0x00000001;
        internal const uint OpenExisting = 3;
        
        internal const uint IoctlCdromRawRead = 0x0002403E;
        internal const uint IoctlCdromReadTocEx = 0x00024054;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
              string lpFileName,
              uint dwDesiredAccess,
              uint dwShareMode,
              IntPtr SecurityAttributes,
              uint dwCreationDisposition,
              uint dwFlagsAndAttributes,
              IntPtr hTemplateFile
              );

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint IoControlCode,
            [In] RawReadInfo InBuffer,
            uint nInBufferSize,
            [Out] byte[] OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr Overlapped
        );

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint IoControlCode,
            [In] ReadTocEx InBuffer,
            uint nInBufferSize,
            [Out] byte[] OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr Overlapped
        );

        internal static int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }

        internal static TocCdtextDataBlock GetTocCdtextDataBlock(byte[] buffer, int offset)
        {
            byte packType = buffer[offset];
            byte trackNumberAndExtension = buffer[offset + 1]; // 7 + 1
            byte sequenceNumber = buffer[offset + 2];
            byte positionAndBlockAndIsUnicode = buffer[offset + 3]; // 4 + 3 + 1
            ushort crc = BitConverter.ToUInt16(buffer, offset + 16);

            byte[] data = new byte[12];
            Array.Copy(buffer, offset + 4, data, 0, data.Length);

            TocCdtextDataBlock result = new TocCdtextDataBlock();
            result.PackType = (TocCdtextDataBlockPackType)packType;
            result.TrackNumber = trackNumberAndExtension & 0x7F;
            result.IsExtension = (trackNumberAndExtension & 0x80) != 0;
            result.SequenceNumber = sequenceNumber;
            result.CharacterPosition = positionAndBlockAndIsUnicode & 0x0F;
            result.BlockNumber = (positionAndBlockAndIsUnicode >> 4) & 0x07;
            result.IsUnicode = (positionAndBlockAndIsUnicode & 0x80) != 0;
            result.Data = data;
            result.Crc = crc;
            return result;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class RawReadInfo
        {
            internal long DiskOffset = 0;
            internal uint SectorCount = 0;
            internal uint TrackMode = CddaTrackModeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class ReadTocEx
        {
            internal byte FormatPlusMsf = CdtextReadTocExFormat;
            internal byte SessionTrack;
            internal byte Reserved2;
            internal byte Reserved3;
        }

        internal const uint RawReadInfoSize = 16;
        internal const uint ReadRawTocExSize = 4;
        internal const uint CddaTrackModeType = 2;
        internal const byte CdtextReadTocExFormat = 5;
        internal const int TocCdtextDataBlockSize = 18;

        internal enum TocCdtextDataBlockPackType : byte
        {
            None = 0,
            AlbumName = 0x80, 
            Performer = 0x81, 
            Songwriter = 0x82,
            Composer = 0x83,
            Arranger = 0x84,
            Messages = 0x85,
            DiscId = 0x86,
            Genre = 0x87,
            TocInfo = 0x88,
            TocInfo2 = 0x89,
            UpcEan = 0x8E,
            SizeInfo = 0x8F 
        }

        internal class TocCdtextDataBlock
        {
            internal TocCdtextDataBlockPackType PackType;
            internal int TrackNumber;
            internal bool IsExtension;
            internal int SequenceNumber;
            internal int CharacterPosition;
            internal int BlockNumber;
            internal bool IsUnicode;
            internal byte[] Data;
            internal ushort Crc;

            public string GetText()
            {
                if (IsUnicode)
                    return Encoding.Unicode.GetString(Data);
                else
                    return Encoding.ASCII.GetString(Data);
            }
        }
    }

}

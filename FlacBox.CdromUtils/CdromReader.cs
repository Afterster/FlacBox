using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Low level CD data reader.
    /// </summary>
    public sealed class CdromReader : IDisposable
    {
        public const int SectorSize = 2352;

        char driveLetter;
        SafeFileHandle hCd;

        public char DriveLetter
        {
            get { return driveLetter; }
        }

        /// <summary>
        /// Creates instance if the reader for assigned CD drive.
        /// </summary>
        /// <param name="driveLetter">CD drive letter</param>
        public CdromReader(char driveLetter)
        {
            if (!Char.IsUpper(driveLetter)) throw new ArgumentException("Invalid drive letter");
            uint driveType = UnsafeCalls.GetDriveType(driveLetter.ToString() + ":");
            if (driveType != UnsafeCalls.CdromDriveType)
                throw new CdromUtilsException("Drive is not a CDROM");

            this.driveLetter = driveLetter;

            hCd = UnsafeCalls.CreateFile(@"\\.\" + driveLetter.ToString() + ":", 
                UnsafeCalls.GenericRead, UnsafeCalls.FileShareRead,
                IntPtr.Zero, UnsafeCalls.OpenExisting, 0, IntPtr.Zero);

            if (hCd.IsInvalid)
                throw new Win32Exception(UnsafeCalls.GetLastError());
        }

        bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                hCd.Close();
                disposed = true;
            }
        }

        public void Close()
        {
            Dispose(true);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        const uint MaxSectorsToRead = 20;

        /// <summary>
        /// Reads WAVE data from CD sectors
        /// </summary>
        /// <param name="startSector">Start sector number</param>
        /// <param name="sectorCount">Number of sectors to read</param>
        /// <param name="buffer">Buffer</param>
        /// <returns>Number of bytes read</returns>
        public uint ReadSectors(uint startSector, uint sectorCount, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (sectorCount > MaxSectorsToRead) throw new ArgumentOutOfRangeException("sectorCount");

            UnsafeCalls.RawReadInfo rawReadInfo = new UnsafeCalls.RawReadInfo();
            rawReadInfo.DiskOffset = startSector * 2048L;
            rawReadInfo.SectorCount = sectorCount;

            uint read = 0;

            bool result = UnsafeCalls.DeviceIoControl(hCd, UnsafeCalls.IoctlCdromRawRead,
                rawReadInfo, UnsafeCalls.RawReadInfoSize, 
                buffer, (uint)buffer.Length, ref read, IntPtr.Zero);

            if (!result) throw new Win32Exception(UnsafeCalls.GetLastError());

            return read;
        }

        /// <summary>
        /// Reads CD Text from CD drive.
        /// </summary>
        /// <returns>CD Text</returns>
        public CdtextData ReadCdtext()
        {
            const int ReadTocExOutputMaxSize = 0xFFF0;
            UnsafeCalls.ReadTocEx readTocInput = new UnsafeCalls.ReadTocEx();
            bool result;
            byte[] buffer = new byte[ReadTocExOutputMaxSize];
            uint read = 0;
            result = UnsafeCalls.DeviceIoControl(hCd, UnsafeCalls.IoctlCdromReadTocEx, 
                readTocInput, UnsafeCalls.RawReadInfoSize,
                buffer, (uint)buffer.Length, ref read, IntPtr.Zero);
            if (!result) throw new Win32Exception(UnsafeCalls.GetLastError());

            Dictionary<UnsafeCalls.TocCdtextDataBlockPackType, StringBuilder> strings = new Dictionary<UnsafeCalls.TocCdtextDataBlockPackType, StringBuilder>();
            List<UnsafeCalls.TocCdtextDataBlock> blocks = new List<UnsafeCalls.TocCdtextDataBlock>();
            int position = 4; // skip length and reserved
            int lastPosition = 2 + BitConverter.ToUInt16(buffer, 0); 
            while (position < lastPosition)
            {
                UnsafeCalls.TocCdtextDataBlock block = UnsafeCalls.GetTocCdtextDataBlock(buffer, position);
                if (block.PackType == UnsafeCalls.TocCdtextDataBlockPackType.None) break; // emtpy space?

                blocks.Add(block);

                StringBuilder sb;
                if (!strings.TryGetValue(block.PackType, out sb))
                {
                    strings.Add(block.PackType, sb = new StringBuilder());
                }
                sb.Append(block.GetText());

                position += UnsafeCalls.TocCdtextDataBlockSize;
            }

            CdtextData cdtext = new CdtextData();
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.AlbumName))
                cdtext.AlbumNames = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.AlbumName]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Performer))
                cdtext.Performers = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Performer]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Songwriter))
                cdtext.Songwriters = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Songwriter]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Composer))
                cdtext.Composers = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Composer]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Arranger))
                cdtext.Arrangers = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Arranger]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Messages))
                cdtext.Messages = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Messages]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.Genre))
                cdtext.Genres = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.Genre]).ToString());
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.DiscId))
                cdtext.DiskId = StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.DiscId]).ToString();
            if (strings.ContainsKey(UnsafeCalls.TocCdtextDataBlockPackType.UpcEan))
                cdtext.UpcEans = SplitByZeros(StripTail(strings[UnsafeCalls.TocCdtextDataBlockPackType.UpcEan]).ToString());
            return cdtext; 
        }

        private static StringBuilder StripTail(StringBuilder sb)
        {
            while (sb.Length > 0 && sb[sb.Length - 1] == 0) sb.Length--;
            return sb;
        }

        private static string[] SplitByZeros(string s)
        {
            return s.Split('\x00');
        }
    }

    /// <summary>
    /// CD Text data structure grouped by types of information. Item 0 contains 
    /// disk level information.
    /// </summary>
    public class CdtextData
    {
        public string[] AlbumNames;
        public string[] Performers;
        public string[] Songwriters;
        public string[] Composers;
        public string[] Arrangers;
        public string[] Messages;
        public string[] Genres;
        public string[] UpcEans;
        public string DiskId;
    }
}

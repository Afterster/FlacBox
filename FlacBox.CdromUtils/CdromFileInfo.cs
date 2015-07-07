using System;
using System.IO;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Helper to retrive track information from CDA.
    /// 
    /// From http://www.fltvu.com/jiaocheng/chenxu1/FORMAT/sound/cda.htm
    /// </summary>
    public sealed class CdromFileInfo
    {
        const int CdaFileSize = 44;

        string path;

        public string Path
        {
            get { return path; }
        }

        int trackNumber;

        public int TrackNumber
        {
            get { return trackNumber; }
        }

        uint diskSerialNumber;

        public uint DiskSerialNumber
        {
            get { return diskSerialNumber; }
        }

        uint startSector;

        public uint StartSector
        {
            get { return startSector; }
        }

        uint sectorCount;

        public uint SectorCount
        {
            get { return sectorCount; }
        }

        TimeSpan startFrom;

        public TimeSpan StartFrom
        {
            get { return startFrom; }
        }

        TimeSpan duration;

        public TimeSpan Duration
        {
            get { return duration; }
        }

        public char DriveLetter
        {
            get
            {
                string fullPath = System.IO.Path.GetFullPath(Path);
                if (fullPath.Length < 2 && fullPath[1] != ':')
                    throw new CdromUtilsException("Invalid track location");
                return Char.ToUpperInvariant(fullPath[0]);
            }
        }

        /// <summary>
        /// Initialize data from Audio CD file.
        /// </summary>
        /// <param name="path">Path to CDA file.</param>
        public CdromFileInfo(string path)
        {
            if(path == null) throw new ArgumentNullException("path");
            this.path = path;

            ReadInfo();
        }

        static byte[] CdaStandardStart = {0x52, 0x49, 0x46, 0x46, 0x24, 0, 0, 0, 0x43, 0x44, 0x44, 0x41, 
            0x66, 0x6D, 0x74, 0x20, 0x18, 0, 0, 0 };

        private void ReadInfo()
        {
            if (!File.Exists(Path))
                throw new CdromUtilsException("File does not exist: " + Path);

            byte[] data = new byte[CdaFileSize];
            using (FileStream fs = File.OpenRead(Path))
            {
                if(fs.Length != CdaFileSize)
                    throw new CdromUtilsException("Invalid file size");

                int read = fs.Read(data, 0, CdaFileSize);
                if(read != CdaFileSize)
                    throw new CdromUtilsException("Cannot read all data");
            }

            for (int i = 0; i < CdaStandardStart.Length; i++)
            {
                if (CdaStandardStart[i] != data[i])
                    throw new CdromUtilsException("Invalid CDA file");
            }

            int dataOffset = CdaStandardStart.Length;
            const int SupportedDataVersion = 1;

            int version = BitConverter.ToUInt16(data, dataOffset + 0x00);
            if (version != SupportedDataVersion)
                throw new CdromUtilsException("Unsupported CDA file format version");

            this.trackNumber = BitConverter.ToUInt16(data, dataOffset + 0x02);
            this.diskSerialNumber = BitConverter.ToUInt32(data, dataOffset + 0x04);

            this.startSector = BitConverter.ToUInt32(data, dataOffset + 0x08);
            this.sectorCount = BitConverter.ToUInt32(data, dataOffset + 0x0C);

            this.startFrom = FromFSM(data, dataOffset + 0x10);
            this.duration = FromFSM(data, dataOffset + 0x14);
        }

        private static TimeSpan FromFSM(byte[] data, int offset)
        {
            byte frames = data[offset + 0x00];
            byte seconds = data[offset + 0x01];
            byte minutes = data[offset + 0x02];

            return new TimeSpan(0, 0, minutes, seconds, frames * 1000 / 75);
        }

        public CdromFileStream CreateStream()
        {
            return CreateStream(true);
        }

        public CdromFileStream CreateStream(bool includeWaveHeader)
        {
            return new CdromFileStream(this, includeWaveHeader);
        }
    }
}

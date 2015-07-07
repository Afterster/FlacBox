using System;
using System.Collections.Generic;
using System.IO;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Stream for reading binary PCM/WAVE from audio CD.
    /// </summary>
    public sealed class CdromFileStream : Stream
    {
        static ArraySegment<byte> NoData = new ArraySegment<byte>(new byte[0]);

        ArraySegment<byte> currentData = NoData;
        IEnumerator<ArraySegment<byte>> dataSource;
        long streamTotalLength;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public CdromFileStream(string cdaFilePath)
            : this(new CdromFileInfo(cdaFilePath), true)
        {
        }

        public CdromFileStream(CdromFileInfo fileInfo)
            : this(fileInfo, true)
        {
        }

        public CdromFileStream(CdromFileInfo fileInfo, bool includeWaveHeader)
        {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");

            CdromReader reader = new CdromReader(fileInfo.DriveLetter);
            try
            {
                this.dataSource = ReadData(reader,
                    fileInfo.StartSector, fileInfo.SectorCount, includeWaveHeader);

                InitializeData();
            }
            catch
            {
                reader.Close();
                throw;
            }
        }

        private void InitializeData()
        {
            dataSource.MoveNext();
            currentData = dataSource.Current;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { return streamTotalLength; }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private IEnumerator<ArraySegment<byte>> ReadData(CdromReader reader, uint diskOffset, uint sectorCount, bool includeRiffHeader)
        {
            try
            {
                uint totalBytes = (uint)CdromReader.SectorSize * sectorCount;

                if (includeRiffHeader)
                {
                    const uint RiffHeaderSize = 0x28;
                    const uint RiffHeaderSizeWithoutSize = 0x24;

                    streamTotalLength = totalBytes + RiffHeaderSize;

                    byte[] waveHeader = new byte[] {
                        0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 
                        0x66, 0x6d, 0x74, 0x20, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00,
                        0x44, 0xAC, 0x00, 0x00, 0x10, 0xB1, 0x02, 0x00, 0x04, 0x00, 0x10, 0x00, 
                        0x64, 0x61, 0x74, 0x61, 0x00, 0x00, 0x00, 0x00 };

                    Array.Copy(BitConverter.GetBytes(totalBytes + RiffHeaderSizeWithoutSize), 0, waveHeader, 0x04, 4);
                    Array.Copy(BitConverter.GetBytes(totalBytes), 0, waveHeader, 0x28, 4);

                    yield return new ArraySegment<byte>(waveHeader);
                }
                else
                    streamTotalLength = totalBytes;

                const uint BufferSizeInSectors = 20;
                const int BufferSize = (int)BufferSizeInSectors * CdromReader.SectorSize;
                byte[] buffer = new byte[BufferSize];
                uint position = 0;
                while (position + BufferSizeInSectors < sectorCount)
                {
                    reader.ReadSectors(diskOffset + position, BufferSizeInSectors, buffer);
                    yield return new ArraySegment<byte>(buffer);
                    position += BufferSizeInSectors;
                }

                uint tail = sectorCount - position;
                reader.ReadSectors(diskOffset + position, tail, buffer);
                yield return new ArraySegment<byte>(buffer, 0, (int)tail * CdromReader.SectorSize);
            }
            finally
            {
                reader.Close();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");

            if (count <= currentData.Count)
            {
                Array.Copy(currentData.Array, currentData.Offset, buffer, offset, count);
                currentData = new ArraySegment<byte>(currentData.Array, currentData.Offset + count, currentData.Count - count);
                return count;
            }
            else
            {
                Array.Copy(currentData.Array, currentData.Offset, buffer, offset, currentData.Count);
                int position = currentData.Count;
                do
                {
                    if (dataSource.MoveNext())
                    {
                        currentData = dataSource.Current;

                        int tail = count - position;
                        if (tail <= currentData.Count)
                        {
                            Array.Copy(currentData.Array, currentData.Offset, buffer, offset + position, tail);
                            position = count;
                            currentData = new ArraySegment<byte>(currentData.Array, currentData.Offset + tail, currentData.Count - tail);
                        }
                        else
                        {
                            Array.Copy(currentData.Array, currentData.Offset, buffer, offset + position, currentData.Count);
                            position += currentData.Count;
                            currentData = NoData;
                        }
                    }
                    else
                    {
                        currentData = NoData;
                        break;
                    }
                } while (position < count);
                return position;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                dataSource.Dispose();
                dataSource = null;
                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

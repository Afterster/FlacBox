using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Utilities for reading of CD information.
    /// </summary>
    public static class CdromUtils
    {
        public static bool IsDriveAudioCd(char driveLetter)
        {
            if (UnsafeCalls.GetDriveType(driveLetter.ToString() + ":") != UnsafeCalls.CdromDriveType)
                return false;

            try
            {
                bool hasTracks = Directory.GetFiles(driveLetter.ToString() + ":\\", TrackCdaFiles).Length > 0;

                return hasTracks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return false;
            }
        }

        public static CdtextData ReadCdtextData(char driveLetter)
        {
            using (CdromReader reader = new CdromReader(driveLetter))
            {
                return reader.ReadCdtext();
            }
        }

        public static AudioCDInformation ReadAudioCDInformation(char driveLetter)
        {
            CdromFileInfo[] files = ReadAllTracks(driveLetter);
            CdtextData cdtext = ReadCdtextData(driveLetter);

            CdromFileInfo firstFile = files[0];
            CdromFileInfo lastFile = files[files.Length - 1];

            AudioCDInformation audioCd = new AudioCDInformation();
            audioCd.CDCatalogNumber = cdtext.DiskId;
            audioCd.CdaDiskSerialNumber = files[0].DiskSerialNumber;
            audioCd.CddbDiskId = GetCddbDiskId(files);
            audioCd.TrackCount = files.Length;
            audioCd.TotalDuration = (lastFile.StartFrom + lastFile.Duration).TotalSeconds;
            audioCd.Metadata = CreateMetadata(cdtext, 0);

            audioCd.Tracks = new AudioTrackInformation[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                AudioTrackInformation track = new AudioTrackInformation();
                track.TrackNumber = i + 1;
                track.Filename = Path.GetFileName(files[i].Path);
                track.Metadata = CreateMetadata(cdtext, i + 1);
                track.StartFrom = files[i].StartFrom.TotalSeconds;
                track.Duration = files[i].Duration.TotalSeconds;
                track.StartSector = files[i].StartSector;
                track.SectorCount = files[i].SectorCount;
                track.Metadata = CreateMetadata(cdtext, i + 1);
                
                audioCd.Tracks[i] = track;
            }

            return audioCd;
        }

        private static AudioMaterialMetadata CreateMetadata(CdtextData cdtext, int itemIndex)
        {
            if (cdtext == null) return null;

            AudioMaterialMetadata metadata = new AudioMaterialMetadata();
            if (cdtext.AlbumNames != null && itemIndex < cdtext.AlbumNames.Length)
                metadata.AlbumName = cdtext.AlbumNames[itemIndex];
            if (cdtext.Performers != null && itemIndex < cdtext.Performers.Length)
                metadata.Performer = cdtext.Performers[itemIndex];
            if (cdtext.Songwriters != null && itemIndex < cdtext.Songwriters.Length)
                metadata.Songwriter = cdtext.Songwriters[itemIndex];
            if (cdtext.Composers != null && itemIndex < cdtext.Composers.Length)
                metadata.Composer = cdtext.Composers[itemIndex];
            if (cdtext.Arrangers != null && itemIndex < cdtext.Arrangers.Length)
                metadata.Arranger = cdtext.Arrangers[itemIndex];
            if (cdtext.Messages != null && itemIndex < cdtext.Messages.Length)
                metadata.Message = cdtext.Messages[itemIndex];
            if (cdtext.Genres != null && itemIndex < cdtext.Genres.Length)
                metadata.Genre = cdtext.Genres[itemIndex];
            if (cdtext.UpcEans != null && itemIndex < cdtext.UpcEans.Length)
                metadata.UpcEan = cdtext.UpcEans[itemIndex];
            return metadata;
        }

        private static uint GetCddbDiskId(CdromFileInfo[] tracks)
        {
            int totalSeconds = 0;
            uint offsetsSum = 0;
            for (int i = 0; i < tracks.Length; i++)
            {
                uint startSeconds = (uint)tracks[i].StartFrom.TotalSeconds;
                while (startSeconds > 0)
                {
                    offsetsSum += startSeconds % 10;
                    startSeconds /= 10;
                }
                totalSeconds += (int)Math.Round( tracks[i].Duration.TotalSeconds );
            }

            uint diskId = ((uint)(offsetsSum & 0xFF)) << 24 | (uint)totalSeconds << 8 | (uint)tracks.Length;
            return diskId;
        }

        const string TrackCdaFiles = "track??.cda";

        public static CdromFileInfo[] ReadAllTracks(char driveLetter)
        {
            Dictionary<int, CdromFileInfo> tracks = new Dictionary<int, CdromFileInfo>();

            foreach (FileInfo fi in new DirectoryInfo(driveLetter.ToString() + ":\\").GetFiles(TrackCdaFiles))
            {
                int trackNumber = Int32.Parse(fi.Name.Substring(5, 2));
                CdromFileInfo cdfi = new CdromFileInfo(fi.FullName);
                tracks.Add(trackNumber, cdfi);
            }

            List<CdromFileInfo> orderedTracks = new List<CdromFileInfo>();
            int i = 1;
            while (tracks.ContainsKey(i))
            {
                orderedTracks.Add(tracks[i]);
                i++;
            }
            if (orderedTracks.Count != tracks.Count)
                throw new CdromUtilsException("Invalid CD content");

            return orderedTracks.ToArray();
        }
    }
}

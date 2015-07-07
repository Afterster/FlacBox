using System;
using System.Collections.Generic;
using System.Text;

namespace FlacBox.CdromUtils
{
    /// <summary>
    /// Provides CD information.
    /// </summary>
    [Serializable]
    public class AudioCDInformation
    {
        public string CDCatalogNumber;
        public uint CdaDiskSerialNumber;
        public uint CddbDiskId;

        public double TotalDuration;
        public AudioMaterialMetadata Metadata;

        public int TrackCount;
        public AudioTrackInformation[] Tracks;
    }

    /// <summary>
    /// Provides CD track information.
    /// </summary>
    [Serializable]
    public class AudioTrackInformation
    {
        public int TrackNumber;
        public string Filename;

        public AudioMaterialMetadata Metadata;

        public double StartFrom;
        public double Duration;

        public uint StartSector;
        public uint SectorCount;
    }

    /// <summary>
    /// Misc CD metadata.
    /// </summary>
    [Serializable]
    public class AudioMaterialMetadata
    {
        public string AlbumName;
        public string Performer;
        public string Songwriter;
        public string Composer;
        public string Arranger;
        public string Message;
        public string Genre;
        public string UpcEan;
    }
}

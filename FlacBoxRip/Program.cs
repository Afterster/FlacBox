using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

using FlacBox;
using FlacBox.CdromUtils;

namespace FlacBoxRip
{
    class Program
    {
        enum ConversionType
        {
            Flac,
            Wave,
            Ogg
        }

        static bool overwriteFolder = false;
        static bool ignoreCdinfo = false;
        static ConversionType conversion = ConversionType.Flac;
        static string drive = null;
        static string outputPath = null;

        static void Main(string[] args)
        {
            string[] argsWithoutOptions = FilterOptions(args);

            if (argsWithoutOptions.Length < 2)
            {
                PrintUsage();
                return;
            }

            drive = argsWithoutOptions[0];
            outputPath = argsWithoutOptions[1];
            try
            {
                ValidateDrive();

                bool outputValidated = CheckOutputPath();
                if (outputValidated)
                {
                    CopyCd();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.ToString());
            }
        }

        const string MetadataFileName = "cdinfo.xml";

        private static void CopyCd()
        {
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            char driveLetter = Char.ToUpperInvariant(drive[0]);
            if (!ignoreCdinfo)
            {
                Console.WriteLine("Retriving CD information");
                AudioCDInformation cdInformation = CdromUtils.ReadAudioCDInformation(driveLetter);
                Console.WriteLine("Saving CD information to " + MetadataFileName);
                SaveAudioCDInformation(Path.Combine(outputPath, MetadataFileName), cdInformation);
            }

            CdromFileInfo[] tracks = CdromUtils.ReadAllTracks(driveLetter);
            foreach (CdromFileInfo trackFileInfo in tracks)
            {
                try
                {
                    string outputFile = Path.Combine(outputPath, Path.GetFileName(trackFileInfo.Path));
                    CopyTrack(trackFileInfo, outputFile);
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine("ERROR: " + ex.Message);
                }
            }
        }

        private static void CopyTrack(CdromFileInfo trackFileInfo, string path)
        {
            Console.Write("Copying track {0}: ", Path.GetFileName(trackFileInfo.Path));
            using (Stream sourceStream = trackFileInfo.CreateStream())
            {
                using (Stream outputStream = CreateOutputStream(path))
                {
                    const int BufferSize = 0xFE00;
                    const int ProgressItemSize = 1000000;
                    byte[] buffer = new byte[BufferSize];
                    int reportedProgress = 0;
                    long copiedBytes = 0;
                    int read;
                    while ((read = sourceStream.Read(buffer, 0, BufferSize)) > 0)
                    {
                        outputStream.Write(buffer, 0, read);

                        copiedBytes += read;
                        int currentProgress = (int)(copiedBytes / ProgressItemSize);
                        while (currentProgress > reportedProgress)
                        {
                            Console.Write(".");
                            reportedProgress++;
                        }
                    }
                }
            }
            Console.WriteLine();
        }

        private static Stream CreateOutputStream(string path)
        {
            switch (conversion)
            {
                case ConversionType.Wave:
                    path = Path.ChangeExtension(path, ".wav");
                    return File.Create(path);
                case ConversionType.Ogg:
                    path = Path.ChangeExtension(path, ".ogg");
                    WaveOverFlacStream flacStream = new WaveOverFlacStream(File.Create(path), WaveOverFlacStreamMode.Encode);
                    try
                    {
                        return new FlacOverOggStream(flacStream, FlacOverOggStreamMode.Encode);
                    }
                    catch
                    {
                        flacStream.Dispose();
                        throw;
                    }                    
                case ConversionType.Flac:
                default:
                    path = Path.ChangeExtension(path, ".flac");
                    return new WaveOverFlacStream(File.Create(path), WaveOverFlacStreamMode.Encode, true);
            }
        }

        private static void SaveAudioCDInformation(string path, AudioCDInformation cdInformation)
        {
            XmlSerializer ser = new XmlSerializer(typeof(AudioCDInformation));
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;            
            using (XmlWriter writer = XmlWriter.Create(path, writerSettings))
            {
                ser.Serialize(writer, cdInformation);
            }
        }

        private static bool CheckOutputPath()
        {
            if (Directory.Exists(outputPath) && Directory.GetFiles(outputPath).Length > 0)
            {
                if (overwriteFolder)
                    return true;
                else
                {
                    Console.WriteLine("Output path '{0}' is exist.", outputPath);
                    Console.Write("Do you want to overwrite its content [y/N]? ");
                    string answer = Console.ReadLine();
                    return answer.ToLowerInvariant() == "y";
                }
            }
            else
                return true;
        }

        private static void ValidateDrive()
        {
            if (drive.Length == 0 || !Char.IsLetter(drive[0])
                || drive.Length > 2)
                throw new ApplicationException("Invalid drive letter: " + drive);

            if(!CdromUtils.IsDriveAudioCd(drive[0]))
                throw new ApplicationException("Drive has no audio CD");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("USAGE: FlacBoxRip.exe [-options] <cd-drive> <output-folder>");
            Console.WriteLine("Options:");
            Console.WriteLine("  -wave - save it as WAVE");
            Console.WriteLine("  -ogg - save it as OGG");
            Console.WriteLine("  -flac - save it as FLAC (default)");
            Console.WriteLine("  -overwrite - overwrites content of the output folder");
            Console.WriteLine("  -nocdinfo - skip cdinfo.xml");
            Console.WriteLine();
        }

        static string[] FilterOptions(string[] args)
        {
            List<string> argsWithoutOptions = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    switch (args[i].ToLowerInvariant())
                    {
                        case "-nocdinfo":
                            ignoreCdinfo = true;
                            break;
                        case "-wave":
                            conversion = ConversionType.Wave;
                            break;
                        case "-ogg":
                            conversion = ConversionType.Ogg;
                            break;
                        case "-flac":
                            conversion = ConversionType.Flac;
                            break;
                        case "-overwrite":
                            overwriteFolder = true;
                            break;
                        default:
                            Console.Error.WriteLine("Invalid option: " + args[i]);
                            break;
                    }
                }
                else
                {
                    argsWithoutOptions.Add(args[i]);
                }
            }
            return argsWithoutOptions.ToArray();
        }
    }
    
}

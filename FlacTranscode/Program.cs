using System;
using System.Collections.Generic;
using System.IO;

using FlacBox;

namespace FlacTranscode
{
    class Program
    {
        private static void PrintUsage()
        {
            Console.WriteLine("FlacTranscode.exe <mode> <input-file> <output-file>");
            Console.WriteLine("Modes:");
            Console.WriteLine("    -wave2flac");
            Console.WriteLine("    -wave2ogg");
            Console.WriteLine("    -flac2wave");
            Console.WriteLine("    -flac2wave16");
            Console.WriteLine("    -ogg2wave");
        }

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage(); return;
            }

            string mode = args[0].ToLowerInvariant();
            string inputFile = args[1];
            string outputFile = args[2];

            Stream inputStream = null;
            Stream outputStream = null;
            try
            {
                switch (mode)
                {
                    case "-wave2flac":
                        inputStream = File.OpenRead(inputFile);
                        outputStream = new WaveOverFlacStream(
                            File.Create(outputFile), WaveOverFlacStreamMode.Encode);
                        break;
                    case "-wave2ogg":
                        inputStream = File.OpenRead(inputFile);
                        outputStream = new FlacOverOggStream(
                            new WaveOverFlacStream(File.Create(outputFile), WaveOverFlacStreamMode.Encode),
                            FlacOverOggStreamMode.Encode);
                        break;
                    case "-flac2wave":
                        inputStream = new WaveOverFlacStream(
                            File.OpenRead(inputFile), WaveOverFlacStreamMode.Decode);
                        outputStream = File.Create(outputFile);
                        break;
                    case "-flac2wave16":
                        inputStream = new Wave16OverFlacStream(File.OpenRead(inputFile));
                        outputStream = File.Create(outputFile);
                        break;
                    case "-ogg2wave":
                        inputStream = new FlacOverOggStream(
                            new WaveOverFlacStream(File.OpenRead(inputFile), WaveOverFlacStreamMode.Decode),
                            FlacOverOggStreamMode.Decode);
                        outputStream = File.Create(outputFile);
                        break;
                    default:
                        throw new ApplicationException("Unknown transcode option: " + mode);
                }

                Console.WriteLine("Copying data from '{0}' to '{1}'.",
                    inputFile, outputFile);

                CopyStreams(inputStream, outputStream);

                Console.WriteLine();
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            finally
            {
                if (inputStream != null) inputStream.Close();
                if (outputStream != null) outputStream.Close();
            }
        }

        private static void CopyStreams(Stream s, Stream t)
        {
            byte[] buffer = new byte[0x1000];
            int read = s.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                t.Write(buffer, 0, read);
                read = s.Read(buffer, 0, buffer.Length);
                Console.Write('.');
            }
        }
    }
}

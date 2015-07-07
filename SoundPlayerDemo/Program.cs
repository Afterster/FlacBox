using System;
using System.Media;
using System.IO;

using FlacBox;

namespace SoundPlayerDemo
{
    class Program
    {        
        // downloaded and transcoded from http://www.soundsnap.com/node/85102
        // wave file size = 1,411,244, flac file size = 551,948
        // no quality loss and compression ratio is ~39%
        const string DemoFilePath = @"85102.flac"; 

        static void Main(string[] args)
        {
            string filePath = DemoFilePath;
            if(args.Length > 0) filePath = args[0];

            WaveOverFlacStream flacStream = new WaveOverFlacStream(File.OpenRead(filePath), WaveOverFlacStreamMode.Decode);
            SoundPlayer player = new SoundPlayer(flacStream);
            player.Play();

            Console.WriteLine("Demo sound is playing... ({0})", filePath);
            Console.WriteLine("Press ENTER to exit application");
            Console.ReadLine();
        }
    }
}

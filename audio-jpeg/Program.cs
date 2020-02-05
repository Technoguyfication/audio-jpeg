﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NAudio.Wave;

namespace audio_jpeg
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Write("Path to audio file: ");
			string audioFileInPath = Console.ReadLine();

			Bitmap audioBitmap;

			// open audio file and begin reading
			using (AudioFileReader reader = new AudioFileReader(audioFileInPath))
			{
				// read entire audio into buffer
				float[] buffer = new float[reader.Length / 4];
				int samplesRead = 0;
				while (samplesRead < buffer.Length)
				{
					samplesRead += reader.Read(buffer, 0, buffer.Length - samplesRead);
				}

				// get max peak value for normalization
				float peak = 0f;
				foreach (var sample in buffer)
				{
					peak = Math.Max(peak, Math.Abs(sample));
				}

				// fill bitmap with data
				const int bitmapWidth = 1024;
				audioBitmap = new Bitmap(bitmapWidth, (int)Math.Ceiling(buffer.Length / (double)bitmapWidth));
				for (int i = 0; i < buffer.Length; i++)
				{
					int sampleValue = (int)Math.Abs(255 * (buffer[i] / peak));
					audioBitmap.SetPixel(i % bitmapWidth, i / bitmapWidth, Color.FromArgb(sampleValue, sampleValue, sampleValue));
				}
			}


			// open output image file
			using (var outputStream = File.OpenWrite(audioFileInPath + ".png"))
			{
				audioBitmap.Save(outputStream, ImageFormat.Png);

				Console.WriteLine("Audio file wrote to: " + audioFileInPath + ".png");

				outputStream.Flush();
			}

		}
	}
}
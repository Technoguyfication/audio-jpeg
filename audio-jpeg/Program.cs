using System;
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
				const uint argbMask = 0xFFFFFF;
				audioBitmap = new Bitmap(bitmapWidth, (int)Math.Ceiling(buffer.Length / (double)bitmapWidth));
				for (int i = 0; i < buffer.Length; i++)
				{
					// use RGB channels to store float sample as 24-bit int
					uint colorArgb = (uint)(argbMask * ((buffer[i] / peak / 2f) + 0.5f));
					Color pixelColor = Color.FromArgb((int)(colorArgb | 0xFF000000));	// set alpha bits to full
					audioBitmap.SetPixel(i % bitmapWidth, i / bitmapWidth, pixelColor);
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

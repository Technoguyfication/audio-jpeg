using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NAudio.Wave;

namespace audio_jpeg
{
	class Program
	{
		const int BitmapWidth = 1024;
		const uint ArgbMask = 0x00FFFFFF;

		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				var exeName = AppDomain.CurrentDomain.FriendlyName;
				Console.WriteLine("Usage:\n" +
					$" {exeName} image (audio file)\n" +
					$" {exeName} audio (image file)");
				return;
			}

			string filePath = args[1];

			switch (args[0].ToLower())
			{
				case "image":
					ImageFile(filePath);
					break;
				case "audio":
					AudioFile(filePath);
					break;
				default:
					Console.WriteLine("Invalid option");
					break;
			}
		}
		private static void ImageFile(string filePath)
		{
			Bitmap audioBitmap;
			WaveFormat audioFormat;

			// open audio file and read data into bitmap
			using (AudioFileReader reader = new AudioFileReader(filePath))
			{
				audioFormat = reader.WaveFormat;

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
				audioBitmap = new Bitmap(BitmapWidth, (int)Math.Ceiling(buffer.Length / (double)BitmapWidth));
				for (long i = 0; i < buffer.Length; i++)
				{
					// use RGB channels to store float sample as 24-bit int
					uint colorArgb = (uint)(ArgbMask * ((buffer[i] / peak / 2f) + 0.5f));
					Color pixelColor = Color.FromArgb((int)(colorArgb | 0xFF000000));   // set alpha bits to full
					audioBitmap.SetPixel((int)(i % BitmapWidth), (int)(i / BitmapWidth), pixelColor);
				}

				// set unused pixels to empty
				Color zeroColor;
				unchecked
				{
					zeroColor = Color.FromArgb((int)4286578687);
				}
				for (long i = buffer.Length; i % BitmapWidth > 0; i++)
				{
					audioBitmap.SetPixel((int)(i % BitmapWidth), (int)(i / BitmapWidth), zeroColor);
				}
			}

			// save image file
			using (var outputStream = File.OpenWrite($"{filePath}.png"))
			{
				audioBitmap.Save(outputStream, ImageFormat.Png);
				Console.WriteLine($"Audio file wrote to: {filePath}.png");
				outputStream.Flush();
			}

			// save serialized wave format
			using (var waveFormatOut = File.OpenWrite($"{filePath}.png.waveFormat"))
			{
				audioFormat.Serialize(new BinaryWriter(waveFormatOut));
				waveFormatOut.Flush();
			}
		}

		private static void AudioFile(string filePath)
		{
			// read wave format
			WaveFormat waveFormat;
			try
			{
				using (var waveFormatReader = File.OpenRead($"{filePath}.waveFormat"))
				{
					waveFormat = new WaveFormat(new BinaryReader(waveFormatReader));
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}

			// read bitmap back into audio file
			using (var audioFileOut = File.OpenWrite($"{filePath}.wav"))
			{
				// open writer for wave file
				using (WaveFileWriter writer = new WaveFileWriter(audioFileOut, waveFormat))
				{
					Bitmap inBitmap = new Bitmap(filePath);
					// read pixels back into samples
					float[] buffer = new float[inBitmap.Width * inBitmap.Height];
					for (long i = 0; i < buffer.Length; i++)
					{
						var color = inBitmap.GetPixel((int)(i % BitmapWidth), (int)(i / BitmapWidth));
						var argb = (uint)(color.ToArgb()) & ArgbMask;
						buffer[i] = (((float)argb / ArgbMask) - 0.5f) * 2;
					}

					// write to file
					writer.WriteSamples(buffer, 0, buffer.Length);
					writer.Flush();
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Tempora.Classes.DataHelpers;
public class SpectrogramHelper
{
    public static Godot.Image ConvertBitmapToGodotImage(System.Drawing.Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Lock the bitmap's data
        BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb); // Ensure 32-bit ARGB format

        // Prepare buffer
        int bufferSize = bmpData.Stride * bmpData.Height;
        byte[] buffer = new byte[bufferSize];

        // Copy bitmap data to buffer
        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, buffer, 0, bufferSize);

        // Unlock bitmap
        bitmap.UnlockBits(bmpData);

        // Convert ARGB to RGBA (Godot uses RGBA order)
        for (int i = 0; i < bufferSize; i += 4)
        {
            byte a = buffer[i + 3];  // Alpha
            byte r = buffer[i + 2];  // Red
            byte g = buffer[i + 1];  // Green
            byte b = buffer[i];      // Blue

            buffer[i] = r;
            buffer[i + 1] = g;
            buffer[i + 2] = b;
            buffer[i + 3] = a;
        }

        // Create a Godot Image and populate it with the data
        Godot.Image gdImage = Godot.Image.CreateFromData(width, height, false, Godot.Image.Format.Rgba8, buffer);

        return gdImage;
    }

    public static ImageTexture ConvertBitmapToImageTextureNew(System.Drawing.Bitmap bitmap)
    {
        Godot.Image gdImage = ConvertBitmapToGodotImage(bitmap);
        ImageTexture texture = ImageTexture.CreateFromImage(gdImage);
        return texture;
    }
}

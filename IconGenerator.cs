using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Program {
    public static void Main(string[] args) {
        string inputPath = @"C:\Users\r1otp\Downloads\b2772fdc-8746-42d4-a016-6926add54e8d_removalai_preview.png";
        string outputPath = @"d:\steam-presence-1.12.3\SteamPresenceUI\Assets\appicon.ico";

        using (Bitmap source = (Bitmap)Image.FromFile(inputPath)) {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create)) {
                // ICO Header
                fs.Write(new byte[] { 0, 0, 1, 0, 4, 0 }, 0, 6); // 4 sizes: 16, 32, 48, 256

                int[] sizes = { 16, 32, 48, 256 };
                long dataOffset = 6 + (16 * sizes.Length);
                byte[][] allIconData = new byte[sizes.Length][];

                for (int i = 0; i < sizes.Length; i++) {
                    int s = sizes[i];
                    using (Bitmap resized = new Bitmap(s, s)) {
                        using (Graphics g = Graphics.FromImage(resized)) {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(source, 0, 0, s, s);
                        }
                        
                        // For 256px, use PNG encoding (modern standard)
                        if (s == 256) {
                            using (MemoryStream ms = new MemoryStream()) {
                                resized.Save(ms, ImageFormat.Png);
                                allIconData[i] = ms.ToArray();
                            }
                        } else {
                            // For smaller sizes, use raw BMP (safer for old GDI)
                            allIconData[i] = CreateRawBmpData(resized);
                        }

                        // Directory Entry
                        fs.WriteByte((byte)(s >= 256 ? 0 : s)); // Width
                        fs.WriteByte((byte)(s >= 256 ? 0 : s)); // Height
                        fs.WriteByte(0); // Colors
                        fs.WriteByte(0); // Reserved
                        fs.Write(new byte[] { 1, 0 }, 0, 2); // Planes
                        fs.Write(new byte[] { 32, 0 }, 0, 2); // BPP
                        
                        byte[] sizeBytes = BitConverter.GetBytes(allIconData[i].Length);
                        fs.Write(sizeBytes, 0, 4);
                        
                        byte[] offsetBytes = BitConverter.GetBytes((int)dataOffset);
                        fs.Write(offsetBytes, 0, 4);
                        
                        dataOffset += allIconData[i].Length;
                    }
                }

                // Write actual data
                foreach (byte[] data in allIconData) {
                    fs.Write(data, 0, data.Length);
                }
            }
        }
    }

    private static byte[] CreateRawBmpData(Bitmap bmp) {
        // ICO BMP format is slightly different (BITMAPINFOHEADER + XOR + AND)
        // For simplicity and 32-bit ARGB, we can often just wrap a standard BMP without a header, 
        // but the standard way is BITMAPINFOHEADER with height doubled.
        using (MemoryStream ms = new MemoryStream()) {
            BinaryWriter bw = new BinaryWriter(ms);
            
            // BITMAPINFOHEADER (partially)
            bw.Write(40); // Size of header
            bw.Write(bmp.Width);
            bw.Write(bmp.Height * 2); // Height doubled (XOR + AND)
            bw.Write((short)1); // Planes
            bw.Write((short)32); // BPP
            bw.Write(0); // Compression (BI_RGB)
            bw.Write(0); // Size of image
            bw.Write(0); // Xppm
            bw.Write(0); // Yppm
            bw.Write(0); // ClrUsed
            bw.Write(0); // ClrImportant

            // XOR Mask (32-bit ARGB)
            // Windows BMP is bottom-up. Standard System.Drawing.Bitmap.LockBits is better here.
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[data.Stride * data.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            // BMP pixels are bottom-up, LockBits is usually top-down or matches source. 
            // In C#, it's usually top-down. We need to flip it for Win32 ICO BMP.
            int stride = data.Stride;
            for (int y = bmp.Height - 1; y >= 0; y--) {
                bw.Write(pixels, y * stride, stride);
            }

            // AND Mask (1-bit transparency) - Required for Win32 ICO even if using Alpha
            // 32x32 -> 32/8 = 4 bytes per row.
            int andStride = ((bmp.Width + 31) & ~31) / 8;
            byte[] andMask = new byte[andStride * bmp.Height]; // Defaults to 0 (fully opaque)
            bw.Write(andMask, 0, andMask.Length);
            
            return ms.ToArray();
        }
    }
}

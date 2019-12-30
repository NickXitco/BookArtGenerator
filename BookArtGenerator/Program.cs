using System;
using System.Drawing;
using System.IO;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Element;
using iText.Layout.Properties;
using Color = System.Drawing.Color;
using Document = iText.Layout.Document;

namespace BookArtGenerator
{
    internal static class Program
    {
        private const string Dest = "../../../juno.pdf";
        private const int Width = 18 * Inch;
        private const int Height = 24 * Inch;
        private const int Inch = 72;

        private const int TextWidth = Width - 2 * Inch;
        private const int TextHeight = Height - 3 * Inch;

        private const float FontSize = 5f;
        private const float Leading = 0.5f;
        private const float LeadingCoef = 2.640625f;
        private const float PHeight = FontSize * Leading * LeadingCoef;

        public static void Main() {
            var file = new FileInfo(Dest);
            file.Directory?.Create();
            
            var text = File.ReadAllText("../../../juno.txt");
            var image = new Bitmap("../../../juno.jpg");
            
            CreatePdf(Dest, text, image);
        }

        private static void CreatePdf(string dest, string text, Bitmap image) {
            var document = new Document(new PdfDocument(new PdfWriter(dest)), new PageSize(Width, Height));
            document.SetMargins(Inch, Inch, Inch, Inch);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            
            image = new Bitmap(image, new Size((int) (image.Width * (Inch / image.HorizontalResolution)), (int) (image.Height * (Inch / image.VerticalResolution))));

            var i = 0;
            var numLines = 0;
            var bankedHeight = 0.0;

            Console.WriteLine("Coloring text...");
            while ((numLines + 1) * PHeight < TextHeight)
            {
                var p = new Paragraph().SetMargin(0).SetPadding(0).SetWidth(Width - 2 * Inch).SetHeight(PHeight)
                    .SetTextAlignment(TextAlignment.JUSTIFIED_ALL);
                var runningWidth = 0.0;
                var bankedWidth = 0.0;
                
                var roundedHeight = (int) Math.Round(PHeight + bankedHeight);
                bankedHeight = (PHeight + bankedHeight) - roundedHeight;

                while (i < text.Length)
                {
                    var pixelWidth = font.GetWidth(text[i], FontSize);
                    var roundedWidth = (int) Math.Round(pixelWidth + bankedWidth);

                    if (runningWidth + pixelWidth > TextWidth) break;

                    float[] color = {0, 0, 0, 0};
                    
                    if (roundedWidth > 0) // this isn't getting triggered like,,, 50% of the time lol
                    {
                        color = AverageColor(image, (int) Math.Floor(runningWidth),
                            (int) Math.Floor(numLines * PHeight), roundedWidth,
                            roundedHeight);
                    }
                    
                    runningWidth += pixelWidth;
                    bankedWidth = (pixelWidth + bankedWidth) - roundedWidth;

                    var t = new Text(text[i].ToString());
                    t.SetFontColor(iText.Kernel.Colors.Color.MakeColor(new PdfDeviceCs.Cmyk(), color));
                    t.SetFont(font);
                    t.SetFontSize(FontSize);

                    i++;

                    p.Add(t);
                }
                numLines++;
                document.Add(p);
            }
            Console.WriteLine("Closing out...");
            document.Close();
        }

        private static float[] AverageColor(Bitmap img, int startX, int startY, int kernelWidth, int kernelHeight) {
            var r = 0;
            var b = 0;
            var g = 0;
            
            for (var i = startX; i < startX + kernelWidth; i++)
            {
                for (var j = startY; j < startY + kernelHeight; j++)
                {
                    var color = img.GetPixel(i, j);
                    r += color.R;
                    b += color.B;
                    g += color.G;
                }
            }

            var numPixels = kernelWidth * kernelHeight;
            r /= numPixels;
            b /= numPixels;
            g /= numPixels;
            
            return ConvertToCmyk(Color.FromArgb(r, g, b));
        }

        private static float[] ConvertToCmyk(Color color) {
            var r = (float) (color.R / 255.0);
            var g = (float) (color.G / 255.0);
            var b = (float) (color.B / 255.0);

            var k = 1 - Math.Max(Math.Max(r, g), b);
            
            float c = 0;
            float m = 0;
            float y = 0;

            if ((Math.Abs(k - 1) < 0.001)) return new[] {c, m, y, k};
            
            c = (1 - r - k) / (1 - k);
            m = (1 - g - k) / (1 - k);
            y = (1 - b - k) / (1 - k);

            return new[] {c, m, y, k};
        }
    }
}
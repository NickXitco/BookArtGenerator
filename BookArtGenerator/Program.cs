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
        private const string Dest = "../../../cmbyn.pdf";
        private const int Width = 18 * Inch;
        private const int Height = 24 * Inch;
        private const int Inch = 72;

        private const int TextWidth = Width - 2 * Inch;
        private const int TextHeight = Height - 3 * Inch;

        private const float FontSize = 5f;
        private const float Leading = 0.5f;
        private const float LeadingCoef = 2.640625f; //This coef was decided by trial and error, it has no basis to it.
        private const float PHeight = FontSize * Leading * LeadingCoef;

        private const float LightnessThreshold = 0.9f;
        
        public static void Main() {
            var file = new FileInfo(Dest);
            file.Directory?.Create();
            
            var text = File.ReadAllText("../../../cmbyn.txt");
            var image = new Bitmap("../../../cmbyn.jpg");
            
            CreatePdf(Dest, text, image);
        }

        private static void CreatePdf(string dest, string text, Bitmap image) {
            var document = new Document(new PdfDocument(new PdfWriter(dest)), new PageSize(Width, Height));
            document.SetMargins(Inch, Inch, Inch, Inch);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            image = new Bitmap(image,
                new Size((int) (image.Width * (Inch / image.HorizontalResolution)),
                        (int) (image.Height * (Inch / image.VerticalResolution)))); //Rescale image in case it's not 72dpi, may need to consider cropping it as well for generality's sake.

            var i = 0;
            var numLines = 0;
            var bankedHeight = 0.0;

            Console.WriteLine("Coloring text...");
            while ((numLines + 1) * PHeight < TextHeight) //+1 because we don't want to add a line that's gonna go over. if this looks bad we could consider adding a condition like line 72.
            {
                var p = new Paragraph().SetMargin(0).SetPadding(0).SetWidth(Width - 2 * Inch).SetHeight(PHeight)
                    .SetTextAlignment(TextAlignment.JUSTIFIED_ALL);
                var runningWidth = 0.0;
                var bankedWidth = 0.0;
                
                var roundedHeight = (int) Math.Round(PHeight + bankedHeight);
                bankedHeight = (PHeight + bankedHeight) - roundedHeight; //return unused bank to bank

                while (i < text.Length)
                {
                    var pixelWidth = font.GetWidth(text[i], FontSize); 
                    var roundedWidth = (int) Math.Round(pixelWidth + bankedWidth);

                    if (runningWidth + pixelWidth > TextWidth) break; //break condition for crossing over the right margin
                    
                    var startX = (int) Math.Floor(runningWidth);
                    var startY = (int) Math.Floor(numLines * PHeight);
                    var color = AverageColor(image, startX, startY, roundedWidth, roundedHeight); //run kernel for area size of character.

                    if (ColorIsTooLight(color, LightnessThreshold) && !string.IsNullOrWhiteSpace(text[i].ToString()))
                    {
                        text = text.Insert(i, " "); //whitespace handling so we don't print characters that you can't read
                        continue;
                    }

                    runningWidth += pixelWidth;
                    bankedWidth = (pixelWidth + bankedWidth) - roundedWidth; //return unused pixels to bank.

                    var t = new Text(text[i].ToString());
                    t.SetFontColor(iText.Kernel.Colors.Color.MakeColor(new PdfDeviceCs.Cmyk(), ConvertToCmyk(color)));
                    t.SetFont(font);
                    t.SetFontSize(FontSize);

                    i++;

                    p.Add(t); //add character to single-line paragraph
                }
                numLines++;
                document.Add(p); //add single-line paragraph to document
            }
            Console.WriteLine("Closing out...");
            document.Close();
        }

        private static bool ColorIsTooLight(Color color, float threshold)
        {
            return color.GetBrightness() >= threshold; //method returns 0 black to 1 white
        }
        
        private static Color AverageColor(Bitmap img, int startX, int startY, int kernelWidth, int kernelHeight) {
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
            if (numPixels < 1) return Color.White; //likely-unused edge-case handler in case a zero-width character goes through 
            
            r /= numPixels; //post-fact averaging, maybe consider making r, g, b larger values or do moving average
            b /= numPixels; //we just don't want to overflow the int if the kernel is too large
            g /= numPixels;
            
            return Color.FromArgb(r, g, b);
        }

        // standard RGB->CMYK conversion for print
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
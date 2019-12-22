using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.StyledXmlParser.Jsoup.Nodes;
using Document = iText.Layout.Document;

namespace BookArtGenerator
{
    internal static class Program
    {
        private const string Dest = "../../../hello_world.pdf";

        public static void Main(string[] args) {
            var file = new FileInfo(Dest);
            file.Directory?.Create();
            
            var text = File.ReadAllText("../../../juno.txt");
            var image = new Bitmap("../../../juno.jpg");
            
            CreatePdf(Dest, text, image);
        }

        private static void CreatePdf(string dest, string text, Bitmap image) {
            var document = new Document(new PdfDocument(new PdfWriter(dest)), new PageSize(1296, 1728));
            document.SetMargins(72, 72, 72 * 2, 72);
            var p =  new Paragraph();
            //p.SetTextAlignment(TextAlignment.JUSTIFIED_ALL);
            
            var adjustedWidth = image.Width * (72 / image.HorizontalResolution);
            var adjustedHeight = image.Height * (72 / image.VerticalResolution);
            
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var runningTotalPixelCount = 0.0;
            var bankedPixels = 0.0;
            
            for (var i = 0; i < text.Length; i++) {
                Text t = new Text(text[i].ToString());

                var pixelWidth = font.GetWidth(text[i], 5);

                var roundedWidth = (int) Math.Round(pixelWidth + bankedPixels);
                runningTotalPixelCount += pixelWidth;
                bankedPixels = pixelWidth - roundedWidth;

                if (runningTotalPixelCount > adjustedWidth)
                {
                    var s = "";
                    for (var j = i - 25; j < i + 1; j++)
                    {
                        s += text[j].ToString();
                    }
                    Console.WriteLine(runningTotalPixelCount.ToString() + ": " + s);
                    runningTotalPixelCount = 0;
                }
                
                
                var color = ConvertToCmyk(image.GetPixel(i % image.Width, i / image.Height));
                
                t.SetFontColor(iText.Kernel.Colors.Color.MakeColor(new PdfDeviceCs.Cmyk(), color));
                t.SetFont(font);
                t.SetFontSize(5);

                p.Add(t);
            }
            
            document.Add(p);
            document.Close();
        }

        private static float[] ConvertToCmyk(Color color) {
            var r = (float) (color.R / 255.0);
            var g = (float) (color.G / 255.0);
            var b = (float) (color.B / 255.0);

            var k = 1 - Math.Max(Math.Max(r, g), b);
            
            float c = 0;
            float m = 0;
            float y = 0;

            if ((Math.Abs(k - 1) < 0.001)) return new float[4] {c, m, y, k};
            
            c = (1 - r - k) / (1 - k);
            m = (1 - g - k) / (1 - k);
            y = (1 - b - k) / (1 - k);

            return new float[4] {c, m, y, k};
        }
    }
}
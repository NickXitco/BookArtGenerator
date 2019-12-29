using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Colorspace;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.StyledXmlParser.Jsoup.Nodes;
using Document = iText.Layout.Document;

namespace BookArtGenerator
{
    internal static class Program
    {
        private const string Dest = "../../../hello_world_singleparagraph.pdf";
        private const int Width = 18 * Inch;
        private const int Height = 8 * Inch;
        private const int Inch = 72;
        private const int FontSize = 5;
        private const float Leading = 0.5f;

        public static void Main(string[] args) {
            var file = new FileInfo(Dest);
            file.Directory?.Create();
            
            var text = File.ReadAllText("../../../juno.txt");
            var image = new Bitmap("../../../juno.jpg");
            
            CreatePdf(Dest, text, image);
        }

        private static void CreatePdf(string dest, string text, Bitmap image) {
            var document = new Document(new PdfDocument(new PdfWriter(dest)), new PageSize(Width, Height));
            document.SetMargins(Inch, Inch, 2 * Inch, Inch);
            var adjustedWidth = image.Width * (Inch / image.HorizontalResolution);
            var adjustedHeight = image.Height * (Inch / image.VerticalResolution);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            Console.WriteLine(adjustedWidth.ToString());

            var i = 0;
            var masterParagraph = new Paragraph().SetMultipliedLeading(Leading).SetMargin(0).SetPadding(0).SetWidth(Width - 2 * Inch).SetHeight(Height - 4 * Inch);
            var runningHeight = 0.0;
            var numLines = 0;
            
            Console.WriteLine(adjustedHeight.ToString());
            
            Console.WriteLine("Coloring text...");
            while (numLines <= 228)
            {
                var p = new Paragraph().SetMargin(0).SetPadding(0).SetWidth(Width - 2 * Inch).SetTextAlignment(TextAlignment.JUSTIFIED_ALL);
                var runningWidth = 0.0;
                var bankedPixels = 0.0;

                while (i < text.Length)
                {
                    Text t = new Text(text[i].ToString());

                    var pixelWidth = font.GetWidth(text[i], FontSize);

                    var roundedWidth = (int) Math.Round(pixelWidth + bankedPixels);

                    if (runningWidth + pixelWidth > adjustedWidth) break;

                    runningWidth += pixelWidth;

                    bankedPixels = pixelWidth - roundedWidth;

                    var color = ConvertToCmyk(image.GetPixel(i % image.Width, i / image.Height));

                    t.SetFontColor(iText.Kernel.Colors.Color.MakeColor(new PdfDeviceCs.Cmyk(), color));
                    t.SetFont(font);
                    t.SetFontSize(FontSize);

                    i++;

                    p.Add(t);
                }

                var s = "";
                for (var j = i - 25; j < i; j++)
                {
                    s += text[j].ToString();
                }

                Console.WriteLine(runningWidth + ": " + s);

                numLines++;
                runningHeight += FontSize * (Leading + 1);
                masterParagraph.Add(p);
            }
            
            
            var titleBox = new Paragraph().SetWidth(Width).SetHeight(Inch);
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            
            Text title = new Text("JUNO");
            title.SetFont(titleFont);
            title.SetFontSize(40);
            titleBox.Add(title);
            

            Console.WriteLine(runningHeight);
            Console.WriteLine("Adding to document...");
            document.Add(masterParagraph);
            document.Add(titleBox);
            Console.WriteLine("Closing out...");
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
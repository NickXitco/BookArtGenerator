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
            p.SetTextAlignment(TextAlignment.JUSTIFIED_ALL);
            for (var i = 0; i < text.Length; i++) {
                p.Add(Color(text[i], i, image));
            }
            document.Add(p);
            document.Close();
        }

        private static Text Color(char c, int i, Bitmap image) {
            Text t = new Text(c.ToString());

            var color = ConvertToCmyk(image.GetPixel(i % image.Width, i / image.Height));
            var f = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            
            var pixelWidth = f.GetWidth(c, 5);

            var flooredWidth = Math.Floor(pixelWidth);

            var leftover = pixelWidth - flooredWidth;
            
            //TODO the idea here is that we keep a running total of "leftover" width, and whenever we check the pixelWidth
            //of a character, we add the global leftovers to the pixelWidth, then floor it.
            //
            // e.g: let's say we have a letter with width 2.6. That gets floored to 2 pixels of the reference image
            //with .6 pixels leftover. The next letter has width 1.5 pixels. That gets added to the leftovers giving
            //it 2.1 pixels, which gets floored to grab 2 pixels of the reference image, instead of 1. Now we have
            //.1 leftover pixels, and we repeat the process. This should ensure that we don't leave any pixels unaccounted
            //for by the end of the loop for each row, or that the final character isn't getting overrepresented.
            
            
            t.SetFont(f);
            t.SetFontSize(5);
            t.SetFontColor(iText.Kernel.Colors.Color.MakeColor(new PdfDeviceCs.Cmyk(), color));
            
            return t;
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
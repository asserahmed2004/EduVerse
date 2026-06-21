using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Application.Services
{
    internal static class CertificatePdfGenerator
    {
        private const double PageWidth = 842;
        private const double PageHeight = 595;

        public static byte[] Generate(
            string studentName,
            string courseName,
            string organizationName,
            DateTime issuedAt,
            string certificateCode)
        {
            var logo = LoadLogo();
            var document = new PdfDocumentBuilder();
            var regularFontId = document.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            var boldFontId = document.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

            int? logoMaskId = null;
            int? logoId = null;
            if (logo != null)
            {
                logoMaskId = document.AddStream(
                    $"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} /ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode",
                    Compress(logo.Alpha));
                logoId = document.AddStream(
                    $"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /SMask {logoMaskId} 0 R",
                    Compress(logo.Rgb));
            }

            var content = BuildContent(studentName, courseName, organizationName, issuedAt, certificateCode, logoId.HasValue);
            var contentId = document.AddStream("<<", Encoding.ASCII.GetBytes(content));
            var pageId = document.NextObjectId;
            var pagesId = pageId + 1;

            var xObjects = logoId.HasValue ? $" /XObject << /Logo {logoId} 0 R >>" : string.Empty;
            document.AddObject(
                $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                $"/Resources << /Font << /F1 {regularFontId} 0 R /F2 {boldFontId} 0 R >>{xObjects} >> " +
                $"/Contents {contentId} 0 R >>");
            document.AddObject($"<< /Type /Pages /Kids [{pageId} 0 R] /Count 1 >>");
            var catalogId = document.AddObject($"<< /Type /Catalog /Pages {pagesId} 0 R >>");
            var infoId = document.AddObject(
                $"<< /Title ({EscapePdfText($"EduVerse Certificate - {courseName}")}) " +
                $"/Author (EduVerse) /Subject (Course completion certificate) " +
                $"/CreationDate (D:{issuedAt.ToUniversalTime():yyyyMMddHHmmss}Z) >>");

            return document.Build(catalogId, infoId);
        }

        private static string BuildContent(
            string studentName,
            string courseName,
            string organizationName,
            DateTime issuedAt,
            string certificateCode,
            bool hasLogo)
        {
            var content = new StringBuilder();

            FillRect(content, 0, 0, PageWidth, PageHeight, "#0A2A5E");
            FillRect(content, 16, 16, PageWidth - 32, PageHeight - 32, "#FFFFFF");
            StrokeRect(content, 27, 27, PageWidth - 54, PageHeight - 54, "#E7A31A", 2);
            StrokeRect(content, 34, 34, PageWidth - 68, PageHeight - 68, "#1769AA", 1);

            FillRect(content, 16, PageHeight - 34, PageWidth - 32, 18, "#0D4F99");
            FillRect(content, 16, PageHeight - 34, 215, 18, "#F39A18");
            FillRect(content, PageWidth - 188, PageHeight - 34, 172, 18, "#E7A31A");
            FillCircle(content, 82, 95, 46, "#EEF5FC");
            FillCircle(content, PageWidth - 76, PageHeight - 103, 34, "#FFF4DF");

            if (hasLogo)
            {
                content.AppendLine("q");
                content.AppendLine("72 0 0 69 385 476 cm");
                content.AppendLine("/Logo Do");
                content.AppendLine("Q");
            }
            else
            {
                DrawCenteredText(content, "EduVerse", 495, 24, true, "#0A2A5E");
            }

            DrawCenteredText(content, "CERTIFICATE OF COMPLETION", 426, 27, true, "#0A2A5E");
            DrawCenteredText(content, "This certificate is proudly presented to", 390, 12, false, "#5C6B80");
            DrawCenteredText(content, studentName, 343, 29, true, "#0D4F99");
            FillRect(content, 230, 330, 382, 2, "#F39A18");

            DrawCenteredText(content, "for successfully completing the course", 298, 12, false, "#5C6B80");
            DrawCenteredMultilineText(content, courseName, 263, 20, true, "#0A2A5E", 54, 25);
            DrawCenteredText(content, $"Presented by {organizationName}", 210, 12, false, "#3D4D63");

            FillRect(content, 88, 123, 666, 1, "#D8E2EE");
            DrawText(content, "ISSUE DATE", 105, 99, 9, true, "#7A8798");
            DrawText(content, issuedAt.ToUniversalTime().ToString("MMMM d, yyyy", CultureInfo.InvariantCulture), 105, 78, 12, true, "#0A2A5E");
            DrawCenteredText(content, "EduVerse Academic Team", 78, 11, true, "#0D4F99");
            DrawCenteredText(content, "Learning without limits", 59, 9, false, "#7A8798");
            DrawRightText(content, "CERTIFICATE CODE", 737, 99, 9, true, "#7A8798");
            DrawRightText(content, certificateCode, 737, 78, 11, true, "#0A2A5E");

            FillRect(content, 16, 16, 190, 8, "#F39A18");
            FillRect(content, 206, 16, PageWidth - 412, 8, "#1769AA");
            FillRect(content, PageWidth - 206, 16, 190, 8, "#E7A31A");
            return content.ToString();
        }

        private static void DrawCenteredMultilineText(
            StringBuilder content,
            string value,
            double y,
            double fontSize,
            bool bold,
            string color,
            int maxCharacters,
            double lineHeight)
        {
            var lines = WrapText(value, maxCharacters).Take(2).ToList();
            var startY = y + ((lines.Count - 1) * lineHeight / 2);
            for (var index = 0; index < lines.Count; index++)
                DrawCenteredText(content, lines[index], startY - (index * lineHeight), fontSize, bold, color);
        }

        private static IEnumerable<string> WrapText(string value, int maxCharacters)
        {
            var words = NormalizeText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maxCharacters)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                    line.Append(' ');
                line.Append(word);
            }

            if (line.Length > 0)
                yield return line.ToString();
        }

        private static void DrawCenteredText(StringBuilder content, string value, double y, double fontSize, bool bold, string color)
        {
            var normalized = NormalizeText(value);
            var width = EstimateTextWidth(normalized, fontSize, bold);
            DrawText(content, normalized, (PageWidth - width) / 2, y, fontSize, bold, color);
        }

        private static void DrawRightText(StringBuilder content, string value, double right, double y, double fontSize, bool bold, string color)
        {
            var normalized = NormalizeText(value);
            DrawText(content, normalized, right - EstimateTextWidth(normalized, fontSize, bold), y, fontSize, bold, color);
        }

        private static void DrawText(StringBuilder content, string value, double x, double y, double fontSize, bool bold, string color)
        {
            content.AppendLine("BT");
            content.AppendLine($"/{(bold ? "F2" : "F1")} {Number(fontSize)} Tf");
            content.AppendLine($"{Rgb(color)} rg");
            content.AppendLine($"1 0 0 1 {Number(x)} {Number(y)} Tm");
            content.AppendLine($"({EscapePdfText(value)}) Tj");
            content.AppendLine("ET");
        }

        private static double EstimateTextWidth(string value, double fontSize, bool bold)
        {
            var factor = bold ? 0.56 : 0.51;
            return value.Length * fontSize * factor;
        }

        private static void FillRect(StringBuilder content, double x, double y, double width, double height, string color)
        {
            content.AppendLine($"{Rgb(color)} rg");
            content.AppendLine($"{Number(x)} {Number(y)} {Number(width)} {Number(height)} re f");
        }

        private static void StrokeRect(StringBuilder content, double x, double y, double width, double height, string color, double lineWidth)
        {
            content.AppendLine($"{Rgb(color)} RG");
            content.AppendLine($"{Number(lineWidth)} w");
            content.AppendLine($"{Number(x)} {Number(y)} {Number(width)} {Number(height)} re S");
        }

        private static void FillCircle(StringBuilder content, double centerX, double centerY, double radius, string color)
        {
            const double kappa = 0.5522847498;
            var control = radius * kappa;
            content.AppendLine($"{Rgb(color)} rg");
            content.AppendLine($"{Number(centerX + radius)} {Number(centerY)} m");
            content.AppendLine($"{Number(centerX + radius)} {Number(centerY + control)} {Number(centerX + control)} {Number(centerY + radius)} {Number(centerX)} {Number(centerY + radius)} c");
            content.AppendLine($"{Number(centerX - control)} {Number(centerY + radius)} {Number(centerX - radius)} {Number(centerY + control)} {Number(centerX - radius)} {Number(centerY)} c");
            content.AppendLine($"{Number(centerX - radius)} {Number(centerY - control)} {Number(centerX - control)} {Number(centerY - radius)} {Number(centerX)} {Number(centerY - radius)} c");
            content.AppendLine($"{Number(centerX + control)} {Number(centerY - radius)} {Number(centerX + radius)} {Number(centerY - control)} {Number(centerX + radius)} {Number(centerY)} c f");
        }

        private static string Rgb(string hex)
        {
            var value = hex.TrimStart('#');
            var red = Convert.ToInt32(value[..2], 16) / 255d;
            var green = Convert.ToInt32(value.Substring(2, 2), 16) / 255d;
            var blue = Convert.ToInt32(value.Substring(4, 2), 16) / 255d;
            return $"{Number(red)} {Number(green)} {Number(blue)}";
        }

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;
                result.Append(character <= 255 ? character : '?');
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string EscapePdfText(string value)
        {
            var escaped = new StringBuilder();
            foreach (var character in NormalizeText(value))
            {
                switch (character)
                {
                    case '\\':
                    case '(':
                    case ')':
                        escaped.Append('\\').Append(character);
                        break;
                    case '\r':
                    case '\n':
                        escaped.Append(' ');
                        break;
                    default:
                        if (character < 32 || character > 126)
                            escaped.Append('\\').Append(Convert.ToString(character, 8).PadLeft(3, '0'));
                        else
                            escaped.Append(character);
                        break;
                }
            }

            return escaped.ToString();
        }

        private static PngImage? LoadLogo()
        {
            var logoPath = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "eduverse-logo.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "EduVerse.Web", "public", "eduverse-logo.png")
            }.FirstOrDefault(File.Exists);
            if (logoPath == null)
                return null;

            try
            {
                return DecodeRgbaPng(File.ReadAllBytes(logoPath));
            }
            catch
            {
                return null;
            }
        }

        private static PngImage DecodeRgbaPng(byte[] png)
        {
            var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (png.Length < 33 || !png.AsSpan(0, 8).SequenceEqual(signature))
                throw new InvalidDataException("Invalid PNG signature.");

            var width = 0;
            var height = 0;
            var bitDepth = 0;
            var colorType = 0;
            using var idat = new MemoryStream();

            var offset = 8;
            while (offset + 12 <= png.Length)
            {
                var length = ReadBigEndianInt32(png, offset);
                var type = Encoding.ASCII.GetString(png, offset + 4, 4);
                var dataOffset = offset + 8;
                if (length < 0 || dataOffset + length + 4 > png.Length)
                    throw new InvalidDataException("Invalid PNG chunk.");

                if (type == "IHDR")
                {
                    width = ReadBigEndianInt32(png, dataOffset);
                    height = ReadBigEndianInt32(png, dataOffset + 4);
                    bitDepth = png[dataOffset + 8];
                    colorType = png[dataOffset + 9];
                    if (png[dataOffset + 12] != 0)
                        throw new NotSupportedException("Interlaced PNG files are not supported.");
                }
                else if (type == "IDAT")
                {
                    idat.Write(png, dataOffset, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                offset = dataOffset + length + 4;
            }

            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType != 6)
                throw new NotSupportedException("The EduVerse logo must be an 8-bit RGBA PNG.");

            idat.Position = 0;
            using var decompressed = new MemoryStream();
            using (var inflater = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
                inflater.CopyTo(decompressed);

            var source = decompressed.ToArray();
            var bytesPerPixel = 4;
            var rowLength = width * bytesPerPixel;
            var expectedLength = height * (rowLength + 1);
            if (source.Length < expectedLength)
                throw new InvalidDataException("PNG pixel data is incomplete.");

            var rgba = new byte[width * height * bytesPerPixel];
            var previousRow = new byte[rowLength];
            var currentRow = new byte[rowLength];
            var sourceOffset = 0;
            var outputOffset = 0;

            for (var row = 0; row < height; row++)
            {
                var filter = source[sourceOffset++];
                for (var index = 0; index < rowLength; index++)
                {
                    var raw = source[sourceOffset++];
                    var left = index >= bytesPerPixel ? currentRow[index - bytesPerPixel] : 0;
                    var above = previousRow[index];
                    var upperLeft = index >= bytesPerPixel ? previousRow[index - bytesPerPixel] : 0;
                    currentRow[index] = filter switch
                    {
                        0 => raw,
                        1 => unchecked((byte)(raw + left)),
                        2 => unchecked((byte)(raw + above)),
                        3 => unchecked((byte)(raw + ((left + above) / 2))),
                        4 => unchecked((byte)(raw + Paeth(left, above, upperLeft))),
                        _ => throw new NotSupportedException($"PNG filter {filter} is not supported.")
                    };
                }

                Buffer.BlockCopy(currentRow, 0, rgba, outputOffset, rowLength);
                outputOffset += rowLength;
                (previousRow, currentRow) = (currentRow, previousRow);
                Array.Clear(currentRow);
            }

            var rgb = new byte[width * height * 3];
            var alpha = new byte[width * height];
            for (var pixel = 0; pixel < width * height; pixel++)
            {
                rgb[(pixel * 3)] = rgba[(pixel * 4)];
                rgb[(pixel * 3) + 1] = rgba[(pixel * 4) + 1];
                rgb[(pixel * 3) + 2] = rgba[(pixel * 4) + 2];
                alpha[pixel] = rgba[(pixel * 4) + 3];
            }

            return new PngImage(width, height, rgb, alpha);
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }

        private static int Paeth(int left, int above, int upperLeft)
        {
            var estimate = left + above - upperLeft;
            var leftDistance = Math.Abs(estimate - left);
            var aboveDistance = Math.Abs(estimate - above);
            var upperLeftDistance = Math.Abs(estimate - upperLeft);
            return leftDistance <= aboveDistance && leftDistance <= upperLeftDistance
                ? left
                : aboveDistance <= upperLeftDistance ? above : upperLeft;
        }

        private static byte[] Compress(byte[] value)
        {
            using var output = new MemoryStream();
            using (var compressor = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
                compressor.Write(value);
            return output.ToArray();
        }

        private sealed record PngImage(int Width, int Height, byte[] Rgb, byte[] Alpha);

        private sealed class PdfDocumentBuilder
        {
            private readonly List<byte[]> objects = [];

            public int NextObjectId => objects.Count + 1;

            public int AddObject(string value)
            {
                objects.Add(Encoding.ASCII.GetBytes(value));
                return objects.Count;
            }

            public int AddStream(string dictionaryStart, byte[] content)
            {
                using var stream = new MemoryStream();
                var dictionary = Encoding.ASCII.GetBytes($"{dictionaryStart} /Length {content.Length} >>\nstream\n");
                stream.Write(dictionary);
                stream.Write(content);
                stream.Write(Encoding.ASCII.GetBytes("\nendstream"));
                objects.Add(stream.ToArray());
                return objects.Count;
            }

            public byte[] Build(int catalogId, int infoId)
            {
                using var output = new MemoryStream();
                Write(output, "%PDF-1.7\n");
                output.Write([0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A]);

                var offsets = new List<long> { 0 };
                for (var index = 0; index < objects.Count; index++)
                {
                    offsets.Add(output.Position);
                    Write(output, $"{index + 1} 0 obj\n");
                    output.Write(objects[index]);
                    Write(output, "\nendobj\n");
                }

                var xrefOffset = output.Position;
                Write(output, $"xref\n0 {objects.Count + 1}\n");
                Write(output, "0000000000 65535 f \n");
                foreach (var offset in offsets.Skip(1))
                    Write(output, $"{offset:0000000000} 00000 n \n");

                Write(output,
                    $"trailer\n<< /Size {objects.Count + 1} /Root {catalogId} 0 R /Info {infoId} 0 R >>\n" +
                    $"startxref\n{xrefOffset}\n%%EOF");
                return output.ToArray();
            }

            private static void Write(Stream stream, string value)
            {
                stream.Write(Encoding.ASCII.GetBytes(value));
            }
        }
    }
}

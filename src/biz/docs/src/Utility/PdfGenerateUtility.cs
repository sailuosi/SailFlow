using System;
using System.Collections.Generic;
using System.IO;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using LayoutDocument = iText.Layout.Document;
using PageSize = iText.Kernel.Geom.PageSize;

namespace sailflow.biz.Utility
{
    /// <summary>
    /// PDF document generation utility for workflow process documents
    /// 工作流程文档PDF生成工具类（业务侧实现，不属于引擎核心）
    /// </summary>
    public static class PdfGenerateUtility
    {
        private static readonly DeviceRgb HeaderColor = new DeviceRgb(22, 119, 255);
        private static readonly DeviceRgb RowAltColor = new DeviceRgb(245, 248, 255);
        private static readonly DeviceRgb LabelBgColor = new DeviceRgb(235, 241, 255);
        private static readonly DeviceRgb BorderColor = new DeviceRgb(220, 220, 220);
        private static readonly DeviceRgb MetaColor = new DeviceRgb(120, 120, 120);

        /// <summary>
        /// Generate a workflow process document PDF from key-value fields
        /// 根据字段键值对生成工作流程文档PDF文件
        /// </summary>
        public static void GenerateProcessDocument(string title, IDictionary<string, string> fields, string outputPath)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath) ?? ".");
            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = CreateLayoutDocument(pdf);
            AddContent(document, title, fields);
        }

        /// <summary>
        /// Generate PDF bytes from title and key-value fields (no file I/O)
        /// 生成PDF字节流，不写入文件
        /// </summary>
        public static byte[] GenerateProcessDocumentBytes(string title, IDictionary<string, string> fields)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            {
                writer.SetCloseStream(false);
                using var pdf = new PdfDocument(writer);
                using var document = CreateLayoutDocument(pdf);
                AddContent(document, title, fields);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Generate a simple PDF from plain text content
        /// 从纯文本内容生成简单PDF
        /// </summary>
        public static byte[] GenerateFromContent(string title, string content)
        {
            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            {
                writer.SetCloseStream(false);
                using var pdf = new PdfDocument(writer);
                using var document = CreateLayoutDocument(pdf);

                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var metaFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                document.Add(new Paragraph(title)
                    .SetFont(titleFont)
                    .SetFontSize(16f)
                    .SetFontColor(HeaderColor)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(4f));
                document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(metaFont)
                    .SetFontSize(9f)
                    .SetFontColor(MetaColor)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(14f));
                document.Add(new Paragraph(content ?? string.Empty)
                    .SetFont(bodyFont)
                    .SetFontSize(11f));
            }
            return ms.ToArray();
        }

        static LayoutDocument CreateLayoutDocument(PdfDocument pdf)
        {
            var document = new LayoutDocument(pdf, PageSize.A4);
            document.SetMargins(60f, 50f, 60f, 50f);
            return document;
        }

        static void AddContent(LayoutDocument document, string title, IDictionary<string, string> fields)
        {
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var metaFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var labelFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var valueFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph(title)
                .SetFont(titleFont)
                .SetFontSize(18f)
                .SetFontColor(HeaderColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(4f));
            document.Add(new Paragraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .SetFont(metaFont)
                .SetFontSize(9f)
                .SetFontColor(MetaColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(16f));

            if (fields == null || fields.Count == 0)
                return;

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3f, 7f }))
                .UseAllAvailableWidth()
                .SetMarginTop(6f);

            var row = 0;
            foreach (var kvp in fields)
            {
                var bg = row % 2 == 0 ? ColorConstants.WHITE : RowAltColor;
                table.AddCell(new Cell()
                    .Add(new Paragraph(kvp.Key).SetFont(labelFont).SetFontSize(10f))
                    .SetPadding(7f)
                    .SetBackgroundColor(LabelBgColor)
                    .SetBorder(new SolidBorder(BorderColor, 0.5f)));
                table.AddCell(new Cell()
                    .Add(new Paragraph(kvp.Value ?? string.Empty).SetFont(valueFont).SetFontSize(10f))
                    .SetPadding(7f)
                    .SetBackgroundColor(bg)
                    .SetBorder(new SolidBorder(BorderColor, 0.5f)));
                row++;
            }

            document.Add(table);
        }
    }
}

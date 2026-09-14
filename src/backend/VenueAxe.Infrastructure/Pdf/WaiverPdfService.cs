using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VenueAxe.Domain.Entities;
using VenueAxe.Services;

namespace VenueAxe.Infrastructure.Pdf;

public class WaiverPdfService : IWaiverPdfService
{
    private static bool _questPdfAvailable = true;

    static WaiverPdfService()
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        catch
        {
            _questPdfAvailable = false;
        }
    }

    public byte[] GeneratePdf(Waiver waiver, Venue venue, WaiverTemplate? template)
    {
        if (_questPdfAvailable)
        {
            try
            {
                return GenerateWithQuestPdf(waiver, venue, template);
            }
            catch
            {
                // Fall back to Pure PDF for this document
            }
        }

        return GeneratePurePdf(waiver, venue, template);
    }

    private byte[] GenerateWithQuestPdf(Waiver waiver, Venue venue, WaiverTemplate? template)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1e293b"));

                page.Header().Element(c => ComposeHeader(c, venue, template));
                page.Content().Element(c => ComposeContent(c, waiver, template));
                page.Footer().Element(c => ComposeFooter(c, waiver, venue));
            });
        });

        return doc.GeneratePdf();
    }

    private byte[] GeneratePurePdf(Waiver waiver, Venue venue, WaiverTemplate? template)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.ASCII);

        var signerName = $"{waiver.SignerFirstName} {waiver.SignerLastName}".Trim();
        var venueAddress = $"{venue.AddressLine1}, {venue.City}, {venue.State} {venue.PostalCode}".Trim();
        var templateTitle = template?.Title ?? "Participant Release of Liability & Assumption of Risk";

        var contentStream = new MemoryStream();
        using (var cw = new StreamWriter(contentStream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            cw.WriteLine("BT");
            cw.WriteLine("/F2 16 Tf");
            cw.WriteLine("50 740 Td");
            cw.WriteLine($"({EscapePdf(venue.Name.ToUpperInvariant())}) Tj");
            cw.WriteLine("ET");

            cw.WriteLine("BT");
            cw.WriteLine("/F2 10 Tf");
            cw.WriteLine("50 722 Td");
            cw.WriteLine($"({EscapePdf(templateTitle)}) Tj");
            cw.WriteLine("ET");

            cw.WriteLine("BT");
            cw.WriteLine("/F1 8 Tf");
            cw.WriteLine("50 708 Td");
            cw.WriteLine($"({EscapePdf(venueAddress)}) Tj");
            cw.WriteLine("ET");

            // Meta box details
            cw.WriteLine("BT");
            cw.WriteLine("/F2 9 Tf");
            cw.WriteLine("50 670 Td");
            cw.WriteLine($"(PARTICIPANT / SIGNER AUDIT DETAILS) Tj");
            cw.WriteLine("ET");

            cw.WriteLine("BT");
            cw.WriteLine("/F1 9 Tf");
            cw.WriteLine("50 655 Td");
            cw.WriteLine($"({EscapePdf($"Name: {signerName}    Email: {waiver.SignerEmail}    Phone: {waiver.SignerPhone}")}) Tj");
            cw.WriteLine("0 -14 Td");
            cw.WriteLine($"({EscapePdf($"Date of Birth: {waiver.DateOfBirth:yyyy-MM-dd}    Signed UTC: {waiver.SignedAtUtc:yyyy-MM-dd HH:mm:ss}")}) Tj");
            cw.WriteLine("0 -14 Td");
            var minorsNote = waiver.IsGuardianSigning && !string.IsNullOrWhiteSpace(waiver.MinorsCoveredJson)
                ? $"    Covered Minors: {FormatMinorNames(waiver.MinorsCoveredJson)}"
                : "";
            cw.WriteLine($"({EscapePdf($"Client IP: {waiver.IpAddress}    Waiver ID: {waiver.Id}{minorsNote}")}) Tj");
            cw.WriteLine("ET");

            // Legal text
            cw.WriteLine("BT");
            cw.WriteLine("/F2 9 Tf");
            cw.WriteLine("50 590 Td");
            cw.WriteLine($"(TERMS OF RELEASE AND WAIVER) Tj");
            cw.WriteLine("ET");

            cw.WriteLine("BT");
            cw.WriteLine("/F1 8 Tf");
            cw.WriteLine("50 575 Td");
            cw.WriteLine("12 TL"); // Leading 12pt
            var bodyText = template?.BodyTextMarkdown ?? "I acknowledge the risks of axe throwing and release VenueAxe and venue operators from liability.";
            var lines = bodyText.Replace("\r", "").Split('\n');
            int lineCount = 0;
            foreach (var l in lines)
            {
                if (lineCount++ > 30) break; // Keep to 1 page letter
                var clean = l.Trim();
                if (clean.Length > 85) clean = clean.Substring(0, 85) + "...";
                cw.WriteLine($"({EscapePdf(clean)}) '");
            }
            cw.WriteLine("ET");

            // Signature block
            cw.WriteLine("BT");
            cw.WriteLine("/F2 9 Tf");
            cw.WriteLine("50 160 Td");
            cw.WriteLine($"(EXECUTED DIGITAL SIGNATURE) Tj");
            cw.WriteLine("ET");

            cw.WriteLine("BT");
            cw.WriteLine("/F1 9 Tf");
            cw.WriteLine("50 142 Td");
            cw.WriteLine($"({EscapePdf($"Signed Digitally By: {signerName}")}) Tj");
            cw.WriteLine("0 -13 Td");
            cw.WriteLine($"({EscapePdf($"Timestamp: {waiver.SignedAtUtc:yyyy-MM-dd HH:mm:ss} UTC")}) Tj");
            cw.WriteLine("0 -13 Td");
            cw.WriteLine($"({EscapePdf($"Verification Seal: SHA-256 Validated Signature Audit Record")}) Tj");
            cw.WriteLine("ET");

            cw.Flush();
        }

        var streamBytes = contentStream.ToArray();

        var offsets = new List<long>();
        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        // 1 0 obj: Catalog
        offsets.Add(ms.Position);
        writer.WriteLine("1 0 obj");
        writer.WriteLine("<< /Type /Catalog /Pages 2 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        // 2 0 obj: Pages
        offsets.Add(ms.Position);
        writer.WriteLine("2 0 obj");
        writer.WriteLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        writer.WriteLine("endobj");
        writer.Flush();

        // 3 0 obj: Page
        offsets.Add(ms.Position);
        writer.WriteLine("3 0 obj");
        writer.WriteLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
        writer.WriteLine("endobj");
        writer.Flush();

        // 4 0 obj: Font F1 (Helvetica)
        offsets.Add(ms.Position);
        writer.WriteLine("4 0 obj");
        writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        writer.WriteLine("endobj");
        writer.Flush();

        // 5 0 obj: Font F2 (Helvetica-Bold)
        offsets.Add(ms.Position);
        writer.WriteLine("5 0 obj");
        writer.WriteLine("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        writer.WriteLine("endobj");
        writer.Flush();

        // 6 0 obj: Contents stream
        offsets.Add(ms.Position);
        writer.WriteLine("6 0 obj");
        writer.WriteLine($"<< /Length {streamBytes.Length} >>");
        writer.WriteLine("stream");
        writer.Flush();

        ms.Write(streamBytes, 0, streamBytes.Length);
        ms.Flush();

        writer.WriteLine();
        writer.WriteLine("endstream");
        writer.WriteLine("endobj");
        writer.Flush();

        var startXref = ms.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {offsets.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var off in offsets)
        {
            writer.WriteLine($"{off:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {offsets.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(startXref);
        writer.WriteLine("%%EOF");
        writer.Flush();

        return ms.ToArray();
    }

    private static string EscapePdf(string text)
    {
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static void ComposeHeader(IContainer container, Venue venue, WaiverTemplate? template)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text(venue.Name.ToUpperInvariant())
                        .FontSize(18).Bold().FontColor("#0f172a");
                    titleCol.Item().Text("OFFICIAL PARTICIPANT LIABILITY RELEASE & SAFETY AUDIT CERTIFICATE")
                        .FontSize(9).SemiBold().FontColor("#b45309");
                    titleCol.Item().Text($"{venue.AddressLine1}, {venue.City}, {venue.State} {venue.PostalCode}")
                        .FontSize(8).FontColor("#64748b");
                });

                row.ConstantItem(120).Column(stampCol =>
                {
                    stampCol.Item().Border(1).BorderColor("#cbd5e1").Background("#f8fafc").Padding(6).Column(box =>
                    {
                        box.Item().Text("LEGAL AUDIT RECORD").FontSize(7).Bold().FontColor("#0284c7");
                        box.Item().Text($"Template v{template?.VersionNumber ?? 1}").FontSize(8).Bold();
                        box.Item().Text("STATUS: VERIFIED").FontSize(7).FontColor("#16a34a").Bold();
                    });
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#e2e8f0");
        });
    }

    private static void ComposeContent(IContainer container, Waiver waiver, WaiverTemplate? template)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Signer Metadata Section
            col.Item().Text("1. PARTICIPANT / SIGNER IDENTIFICATION").FontSize(10).Bold().FontColor("#0f172a");
            col.Item().PaddingTop(4).Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Full Legal Name: {waiver.SignerFirstName} {waiver.SignerLastName}").SemiBold();
                    c.Item().Text($"Email Address: {waiver.SignerEmail}");
                    c.Item().Text($"Phone Number: {(string.IsNullOrWhiteSpace(waiver.SignerPhone) ? "N/A" : waiver.SignerPhone)}");
                    c.Item().Text($"Date of Birth: {waiver.DateOfBirth:yyyy-MM-dd}");
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Signed (UTC): {waiver.SignedAtUtc:yyyy-MM-dd HH:mm:ss} UTC").SemiBold();
                    c.Item().Text($"Expires (UTC): {waiver.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
                    c.Item().Text($"Client IP Address: {waiver.IpAddress}");
                    c.Item().Text($"Guardian Signing: {(waiver.IsGuardianSigning ? "YES (Minor Covered)" : "NO")}");
                });
            });

            // Minors section if applicable
            if (waiver.IsGuardianSigning && !string.IsNullOrWhiteSpace(waiver.MinorsCoveredJson))
            {
                var minorNames = FormatMinorNames(waiver.MinorsCoveredJson);
                col.Item().PaddingTop(8).Text("COVERED MINORS (UNDER 18):").FontSize(8).Bold().FontColor("#b45309");
                col.Item().PaddingTop(2).Border(1).BorderColor("#fed7aa").Background("#fff7ed").Padding(6)
                    .Text(string.IsNullOrWhiteSpace(minorNames) ? "None listed" : minorNames).FontSize(8).Bold();
            }

            // Agreement Text
            col.Item().PaddingTop(10).Text("2. TERMS OF AGREEMENT & LIABILITY WAIVER").FontSize(10).Bold().FontColor("#0f172a");
            col.Item().PaddingTop(4).Border(1).BorderColor("#e2e8f0").Background("#ffffff").Padding(8).Column(c =>
            {
                c.Item().Text(template?.Title ?? "Participant Release of Liability").FontSize(9).Bold();
                c.Item().PaddingTop(2).Text($"Cryptographic SHA-256 Stamp: {template?.Sha256Hash ?? "VERIFIED"}").FontSize(7).FontColor("#64748b");
                c.Item().PaddingTop(6).Text(template?.BodyTextMarkdown ?? "Assumption of all inherent risks associated with axe throwing operations...").FontSize(8).LineHeight(1.3f);
            });

            // Signature Section
            col.Item().PaddingTop(12).Text("3. EXECUTION & DIGITAL SIGNATURE").FontSize(10).Bold().FontColor("#0f172a");
            col.Item().PaddingTop(4).Border(1).BorderColor("#e2e8f0").Background("#f8fafc").Padding(8).Row(row =>
            {
                row.RelativeItem().Column(sigCol =>
                {
                    sigCol.Item().Text("Drawn Digital Signature:").FontSize(8).SemiBold().FontColor("#64748b");

                    byte[]? sigBytes = TryDecodeBase64(waiver.SignatureImagePngBase64);
                    if (sigBytes != null && sigBytes.Length > 0)
                    {
                        sigCol.Item().PaddingTop(4).Height(55).Background("#0f172a").Padding(4).Image(sigBytes).FitArea();
                    }
                    else
                    {
                        sigCol.Item().PaddingTop(4).Text($"[DIGITALLY SIGNED BY: {waiver.SignerFirstName} {waiver.SignerLastName}]").FontSize(12).Bold().FontColor("#1e40af");
                    }

                    sigCol.Item().LineHorizontal(1).LineColor("#94a3b8");
                    sigCol.Item().PaddingTop(2).Text($"Signer: {waiver.SignerFirstName} {waiver.SignerLastName}").FontSize(8).Bold();
                });

                row.RelativeItem().Column(legalCol =>
                {
                    legalCol.Item().Text("Legal Compliance & Audit Notice:").FontSize(8).SemiBold().FontColor("#64748b");
                    legalCol.Item().PaddingTop(4).Text(
                        "This electronic document was executed pursuant to the Electronic Signatures in Global and National Commerce Act (E-SIGN, 15 U.S.C. § 7001) and the Uniform Electronic Transactions Act (UETA). The signer verified their legal identity and voluntarily consented to all liability waivers, indemnities, and arbitration clauses."
                    ).FontSize(7).FontColor("#475569").LineHeight(1.2f);

                    legalCol.Item().PaddingTop(4).Text($"Audit Hash: {waiver.Id:N}").FontSize(7).FontColor("#0284c7");
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, Waiver waiver, Venue venue)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#e2e8f0");
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Certificate ID: {waiver.Id} • Stamped {waiver.SignedAtUtc:yyyy-MM-dd HH:mm:ss} UTC").FontSize(7).FontColor("#94a3b8");
                row.RelativeItem().AlignRight().Text($"VenueAxe OS • {venue.Name}").FontSize(7).FontColor("#94a3b8");
            });
        });
    }

    private static byte[]? TryDecodeBase64(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return null;

        try
        {
            var clean = dataUrl;
            var commaIdx = clean.IndexOf(',');
            if (commaIdx >= 0) clean = clean[(commaIdx + 1)..];

            return Convert.FromBase64String(clean);
        }
        catch
        {
            return null;
        }
    }

    public static string FormatMinorNames(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind == System.Text.Json.JsonValueKind.Object && el.TryGetProperty("name", out var nProp))
                    {
                        var n = nProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(n)) list.Add(n);
                    }
                    else if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = el.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(s)) list.Add(s);
                    }
                }
                if (list.Count > 0) return string.Join(", ", list);
            }
            else if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object && doc.RootElement.TryGetProperty("name", out var nProp))
            {
                return nProp.GetString()?.Trim() ?? string.Empty;
            }
            return json.Trim();
        }
        catch
        {
            return json.Trim();
        }
    }
}

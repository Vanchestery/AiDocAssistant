using UglyToad.PdfPig;
using Xunit;

namespace AiDocAssistant.Tests;

public class SamplePdfFixturesTests
{
    [Fact]
    public void Sample_pdf_a_is_readable_by_pdfpig()
    {
        var text = ReadFixture("sample-invoice-a-112700.pdf");
        Assert.Contains("112700", text.Replace(" ", string.Empty));
    }

    [Fact]
    public void Sample_pdf_b_is_readable_by_pdfpig()
    {
        var text = ReadFixture("sample-invoice-b-115000.pdf");
        Assert.Contains("115000", text.Replace(" ", string.Empty));
    }

    private static string ReadFixture(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", fileName));
        using var pdf = PdfDocument.Open(path);
        return pdf.GetPage(1).Text;
    }
}

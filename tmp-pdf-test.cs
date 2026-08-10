using UglyToad.PdfPig;
try {
  using var pdf = PdfDocument.Open(@"tests/fixtures/sample-invoice-a-112700.pdf");
  Console.WriteLine("OK pages=" + pdf.NumberOfPages + " text len=" + pdf.GetPage(1).Text.Length);
} catch (Exception ex) { Console.WriteLine("FAIL: " + ex.Message); }

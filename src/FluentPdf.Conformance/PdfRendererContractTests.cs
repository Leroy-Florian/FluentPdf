using System.Text;
using FluentAssertions;
using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Kernel;
using Xunit;

namespace FluentPdf.Conformance;

/// <summary>
/// The contract every <see cref="IPdfRenderer"/> adapter must satisfy. An adapter author
/// adds a small concrete subclass in their test project — supplying a renderer factory and
/// a text-extraction hook backed by their own library — and the inherited facts prove the
/// adapter honours the agnostic model.
/// </summary>
/// <remarks>
/// The suite asserts <em>semantic</em> equivalence (a valid PDF header, the presence and
/// ordering of text, page counts, capability honouring), never pixel- or byte-level
/// equality, because every PDF library lays content out differently. That is precisely how
/// a single document model stays portable across QuestPDF, iText, PDFsharp and the rest.
/// </remarks>
public abstract class PdfRendererContractTests
{
    /// <summary>Creates a fresh instance of the adapter under test.</summary>
    protected abstract IPdfRenderer CreateRenderer();

    /// <summary>
    /// Extracts the visible text from rendered bytes using the adapter's own tooling
    /// (a real adapter would use its library's text extractor; a text-based fake decodes
    /// the payload directly).
    /// </summary>
    protected abstract string ExtractText(IReadOnlyList<byte> content);

    [Fact]
    public void Describes_itself()
    {
        var renderer = CreateRenderer();

        renderer.Descriptor.Should().NotBeNull();
        renderer.Descriptor.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Rejects_a_null_document()
    {
        var renderer = CreateRenderer();

        var result = renderer.Render(null!);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Produces_a_pdf_header()
    {
        var renderer = CreateRenderer();

        var result = renderer.Render(CanonicalDocuments.Baseline());

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().NotBeEmpty();
        StartsWithPdfHeader(result.Value.Content).Should().BeTrue(
            "every PDF document begins with the '%PDF-' signature");
    }

    [Fact]
    public void Reports_the_pdf_content_type()
    {
        var renderer = CreateRenderer();

        var result = renderer.Render(CanonicalDocuments.Baseline());

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be(RenderedPdf.PdfContentType);
    }

    [Fact]
    public void Preserves_paragraph_text()
    {
        var renderer = CreateRenderer();

        var result = renderer.Render(CanonicalDocuments.WithText(CanonicalDocuments.TextMarker));

        result.IsSuccess.Should().BeTrue();
        ExtractText(result.Value.Content).Should().Contain(CanonicalDocuments.TextMarker);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Counts_at_least_one_page_per_explicit_break(int pageBreaks)
    {
        var renderer = CreateRenderer();

        var result = renderer.Render(CanonicalDocuments.WithPageBreaks(pageBreaks));

        result.IsSuccess.Should().BeTrue();
        result.Value.PageCount.Should().BeGreaterThanOrEqualTo(pageBreaks + 1);
    }

    [Fact]
    public void Honours_its_declared_capabilities()
    {
        var renderer = CreateRenderer();
        var document = CanonicalDocuments.FullFeature();
        var useCase = new RenderDocumentUseCase(renderer);

        var result = useCase.Execute(document);

        var required = DocumentFeatureScanner.Scan(document);
        var missing = renderer.Descriptor.Capabilities.Missing(required);

        if (missing == PdfFeature.None)
        {
            result.IsSuccess.Should().BeTrue(
                "the adapter advertises support for every feature this document uses");
        }
        else
        {
            result.IsFailure.Should().BeTrue(
                "the adapter must refuse content it cannot render rather than diverge silently");
            result.Error.Code.Should().Be("Render.UnsupportedFeatures");
        }
    }

    [Fact]
    public void Renders_consistently_under_concurrent_load()
    {
        // A single renderer instance is shared across threads, exactly as a mass-print host
        // would reuse one. This is the contract that makes parallel batch rendering safe: it
        // catches any mutable state hidden in the adapter or the pipeline it drives (the
        // paginator, measurer, composer), which would otherwise corrupt output under load.
        var renderer = CreateRenderer();
        var document = CanonicalDocuments.Paged();

        var expectedPageCount = renderer.Render(document).Value.PageCount;

        const int renders = 64;
        var results = new Result<RenderedPdf>[renders];

        Parallel.For(0, renders, i => results[i] = renderer.Render(document));

        results.Should().OnlyContain(
            result => result.IsSuccess,
            "a shared renderer must never fail when rendered from many threads at once");
        results.Should().OnlyContain(
            result => result.Value.PageCount == expectedPageCount,
            "concurrent renders of one document must paginate identically to a serial render");

        foreach (var result in results)
        {
            StartsWithPdfHeader(result.Value.Content).Should().BeTrue();
            ExtractText(result.Value.Content).Should().Contain(CanonicalDocuments.TextMarker);
        }
    }

    private static bool StartsWithPdfHeader(IReadOnlyList<byte> content)
    {
        var signature = Encoding.ASCII.GetBytes("%PDF-");

        if (content.Count < signature.Length)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (content[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}

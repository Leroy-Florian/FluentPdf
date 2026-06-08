using FluentPdf.Adapters.IText;
using FluentPdf.Adapters.QuestPdf;
using FluentPdf.Application.Rendering;
using FluentPdf.Kernel;
using FluentPdf.Samples.Contract;
using FluentPdf.Samples.FinancialReport;
using FluentPdf.Samples.Invoice;
using FluentPdf.Visual;

namespace FluentPdf.Visual.UnitTests;

/// <summary>
/// Integration tests that render the real samples through the QuestPDF and iText adapters and
/// verify, via the visual engine, that the output is valid and the content is actually on the
/// page. Cross-adapter pixel similarity is reported for review but not asserted (two layout
/// engines place content differently); identity (self-comparison) is asserted to prove the
/// engine's baseline-regression use.
/// </summary>
public sealed class AdapterRenderingTests
{
    public static IEnumerable<object[]> Renderers =>
    [
        [new QuestPdfRenderer()],
        [new ITextRenderer()],
    ];

    [Theory]
    [MemberData(nameof(Renderers))]
    public void Both_adapters_render_the_invoice_with_visible_content(IPdfRenderer renderer)
    {
        var result = InvoiceSample.Render(renderer);

        result.IsSuccess.Should().BeTrue();
        StartsWithPdfHeader(result.Value.ToArray()).Should().BeTrue();

        var pages = new PdfRasterizer().Rasterize(result.Value.ToArray());
        pages.Should().NotBeEmpty();
        pages[0].InkRatio().Should().BeGreaterThan(0.002d, "the rendered page must not be blank");
    }

    [Theory]
    [MemberData(nameof(Renderers))]
    public void Both_adapters_paginate_the_contract_to_many_pages(IPdfRenderer renderer)
    {
        var result = ContractSample.Render(renderer);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageCount.Should().BeGreaterThan(20);
    }

    [Theory]
    [MemberData(nameof(Renderers))]
    public void Both_adapters_render_the_financial_report_including_its_charts(IPdfRenderer renderer)
    {
        var result = FinancialReportSample.RenderFullReport(renderer);

        result.IsSuccess.Should().BeTrue("charts are now drawn as embedded images");
        result.Value.PageCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public void An_adapters_output_is_visually_identical_to_itself()
    {
        var pdf = InvoiceSample.Render(new QuestPdfRenderer()).Value.ToArray();

        var report = new PdfVisualComparer().Compare(pdf, pdf);

        report.PageCountMatches.Should().BeTrue();
        report.MeanSimilarity.Should().Be(1d);
    }

    [Fact]
    public void The_two_adapters_can_be_compared_and_a_diff_is_produced()
    {
        var quest = InvoiceSample.Render(new QuestPdfRenderer());
        var itext = InvoiceSample.Render(new ITextRenderer());
        quest.IsSuccess.Should().BeTrue();
        itext.IsSuccess.Should().BeTrue();

        var report = new PdfVisualComparer().Compare(quest.Value.ToArray(), itext.Value.ToArray());

        report.Pages.Should().NotBeEmpty();
        report.Pages[0].DiffPng.Should().NotBeEmpty();
        report.MeanSimilarity.Should().BeInRange(0d, 1d);
    }

    private static bool StartsWithPdfHeader(byte[] content) =>
        content.Length >= 5
        && content[0] == '%'
        && content[1] == 'P'
        && content[2] == 'D'
        && content[3] == 'F'
        && content[4] == '-';
}

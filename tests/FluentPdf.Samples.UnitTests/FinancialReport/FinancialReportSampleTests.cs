using System.Text;
using FluentPdf.Application.Rendering;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Kernel;
using FluentPdf.Samples.FinancialReport;

namespace FluentPdf.Samples.UnitTests.FinancialReport;

public sealed class FinancialReportSampleTests
{
    private static string Decode(Result<RenderedPdf> result)
    {
        result.IsSuccess.Should().BeTrue();
        return Encoding.UTF8.GetString(result.Value.ToArray());
    }

    [Fact]
    public void Full_report_renders_successfully_across_several_pages()
    {
        var result = FinancialReportSample.RenderWithInMemoryAdapter();

        result.IsSuccess.Should().BeTrue();
        result.Value.PageCount.Should().BeGreaterThan(1);
        result.Value.ContentType.Should().Be(RenderedPdf.PdfContentType);
    }

    [Fact]
    public void Full_report_includes_every_major_business_block()
    {
        var text = Decode(FinancialReportSample.RenderWithInMemoryAdapter());

        text.Should().Contain("Helios Capital Group");
        text.Should().Contain("1. Executive summary");
        text.Should().Contain("2. Financial statements");
        text.Should().Contain("3. Segment performance");
        text.Should().Contain("4. Risk & governance");
    }

    [Fact]
    public void The_statement_component_is_reused_for_all_three_statements()
    {
        var text = Decode(FinancialReportSample.RenderWithInMemoryAdapter());

        text.Should().Contain("Consolidated income statement");
        text.Should().Contain("Consolidated balance sheet");
        text.Should().Contain("Consolidated cash-flow statement");
    }

    [Fact]
    public void Scorecard_lays_cards_out_as_auto_width_grid_columns()
    {
        var text = Decode(FinancialReportSample.RenderWithInMemoryAdapter());

        // Four KPI cards per row, each an auto column, resolve to 12 / 4 = 3 grid units.
        text.Should().Contain("[col:3]");
        text.Should().Contain("Revenue");
        text.Should().Contain("Free cash flow");
    }

    [Fact]
    public void Full_report_renders_trend_and_segment_mix_charts()
    {
        var text = Decode(FinancialReportSample.RenderWithInMemoryAdapter());

        text.Should().Contain("[chart:Line");
        text.Should().Contain("Revenue & EBITDA trend");
        text.Should().Contain("[chart:Pie");
        text.Should().Contain("Revenue by segment");
        text.Should().Contain("categories: Q1, Q2, Q3, Q4");
    }

    [Fact]
    public void Full_report_includes_segments_risks_and_signatories()
    {
        var text = Decode(FinancialReportSample.RenderWithInMemoryAdapter());

        text.Should().Contain("Asset Management");
        text.Should().Contain("Group total");
        text.Should().Contain("Market volatility");
        text.Should().Contain("Amara Okonkwo");
        text.Should().Contain("Chief Financial Officer");
    }

    [Fact]
    public void Board_one_pager_reuses_the_same_blocks_on_a_single_page()
    {
        var result = FinancialReportSample.RenderBoardOnePager(new InMemoryPdfRenderer());

        result.IsSuccess.Should().BeTrue();
        result.Value.PageCount.Should().Be(1);

        var text = Encoding.UTF8.GetString(result.Value.ToArray());
        text.Should().Contain("at a glance");
        text.Should().Contain("Management commentary");
        text.Should().Contain("Revenue");
    }

    [Fact]
    public void Full_report_template_builds_four_sections_with_metadata()
    {
        var document = new FinancialReportTemplate().Build(FinancialReportSample.SampleData());

        document.IsSuccess.Should().BeTrue();
        document.Value.Sections.Should().HaveCount(4);
        document.Value.Metadata.Title.Should().Contain("Helios Capital Group");
    }
}

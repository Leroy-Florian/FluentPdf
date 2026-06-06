using FluentPdf.Application.Building;
using FluentPdf.Domain.Content;
using FluentPdf.Domain.Styling;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.FinancialReport;

// Each type below is a *reusable business block*: a self-contained mapping from a print DTO
// to agnostic document blocks. They are deliberately small and single-purpose so the same
// instance can be composed into many different documents (see FinancialReportTemplate and
// BoardOnePagerTemplate, which both reuse several of these).

/// <summary>A numbered/titled section heading. Reused before every major section.</summary>
public sealed class SectionHeadingComponent : IBlockComponent<string>
{
    public Result<IReadOnlyList<IBlock>> Build(string title) =>
        BlockComposer.Compose(blocks => blocks
            .Paragraph(p => p.Run(title, ReportStyles.SectionTitle))
            .Spacer(6d));
}

/// <summary>The branded cover block: entity, report title, period and confidentiality.</summary>
public sealed class BrandedCoverComponent : IBlockComponent<CompanyProfile>
{
    public Result<IReadOnlyList<IBlock>> Build(CompanyProfile company) =>
        BlockComposer.Compose(blocks => blocks
            .Spacer(140d)
            .Paragraph(p => p.Run(company.Name, ReportStyles.CoverTitle))
            .Spacer(10d)
            .Paragraph(p => p.Run(company.ReportTitle, ReportStyles.CoverSubtitle))
            .Paragraph(p => p.Run(company.Period, ReportStyles.CoverSubtitle))
            .Spacer(28d)
            .Paragraph(p => p.Run($"Reporting currency: {company.Currency}", ReportStyles.Muted))
            .Paragraph(p => p.Run(company.Confidentiality, ReportStyles.Muted)));
}

/// <summary>The document-control / audit-trail block shown on the cover of any controlled doc.</summary>
public sealed class DocumentControlComponent : IBlockComponent<DocumentControl>
{
    public Result<IReadOnlyList<IBlock>> Build(DocumentControl control) =>
        BlockComposer.Compose(blocks => blocks
            .Spacer(180d)
            .Table(table => table
                .Columns(2)
                .HeaderRow(row => row
                    .Cell("Document control")
                    .Cell("Detail"))
                .Row(row => row.Cell("Version").Cell(control.Version))
                .Row(row => row.Cell("Prepared by").Cell(control.PreparedBy))
                .Row(row => row.Cell("Reviewed by").Cell(control.ReviewedBy))
                .Row(row => row.Cell("Approved by").Cell(control.ApprovedBy))
                .Row(row => row.Cell("Issued on").Cell(ReportFormatting.Date(control.IssuedOn)))));
}

/// <summary>
/// A scorecard of KPI "cards" laid out on the grid, four per row. Each card uses an
/// auto-width column so the cards always share the row evenly, whatever the count.
/// </summary>
public sealed class KpiScorecardComponent : IBlockComponent<IReadOnlyList<Kpi>>
{
    private const int CardsPerRow = 4;

    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<Kpi> kpis) =>
        BlockComposer.Compose(blocks =>
        {
            for (var offset = 0; offset < kpis.Count; offset += CardsPerRow)
            {
                var cards = kpis.Skip(offset).Take(CardsPerRow).ToList();

                blocks
                    .Row(row =>
                    {
                        foreach (var kpi in cards)
                        {
                            row.Column(card => card
                                .Paragraph(p => p.Run(kpi.Label, ReportStyles.Muted))
                                .Paragraph(p => p.Run(
                                    $"{ReportFormatting.Amount(kpi.Current)} {kpi.Unit}",
                                    ReportStyles.KpiValue))
                                .Paragraph(p => p.Run(
                                    $"{ReportFormatting.SignedPercent(kpi.ChangePercent)} YoY",
                                    kpi.IsFavourable ? ReportStyles.Favourable : ReportStyles.Adverse)));
                        }
                    })
                    .Spacer(10d);
            }
        });
}

/// <summary>
/// Renders any two-period <see cref="FinancialStatement"/> as a four-column table. This is the
/// keystone reusable block: the template feeds it the income statement, the balance sheet and
/// the cash-flow statement in turn — one component, three documents-within-a-document.
/// </summary>
public sealed class FinancialStatementComponent : IBlockComponent<FinancialStatement>
{
    public Result<IReadOnlyList<IBlock>> Build(FinancialStatement statement) =>
        BlockComposer.Compose(blocks => blocks
            .Paragraph(p => p.Run(statement.Title, ReportStyles.SubHeading))
            .Spacer(4d)
            .Table(table =>
            {
                table
                    .Columns(4)
                    .HeaderRow(row => row
                        .Cell("Line item")
                        .Cell("Current", HorizontalAlignment.Right)
                        .Cell("Prior", HorizontalAlignment.Right)
                        .Cell("Var %", HorizontalAlignment.Right));

                foreach (var line in statement.Lines)
                {
                    table.Row(row => row
                        .Cell(cell => cell.Paragraph(p => RenderLabel(p, line)))
                        .Cell(cell => cell
                            .Align(HorizontalAlignment.Right)
                            .Paragraph(p => p.Run(ReportFormatting.Amount(line.Current), AmountStyle(line))))
                        .Cell(cell => cell
                            .Align(HorizontalAlignment.Right)
                            .Paragraph(p => p.Run(ReportFormatting.Amount(line.Prior), AmountStyle(line))))
                        .Cell(cell => cell
                            .Align(HorizontalAlignment.Right)
                            .Paragraph(p => p.Run(
                                ReportFormatting.SignedPercent(line.VariancePercent),
                                line.Variance >= 0m ? ReportStyles.Favourable : ReportStyles.Adverse))));
                }
            })
            .Spacer(12d));

    private static void RenderLabel(ParagraphBuilder paragraph, StatementLine line)
    {
        if (line.IsSubtotal)
        {
            paragraph.Bold(line.Label);
        }
        else
        {
            paragraph.Text(line.Label);
        }
    }

    private static TextStyle? AmountStyle(StatementLine line) =>
        line.IsSubtotal ? ReportStyles.Strong : null;
}

/// <summary>A revenue/profit breakdown by operating segment, with a bold totals row.</summary>
public sealed class SegmentBreakdownComponent : IBlockComponent<IReadOnlyList<BusinessSegment>>
{
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<BusinessSegment> segments) =>
        BlockComposer.Compose(blocks => blocks
            .Table(table =>
            {
                table
                    .Columns(5)
                    .HeaderRow(row => row
                        .Cell("Segment")
                        .Cell("Revenue", HorizontalAlignment.Right)
                        .Cell("Op. profit", HorizontalAlignment.Right)
                        .Cell("Margin", HorizontalAlignment.Right)
                        .Cell("Headcount", HorizontalAlignment.Right));

                foreach (var segment in segments)
                {
                    table.Row(row => row
                        .Cell(segment.Name)
                        .Cell(ReportFormatting.Amount(segment.Revenue), HorizontalAlignment.Right)
                        .Cell(ReportFormatting.Amount(segment.OperatingProfit), HorizontalAlignment.Right)
                        .Cell(ReportFormatting.Percent(segment.Margin), HorizontalAlignment.Right)
                        .Cell(ReportFormatting.Count(segment.Headcount), HorizontalAlignment.Right));
                }

                table.Row(row => row
                    .Cell(cell => cell.Paragraph(p => p.Bold("Group total")))
                    .Cell(cell => cell
                        .Align(HorizontalAlignment.Right)
                        .Paragraph(p => p.Bold(ReportFormatting.Amount(segments.Sum(s => s.Revenue)))))
                    .Cell(cell => cell
                        .Align(HorizontalAlignment.Right)
                        .Paragraph(p => p.Bold(ReportFormatting.Amount(segments.Sum(s => s.OperatingProfit)))))
                    .Cell(cell => cell
                        .Align(HorizontalAlignment.Right)
                        .Paragraph(p => p.Bold(ReportFormatting.Percent(GroupMargin(segments)))))
                    .Cell(cell => cell
                        .Align(HorizontalAlignment.Right)
                        .Paragraph(p => p.Bold(ReportFormatting.Count(segments.Sum(s => s.Headcount))))));
            }));

    private static double GroupMargin(IReadOnlyList<BusinessSegment> segments)
    {
        var revenue = segments.Sum(s => s.Revenue);
        return revenue == 0m ? 0d : (double)(segments.Sum(s => s.OperatingProfit) / revenue) * 100d;
    }
}

/// <summary>Management commentary: a heading, narrative paragraphs and bullet highlights.</summary>
public sealed class CommentaryComponent : IBlockComponent<ManagementCommentary>
{
    public Result<IReadOnlyList<IBlock>> Build(ManagementCommentary commentary) =>
        BlockComposer.Compose(blocks =>
        {
            blocks
                .Paragraph(p => p.Run(commentary.Heading, ReportStyles.SubHeading))
                .Spacer(4d);

            foreach (var paragraph in commentary.Paragraphs)
            {
                blocks.Paragraph(paragraph);
            }

            if (commentary.Highlights.Count > 0)
            {
                blocks
                    .Spacer(6d)
                    .Paragraph(p => p.Run("Highlights", ReportStyles.Strong))
                    .UnorderedList(list =>
                    {
                        foreach (var highlight in commentary.Highlights)
                        {
                            list.Item(highlight);
                        }
                    });
            }
        });
}

/// <summary>
/// A line chart of the rolling revenue and EBITDA trend. Charts are agnostic descriptions —
/// categories and numeric series — so the same block renders through any charting adapter.
/// </summary>
public sealed class TrendChartComponent : IBlockComponent<IReadOnlyList<QuarterlyResult>>
{
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<QuarterlyResult> trend) =>
        BlockComposer.Compose(blocks => blocks
            .Chart(chart => chart
                .Line()
                .Title("Revenue & EBITDA trend")
                .Size(520d, 240d)
                .Categories([.. trend.Select(point => point.Quarter)])
                .Series("Revenue", trend.Select(point => (double)point.Revenue))
                .Series("EBITDA", trend.Select(point => (double)point.Ebitda)))
            .Spacer(12d));
}

/// <summary>A pie chart of the revenue mix by operating segment.</summary>
public sealed class SegmentMixChartComponent : IBlockComponent<IReadOnlyList<BusinessSegment>>
{
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<BusinessSegment> segments) =>
        BlockComposer.Compose(blocks => blocks
            .Chart(chart => chart
                .Pie()
                .Title("Revenue by segment")
                .Size(320d, 320d)
                .Categories([.. segments.Select(segment => segment.Name)])
                .Series("Revenue", segments.Select(segment => (double)segment.Revenue)))
            .Spacer(12d));
}

/// <summary>The risk register: one row per risk with likelihood, impact and mitigation.</summary>
public sealed class RiskRegisterComponent : IBlockComponent<IReadOnlyList<RiskEntry>>
{
    public Result<IReadOnlyList<IBlock>> Build(IReadOnlyList<RiskEntry> risks) =>
        BlockComposer.Compose(blocks => blocks
            .Table(table =>
            {
                table
                    .Columns(4)
                    .HeaderRow(row => row
                        .Cell("Risk")
                        .Cell("Likelihood")
                        .Cell("Impact")
                        .Cell("Mitigation"));

                foreach (var risk in risks)
                {
                    table.Row(row => row
                        .Cell(risk.Title)
                        .Cell(risk.Likelihood)
                        .Cell(risk.Impact)
                        .Cell(risk.Mitigation));
                }
            }));
}

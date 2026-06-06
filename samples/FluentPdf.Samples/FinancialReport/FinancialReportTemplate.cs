using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Layout;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// Assembles the full quarterly financial report from a <see cref="FinancialReportDto"/>. The
/// template owns layout and flow (sections, page sizes, running furniture) and delegates all
/// content to reusable business blocks — the same component instances are composed several
/// times over (notably <see cref="FinancialStatementComponent"/>, fed three different
/// statements, and <see cref="SectionHeadingComponent"/>, reused before every section).
/// </summary>
public sealed class FinancialReportTemplate : IDocumentTemplate<FinancialReportDto>
{
    private readonly SectionHeadingComponent _heading = new();
    private readonly BrandedCoverComponent _cover = new();
    private readonly DocumentControlComponent _control = new();
    private readonly KpiScorecardComponent _scorecard = new();
    private readonly TrendChartComponent _trend = new();
    private readonly SegmentMixChartComponent _segmentMix = new();
    private readonly FinancialStatementComponent _statement = new();
    private readonly SegmentBreakdownComponent _segments = new();
    private readonly CommentaryComponent _commentary = new();
    private readonly RiskRegisterComponent _risks = new();

    public Result<PdfDocument> Build(FinancialReportDto report)
    {
        var company = report.Company;

        return PdfDocumentBuilder.Create()
            .Metadata(meta => meta
                .Title($"{company.Name} — {company.ReportTitle}")
                .Author(company.Name)
                .Subject(company.Period)
                .Creator("FluentPdf")
                .Keywords("financial report", company.Period, company.Currency))

            // 1 — Cover page: branding plus the document-control audit trail.
            .Section(cover => cover
                .PageSize(PageSize.A4)
                .Margins(64d)
                .Component(_cover, company)
                .Component(_control, report.Control))

            // 2 — Executive summary: KPI scorecard and management commentary.
            .Section(summary => RunningPage(summary, company)
                .Component(_heading, "1. Executive summary")
                .Component(_scorecard, report.Kpis)
                .Component(_trend, report.Trend)
                .Component(_commentary, report.Commentary))

            // 3 — Financial statements: landscape so the wide variance tables breathe. The
            //     same statement component renders all three statements.
            .Section(statements => RunningPage(statements, company)
                .Landscape()
                .Component(_heading, "2. Financial statements")
                .Component(_statement, report.IncomeStatement)
                .Component(_statement, report.BalanceSheet)
                .Component(_statement, report.CashFlow)
                .PageBreak()
                .Component(_heading, "3. Segment performance")
                .Component(_segments, report.Segments)
                .Component(_segmentMix, report.Segments))

            // 4 — Risk register and sign-off. The sign-off is composed inline as a delegate
            //     component, showing the lightweight alternative to a dedicated class.
            .Section(governance => RunningPage(governance, company)
                .Component(_heading, "4. Risk & governance")
                .Component(_risks, report.Risks)
                .Spacer(16d)
                .Component(report.Signatories, BuildSignOff))

            .Build();
    }

    /// <summary>Applies the shared A4 layout and running header/footer to a content section.</summary>
    private static SectionBuilder RunningPage(SectionBuilder section, CompanyProfile company) =>
        section
            .PageSize(PageSize.A4)
            .Margins(56d)
            .Header(header => header.Paragraph(p => p.Run(
                $"{company.Name} — {company.Period}",
                ReportStyles.Muted)))
            .Footer(footer => footer.Paragraph(p => p.Run(
                $"{company.Confidentiality} · Figures in {company.Currency}",
                ReportStyles.Muted)));

    /// <summary>An inline delegate component: the signature block and statutory disclaimer.</summary>
    private static Result<IReadOnlyList<Domain.Content.IBlock>> BuildSignOff(
        IReadOnlyList<Signatory> signatories) =>
        BlockComposer.Compose(blocks =>
        {
            blocks.Paragraph(p => p.Run("Approved by", ReportStyles.SubHeading));

            foreach (var signatory in signatories)
            {
                blocks.Paragraph(p => p
                    .Bold(signatory.Name)
                    .Text($" — {signatory.Role}"));
            }

            blocks
                .Spacer(10d)
                .Paragraph(p => p.Run(
                    "This report is prepared for information purposes and does not constitute "
                    + "audited financial statements.",
                    ReportStyles.Muted));
        });
}

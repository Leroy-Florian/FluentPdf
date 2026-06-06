using FluentPdf.Application.Building;
using FluentPdf.Domain;
using FluentPdf.Domain.Layout;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// A condensed, single-page board briefing built from the <em>same</em> DTO and the
/// <em>same</em> reusable blocks as <see cref="FinancialReportTemplate"/>. It exists to prove
/// the point: a business block (KPI scorecard, commentary, section heading) is authored once
/// and composed into as many different documents as the application needs.
/// </summary>
public sealed class BoardOnePagerTemplate : IDocumentTemplate<FinancialReportDto>
{
    private readonly SectionHeadingComponent _heading = new();
    private readonly KpiScorecardComponent _scorecard = new();
    private readonly CommentaryComponent _commentary = new();

    public Result<PdfDocument> Build(FinancialReportDto report) =>
        PdfDocumentBuilder.Create()
            .Metadata(meta => meta
                .Title($"{report.Company.Name} — Board briefing")
                .Author(report.Company.Name)
                .Subject(report.Company.Period))
            .Section(page => page
                .PageSize(PageSize.A4)
                .Margins(56d)
                .Header(header => header.Paragraph(p => p.Run(
                    $"Board briefing — {report.Company.Period}",
                    ReportStyles.Muted)))
                .Component(_heading, $"{report.Company.Name} — at a glance")
                .Component(_scorecard, report.Kpis)
                .Component(_commentary, report.Commentary))
            .Build();
}

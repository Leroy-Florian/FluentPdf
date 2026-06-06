using FluentPdf.Application.Rendering;
using FluentPdf.Application.UseCases;
using FluentPdf.Domain;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Kernel;

namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// End-to-end usage of the financial-report sample. Builds a realistic DTO once, then renders
/// it both as the full quarterly report and as a one-page board briefing — through the same
/// adapter, proving the blocks and DTO are shared across documents. Swapping
/// <see cref="InMemoryPdfRenderer"/> for an iText/QuestPDF adapter is the only change needed
/// to emit real PDF bytes; templates and components stay untouched.
/// </summary>
public static class FinancialReportSample
{
    /// <summary>Renders the full quarterly report with the built-in in-memory adapter.</summary>
    public static Result<RenderedPdf> RenderFullReport(IPdfRenderer renderer) =>
        Render(renderer, new FinancialReportTemplate().Build(SampleData()));

    /// <summary>Renders the condensed board one-pager with the built-in in-memory adapter.</summary>
    public static Result<RenderedPdf> RenderBoardOnePager(IPdfRenderer renderer) =>
        Render(renderer, new BoardOnePagerTemplate().Build(SampleData()));

    /// <summary>Renders the full report with the reference in-memory adapter.</summary>
    public static Result<RenderedPdf> RenderWithInMemoryAdapter() =>
        RenderFullReport(new InMemoryPdfRenderer());

    private static Result<RenderedPdf> Render(IPdfRenderer renderer, Result<PdfDocument> document)
    {
        if (document.IsFailure)
        {
            return document.Error;
        }

        // The use case checks the adapter advertises every feature the document uses before
        // rendering, so unsupported content fails fast rather than being silently dropped.
        return new RenderDocumentUseCase(renderer).Execute(document.Value);
    }

    /// <summary>A realistic, fully-populated sample DTO for "Helios Capital Group, FY25 Q4".</summary>
    public static FinancialReportDto SampleData() => new(
        Company: new CompanyProfile(
            Name: "Helios Capital Group",
            ReportTitle: "Quarterly Financial Report",
            Period: "FY2025 — Q4 (three months ended 31 December 2025)",
            Currency: "EUR thousands",
            Confidentiality: "Strictly confidential — Board distribution only"),
        Control: new DocumentControl(
            Version: "v3.1 (final)",
            PreparedBy: "Group FP&A",
            ReviewedBy: "Group Financial Controller",
            ApprovedBy: "Chief Financial Officer",
            IssuedOn: new DateOnly(2026, 1, 28)),
        Kpis:
        [
            new Kpi("Revenue", 482_300m, 451_900m, "EUR k", HigherIsBetter: true),
            new Kpi("EBITDA", 118_540m, 104_220m, "EUR k", HigherIsBetter: true),
            new Kpi("Net income", 61_180m, 57_640m, "EUR k", HigherIsBetter: true),
            new Kpi("Free cash flow", 73_900m, 69_410m, "EUR k", HigherIsBetter: true),
            new Kpi("Net debt", 142_700m, 158_300m, "EUR k", HigherIsBetter: false),
            new Kpi("Operating margin", 24m, 22m, "%", HigherIsBetter: true),
            new Kpi("Headcount", 3_240m, 3_115m, "FTE", HigherIsBetter: true),
            new Kpi("DSO", 47m, 52m, "days", HigherIsBetter: false),
        ],
        IncomeStatement: new FinancialStatement("Consolidated income statement",
        [
            new StatementLine("Revenue", 482_300m, 451_900m),
            new StatementLine("Cost of sales", -276_140m, -262_510m),
            new StatementLine("Gross profit", 206_160m, 189_390m, IsSubtotal: true),
            new StatementLine("Operating expenses", -87_620m, -85_170m),
            new StatementLine("EBITDA", 118_540m, 104_220m, IsSubtotal: true),
            new StatementLine("Depreciation & amortisation", -29_410m, -27_980m),
            new StatementLine("Operating profit (EBIT)", 89_130m, 76_240m, IsSubtotal: true),
            new StatementLine("Net finance costs", -8_950m, -9_840m),
            new StatementLine("Profit before tax", 80_180m, 66_400m, IsSubtotal: true),
            new StatementLine("Income tax", -19_000m, -8_760m),
            new StatementLine("Net income", 61_180m, 57_640m, IsSubtotal: true),
        ]),
        BalanceSheet: new FinancialStatement("Consolidated balance sheet",
        [
            new StatementLine("Property, plant & equipment", 214_600m, 207_300m),
            new StatementLine("Goodwill & intangibles", 168_900m, 171_200m),
            new StatementLine("Non-current assets", 383_500m, 378_500m, IsSubtotal: true),
            new StatementLine("Inventories", 64_300m, 61_900m),
            new StatementLine("Trade receivables", 98_700m, 102_400m),
            new StatementLine("Cash & equivalents", 121_400m, 96_800m),
            new StatementLine("Current assets", 284_400m, 261_100m, IsSubtotal: true),
            new StatementLine("Total assets", 667_900m, 639_600m, IsSubtotal: true),
            new StatementLine("Total equity", 351_500m, 318_900m, IsSubtotal: true),
            new StatementLine("Borrowings", 264_100m, 255_100m),
            new StatementLine("Other liabilities", 52_300m, 65_600m),
            new StatementLine("Total equity & liabilities", 667_900m, 639_600m, IsSubtotal: true),
        ]),
        CashFlow: new FinancialStatement("Consolidated cash-flow statement",
        [
            new StatementLine("Cash from operations", 112_300m, 101_700m),
            new StatementLine("Capital expenditure", -38_400m, -32_290m),
            new StatementLine("Free cash flow", 73_900m, 69_410m, IsSubtotal: true),
            new StatementLine("Net financing cash flow", -49_300m, -55_120m),
            new StatementLine("Net change in cash", 24_600m, 14_290m, IsSubtotal: true),
        ]),
        Segments:
        [
            new BusinessSegment("Asset Management", 198_400m, 61_300m, 980),
            new BusinessSegment("Private Banking", 154_700m, 38_900m, 1_240),
            new BusinessSegment("Capital Markets", 96_200m, 21_400m, 720),
            new BusinessSegment("Insurance Solutions", 33_000m, 4_530m, 300),
        ],
        Commentary: new ManagementCommentary(
            Heading: "Management commentary",
            Paragraphs:
            [
                "Group revenue grew 6.7% year on year, driven by net new assets in Asset "
                + "Management and resilient fee margins across Private Banking. Reported EBITDA "
                + "rose 13.7%, with operating leverage from the cost-efficiency programme "
                + "launched in H1.",
                "Net debt fell to EUR 142.7m as strong free cash flow funded both the interim "
                + "dividend and continued deleveraging. The Board reaffirms full-year guidance.",
            ],
            Highlights:
            [
                "Net new money of EUR 4.1bn in Asset Management, a record quarter.",
                "Cost-income ratio improved 180bps to 61.2%.",
                "Liquidity coverage ratio comfortably above regulatory minimums at 143%.",
            ]),
        Risks:
        [
            new RiskEntry(
                "Market volatility",
                "Medium",
                "High",
                "Diversified revenue mix; hedging of seed capital; stress-tested limits."),
            new RiskEntry(
                "Credit deterioration",
                "Low",
                "High",
                "Conservative underwriting; collateralised lending; weekly watchlist review."),
            new RiskEntry(
                "Regulatory change",
                "Medium",
                "Medium",
                "Dedicated change programme; early engagement with supervisors."),
            new RiskEntry(
                "Cyber & operational",
                "Medium",
                "High",
                "Zero-trust architecture; 24/7 SOC; annual red-team exercises."),
        ],
        Signatories:
        [
            new Signatory("Amara Okonkwo", "Chief Financial Officer"),
            new Signatory("Lucas Berger", "Group Financial Controller"),
        ]);
}

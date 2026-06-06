namespace FluentPdf.Samples.FinancialReport;

/// <summary>
/// The complete print DTO an application hands to the financial-report template. It is a pure
/// data contract — no rendering concerns leak into it — assembled from focused sub-DTOs that
/// each map to a reusable presentation block.
/// </summary>
public sealed record FinancialReportDto(
    CompanyProfile Company,
    DocumentControl Control,
    IReadOnlyList<Kpi> Kpis,
    FinancialStatement IncomeStatement,
    FinancialStatement BalanceSheet,
    FinancialStatement CashFlow,
    IReadOnlyList<BusinessSegment> Segments,
    IReadOnlyList<QuarterlyResult> Trend,
    ManagementCommentary Commentary,
    IReadOnlyList<RiskEntry> Risks,
    IReadOnlyList<Signatory> Signatories);

/// <summary>Identifies the reporting entity and the period covered.</summary>
public sealed record CompanyProfile(
    string Name,
    string ReportTitle,
    string Period,
    string Currency,
    string Confidentiality);

/// <summary>Audit trail metadata shown on the cover of any controlled corporate document.</summary>
public sealed record DocumentControl(
    string Version,
    string PreparedBy,
    string ReviewedBy,
    string ApprovedBy,
    DateOnly IssuedOn);

/// <summary>A single headline metric with its prior-period comparator.</summary>
public sealed record Kpi(string Label, decimal Current, decimal Prior, string Unit, bool HigherIsBetter)
{
    /// <summary>The absolute change versus the prior period.</summary>
    public decimal Delta => Current - Prior;

    /// <summary>The relative change versus the prior period, as a percentage.</summary>
    public double ChangePercent =>
        Prior == 0m ? 0d : (double)((Current - Prior) / Math.Abs(Prior)) * 100d;

    /// <summary>Whether the movement is favourable given the metric's polarity.</summary>
    public bool IsFavourable => HigherIsBetter ? Current >= Prior : Current <= Prior;
}

/// <summary>A generic two-period financial statement (P&amp;L, balance sheet or cash flow).</summary>
public sealed record FinancialStatement(string Title, IReadOnlyList<StatementLine> Lines);

/// <summary>A single line of a <see cref="FinancialStatement"/>, optionally a subtotal.</summary>
public sealed record StatementLine(string Label, decimal Current, decimal Prior, bool IsSubtotal = false)
{
    /// <summary>The absolute variance between the two periods.</summary>
    public decimal Variance => Current - Prior;

    /// <summary>The relative variance between the two periods, as a percentage.</summary>
    public double VariancePercent =>
        Prior == 0m ? 0d : (double)((Current - Prior) / Math.Abs(Prior)) * 100d;
}

/// <summary>One quarter of the rolling revenue/EBITDA trend.</summary>
public sealed record QuarterlyResult(string Quarter, decimal Revenue, decimal Ebitda);

/// <summary>Performance of a single operating segment.</summary>
public sealed record BusinessSegment(string Name, decimal Revenue, decimal OperatingProfit, int Headcount)
{
    /// <summary>The operating margin, as a percentage of revenue.</summary>
    public double Margin => Revenue == 0m ? 0d : (double)(OperatingProfit / Revenue) * 100d;
}

/// <summary>Narrative management commentary with a set of pull-out highlights.</summary>
public sealed record ManagementCommentary(
    string Heading,
    IReadOnlyList<string> Paragraphs,
    IReadOnlyList<string> Highlights);

/// <summary>A single entry of the risk register.</summary>
public sealed record RiskEntry(string Title, string Likelihood, string Impact, string Mitigation);

/// <summary>A person who signs off the report.</summary>
public sealed record Signatory(string Name, string Role);

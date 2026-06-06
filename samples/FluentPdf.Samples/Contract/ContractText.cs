using System.Globalization;

namespace FluentPdf.Samples.Contract;

/// <summary>
/// Builds a realistic, long-form sample facility agreement. The body is generated from a pool
/// of genuine-sounding clause prose so the document spans 30-40 pages and exercises automatic
/// pagination (paragraph splitting across page boundaries) on real text volume.
/// </summary>
public static class ContractText
{
    private const int ClausesPerArticle = 11;

    private static readonly string[] ArticleTitles =
    [
        "Definitions and Interpretation",
        "The Facility",
        "Purpose and Application of Proceeds",
        "Conditions Precedent",
        "Drawdown and Availability",
        "Representations and Warranties",
        "Information Undertakings",
        "General Undertakings",
        "Financial Covenants",
        "Interest and Interest Periods",
        "Repayment and Amortisation",
        "Prepayment and Cancellation",
        "Fees, Costs and Expenses",
        "Taxes and Gross-Up",
        "Increased Costs and Market Disruption",
        "Events of Default",
        "Indemnities and Mitigation",
        "Confidentiality, Assignment and Governing Law",
    ];

    private static readonly string[] ClausePool =
    [
        "The Borrower shall apply all amounts borrowed by it under the Facility towards the "
        + "purposes specified in the relevant Utilisation Request and shall not, without the "
        + "prior written consent of the Lender, apply any part of those amounts for any other "
        + "purpose. The Lender is not obliged to monitor or verify the application of any amount "
        + "borrowed pursuant to this Agreement, and no failure by the Lender to monitor such "
        + "application shall relieve the Borrower of any of its obligations under the Finance "
        + "Documents or constitute a waiver of any right or remedy of the Lender.",

        "Each representation and warranty set out in this Article is made by the Borrower on the "
        + "date of this Agreement and is deemed to be repeated by the Borrower on the date of each "
        + "Utilisation Request, on each Utilisation Date and on the first day of each Interest "
        + "Period, in each case by reference to the facts and circumstances then existing. If any "
        + "such representation or warranty is found to have been incorrect or misleading in any "
        + "material respect when made or deemed to be made, that occurrence shall constitute an "
        + "Event of Default, subject to any applicable grace period expressly provided herein.",

        "The Borrower shall ensure that at all times the aggregate principal amount of its "
        + "Financial Indebtedness does not exceed the limits agreed between the parties and shall "
        + "maintain the financial ratios described in Schedule 2, tested on each Quarter Date by "
        + "reference to the most recent financial statements delivered under the information "
        + "undertakings. The Borrower shall promptly notify the Lender upon becoming aware of any "
        + "breach or anticipated breach of any such ratio, together with reasonable detail of the "
        + "steps it proposes to take to remedy that breach within the cure period permitted.",

        "Interest shall accrue on each Loan from day to day at the applicable rate, calculated on "
        + "the basis of the actual number of days elapsed and a year of three hundred and sixty "
        + "days, and shall be payable in arrear on the last day of each Interest Period. If the "
        + "Borrower fails to pay any amount payable by it under a Finance Document on its due date, "
        + "default interest shall accrue on the overdue amount from the due date up to the date of "
        + "actual payment, both before and after judgment, at a rate determined by the Lender to "
        + "reflect the cost to it of funding the overdue amount plus the agreed default margin.",

        "All payments to be made by the Borrower under the Finance Documents shall be made free "
        + "and clear of, and without any deduction or withholding for or on account of, any Tax "
        + "unless such deduction or withholding is required by law. If any such deduction or "
        + "withholding is so required, the Borrower shall pay an additional amount so that the net "
        + "amount received by the Lender equals the amount it would have received had no such "
        + "deduction or withholding been made, and the Borrower shall deliver to the Lender "
        + "evidence reasonably satisfactory to it that the relevant deduction has been remitted.",

        "Without prejudice to any other rights of the Lender, if an Event of Default is continuing "
        + "the Lender may, by notice to the Borrower, cancel any undrawn portion of the Facility "
        + "and declare that all or part of the Loans, together with accrued interest and all other "
        + "amounts accrued or outstanding under the Finance Documents, be immediately due and "
        + "payable, whereupon they shall become so due and payable. Any such notice shall take "
        + "effect in accordance with its terms and shall not require any further demand, protest "
        + "or notice of any kind, all of which are expressly waived by the Borrower.",

        "The Borrower shall supply to the Lender its audited consolidated financial statements "
        + "within one hundred and twenty days after the end of each of its financial years and its "
        + "unaudited management accounts within sixty days after the end of each financial quarter, "
        + "in each case prepared in accordance with the accounting principles consistently applied "
        + "in the preparation of the original financial statements. Together with each set of "
        + "financial statements the Borrower shall deliver a compliance certificate signed by two "
        + "authorised signatories setting out computations of the financial covenants in reasonable detail.",

        "This Agreement and any non-contractual obligations arising out of or in connection with it "
        + "are governed by the laws of the relevant jurisdiction agreed between the parties. The "
        + "parties irrevocably agree that the courts of that jurisdiction have exclusive "
        + "jurisdiction to settle any dispute arising out of or in connection with this Agreement, "
        + "including a dispute regarding its existence, validity or termination. Nothing in this "
        + "clause shall limit the right of the Lender to take proceedings against the Borrower in "
        + "any other court of competent jurisdiction, nor shall the taking of proceedings in one or "
        + "more jurisdictions preclude the taking of proceedings in any other jurisdiction.",
    ];

    /// <summary>Builds the worked sample agreement: "Project Helios Senior Facility Agreement".</summary>
    public static ContractDto Sample()
    {
        var articles = new List<ContractArticle>(ArticleTitles.Length);

        for (var a = 0; a < ArticleTitles.Length; a++)
        {
            var clauses = new List<string>(ClausesPerArticle);
            for (var c = 0; c < ClausesPerArticle; c++)
            {
                clauses.Add(ClausePool[(a + c) % ClausePool.Length]);
            }

            articles.Add(new ContractArticle(ArticleTitles[a], clauses));
        }

        return new ContractDto(
            Title: "Senior Secured Facility Agreement",
            Reference: "Ref. HCG/2026/SF-014",
            Date: new DateOnly(2026, 1, 31),
            Currency: "EUR",
            FacilityAmount: 250_000_000m,
            Lender: new Party(
                "Helios Capital Group plc",
                "Lender",
                "1 Solaris Square, London EC2N 1AR, United Kingdom"),
            Borrower: new Party(
                "Meridian Energy Holdings S.A.",
                "Borrower",
                "12 Boulevard Royal, L-2449 Luxembourg"),
            Recitals:
            [
                "The Lender has agreed to make available to the Borrower a senior secured term "
                + "facility in an aggregate principal amount not exceeding "
                + Amount(250_000_000m) + " on the terms and subject to the conditions set out in "
                + "this Agreement.",
                "The Borrower intends to apply the proceeds of the Facility towards the refinancing "
                + "of existing indebtedness and the general corporate purposes of the Group.",
                "The parties have agreed that the obligations of the Borrower under the Finance "
                + "Documents shall be secured by the Security described in the Security Documents.",
            ],
            Articles: articles,
            Signatories:
            [
                new Party("Helios Capital Group plc", "For and on behalf of the Lender", string.Empty),
                new Party("Meridian Energy Holdings S.A.", "For and on behalf of the Borrower", string.Empty),
            ]);
    }

    private static string Amount(decimal value) =>
        "EUR " + value.ToString("#,##0", CultureInfo.InvariantCulture);
}

// ════════════════════════════════════════════════════════════════════
// BLINKIT — add the 8 new measures + the inactive registration relationship
// Tool: Tabular Editor 2 (free) or 3, connected to the open .pbix.
//
// RUN:
//   1. Open my_first_powerbi.pbix in Power BI Desktop.
//   2. External Tools → Tabular Editor (install free version if missing).
//   3. Paste this whole file into the C# Script tab → Run (F5).
//   4. Ctrl+S to push back to Power BI.
//
// Idempotent: re-running replaces the measures / skips an existing relationship.
// NOTE: your measure table is "Measure Table". If yours is named differently,
//       change TARGET_TABLE below.
// ════════════════════════════════════════════════════════════════════

var TARGET_TABLE = "Measure Table";

var T = Model.Tables.FirstOrDefault(x => x.Name == TARGET_TABLE);
if (T == null) { Error("Table '" + TARGET_TABLE + "' not found — edit TARGET_TABLE."); return; }

System.Action<string,string,string,string> M = (name, expr, fmt, folder) =>
{
    var ex = T.Measures.FirstOrDefault(x => x.Name == name);
    if (ex != null) ex.Delete();
    var m = T.AddMeasure(name, expr);
    if (!string.IsNullOrEmpty(fmt))    m.FormatString  = fmt;
    if (!string.IsNullOrEmpty(folder)) m.DisplayFolder = folder;
};

// ── Core / customers ──────────────────────────────────────────────────
M("Active Customers", @"DISTINCTCOUNT ( Orders[customer_id] )", "#,##0", "01 Core");

M("Repeat Customers",
  @"COUNTROWS ( FILTER ( VALUES ( Orders[customer_id] ), CALCULATE ( DISTINCTCOUNT ( Orders[order_id] ) ) >= 2 ) )",
  "#,##0", "04 Customer");

M("Repeat Rate %",      @"DIVIDE ( [Repeat Customers], [Active Customers] )", "0.0%",    "04 Customer");
M("Orders per Customer",@"DIVIDE ( [Total Orders], [Active Customers] )",     "#,##0.00","04 Customer");

// ── Profitability ────────────────────────────────────────────────────
M("Est Gross Margin",
  @"SUMX ( 'Order Items', 'Order Items'[quantity] * 'Order Items'[unit_price] * RELATED ( Products[margin_percentage] ) / 100 )",
  "₹#,##0", "02 Items");

M("Gross Margin %", @"DIVIDE ( [Est Gross Margin], [Item Revenue] )", "0.0%", "02 Items");

// ── Marketing / inventory ────────────────────────────────────────────
M("CAC",          @"DIVIDE ( [Total Spend], [Total Conversions] )",                 "₹#,##0", "06 Marketing");
M("Good Stock %", @"DIVIDE ( [Stock Received] - [Damaged Stock], [Stock Received] )","0.0%",  "05 Inventory");

// ── Inactive relationship for New Customers (registration_date → Date) ─
// Requires Customers[registration_date] to be Date type.
var fromCol = Model.Tables["Customers"].Columns["registration_date"];
var toCol   = Model.Tables["Date"].Columns["Date"];
bool exists = Model.Relationships.Any(r => r.FromColumn == fromCol && r.ToColumn == toCol);
if (!exists)
{
    var rel = Model.AddRelationship();
    rel.FromColumn = fromCol;     // many side
    rel.ToColumn   = toCol;       // one side
    rel.IsActive   = false;       // keep inactive; Orders→Date stays the active path
    Info("Created INACTIVE relationship Customers[registration_date] → Date[Date].");
}
else { Info("Registration relationship already exists — skipped."); }

// Optional New Customers measure (uses the inactive relationship)
M("New Customers",
  @"CALCULATE ( DISTINCTCOUNT ( Customers[customer_id] ), USERELATIONSHIP ( Customers[registration_date], 'Date'[Date] ) )",
  "#,##0", "01 Core");

Info("Done. 9 measures + 1 relationship. Press Ctrl+S to save back to Power BI.");

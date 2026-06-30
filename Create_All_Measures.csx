// ════════════════════════════════════════════════════════════════════
// BLINKIT — create ALL 36 measures in one run
// Tool: Tabular Editor 2 (free) or 3.  Prereqs below.
//
// HOW TO RUN:
//   1. Open my_first_powerbi.pbix in Power BI Desktop (leave it open).
//   2. Make sure a table named exactly "Measures" exists in the model.
//   3. Open Tabular Editor → it auto-connects to the running Desktop model
//      (or: External Tools ribbon → Tabular Editor).
//   4. Paste this whole file into the "C# Script" tab → Run (▶ / F5).
//   5. Ctrl+S in Tabular Editor to push the measures back into Power BI.
//
// Idempotent: re-running replaces existing measures of the same name.
// ════════════════════════════════════════════════════════════════════

var T = Model.Tables["Measures"];

System.Action<string,string,string,string> M = (name, expr, fmt, folder) =>
{
    var ex = T.Measures.FirstOrDefault(x => x.Name == name);
    if (ex != null) ex.Delete();
    var m = T.AddMeasure(name, expr);
    if (!string.IsNullOrEmpty(fmt))    m.FormatString  = fmt;
    if (!string.IsNullOrEmpty(folder)) m.DisplayFolder = folder;
};

// ── 01 Core ──────────────────────────────────────────────────────────
M("Total Revenue",        @"SUM ( Orders[order_total] )",                       "₹#,##0",    "01 Core");
M("Total Orders",         @"DISTINCTCOUNT ( Orders[order_id] )",                "#,##0",          "01 Core");
M("Total Customers",      @"DISTINCTCOUNT ( Customers[customer_id] )",          "#,##0",          "01 Core");
M("Average Order Value",  @"DIVIDE ( [Total Revenue], [Total Orders] )",        "₹#,##0.00", "01 Core");

// ── 02 Items (category / product splits only) ────────────────────────
M("Item Revenue",         @"SUMX ( 'Order Items', 'Order Items'[quantity] * 'Order Items'[unit_price] )", "₹#,##0", "02 Items");
M("Total Units",          @"SUM ( 'Order Items'[quantity] )",                   "#,##0",          "02 Items");
M("Avg Selling Price",    @"DIVIDE ( [Item Revenue], [Total Units] )",          "₹#,##0.00", "02 Items");

// ── 03 Delivery ──────────────────────────────────────────────────────
M("Total Deliveries",     @"COUNTROWS ( 'Delivery Performance' )",              "#,##0",          "03 Delivery");
M("On Time Deliveries",   @"CALCULATE ( COUNTROWS ( 'Delivery Performance' ), 'Delivery Performance'[delivery_status] = ""On Time"" )", "#,##0", "03 Delivery");
M("On-Time Delivery %",   @"DIVIDE ( [On Time Deliveries], [Total Deliveries], 0 )", "0.0%",      "03 Delivery");
M("Delayed Orders",       @"CALCULATE ( COUNTROWS ( 'Delivery Performance' ), 'Delivery Performance'[delivery_status] <> ""On Time"" )", "#,##0", "03 Delivery");
M("Delay Rate %",         @"DIVIDE ( [Delayed Orders], [Total Deliveries], 0 )", "0.0%",          "03 Delivery");
M("Avg Delivery Time (min)",     @"AVERAGE ( 'Delivery Performance'[delivery_time_minutes] )", "#,##0.0",  "03 Delivery");
M("Avg Delivery Distance (km)",  @"AVERAGE ( 'Delivery Performance'[distance_km] )",           "#,##0.00", "03 Delivery");

// ── 04 Customer & Feedback ───────────────────────────────────────────
M("Average Rating",       @"AVERAGE ( 'Customer Feedback'[rating] )",           "#,##0.00",       "04 Customer");
M("Total Feedback",       @"COUNTROWS ( 'Customer Feedback' )",                 "#,##0",          "04 Customer");
M("Positive Feedback",    @"CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = ""Positive"" )", "#,##0", "04 Customer");
M("Negative Feedback",    @"CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = ""Negative"" )", "#,##0", "04 Customer");
M("Positive Sentiment %", @"DIVIDE ( [Positive Feedback], [Total Feedback], 0 )", "0.0%",         "04 Customer");
M("% 5-Star",             @"DIVIDE ( CALCULATE ( [Total Feedback], 'Customer Feedback'[rating] = 5 ), [Total Feedback], 0 )", "0.0%", "04 Customer");

// ── 05 Inventory ─────────────────────────────────────────────────────
M("Stock Received",       @"SUM ( Inventory[stock_received] )",                 "#,##0",          "05 Inventory");
M("Damaged Stock",        @"SUM ( Inventory[damaged_stock] )",                  "#,##0",          "05 Inventory");
M("Damaged Stock %",      @"DIVIDE ( [Damaged Stock], [Stock Received], 0 )",   "0.0%",           "05 Inventory");
M("Product Count",        @"DISTINCTCOUNT ( Products[product_id] )",            "#,##0",          "05 Inventory");

// ── 06 Marketing (disconnected table) ────────────────────────────────
M("Total Spend",          @"SUM ( 'Marketing Performance'[spend] )",            "₹#,##0",    "06 Marketing");
M("Marketing Revenue",    @"SUM ( 'Marketing Performance'[revenue_generated] )", "₹#,##0",   "06 Marketing");
M("Total Impressions",    @"SUM ( 'Marketing Performance'[impressions] )",      "#,##0",          "06 Marketing");
M("Total Clicks",         @"SUM ( 'Marketing Performance'[clicks] )",           "#,##0",          "06 Marketing");
M("Total Conversions",    @"SUM ( 'Marketing Performance'[conversions] )",      "#,##0",          "06 Marketing");
M("CTR %",                @"DIVIDE ( [Total Clicks], [Total Impressions], 0 )", "0.0%",           "06 Marketing");
M("Conversion Rate %",    @"DIVIDE ( [Total Conversions], [Total Clicks], 0 )", "0.0%",           "06 Marketing");
M("ROAS",                 @"DIVIDE ( [Marketing Revenue], [Total Spend], 0 )",  "#,##0.00",       "06 Marketing");

// ── 07 Time Intelligence (needs Date marked as a date table) ─────────
M("Revenue PM",     @"CALCULATE ( [Total Revenue], DATEADD ( 'Date'[Date], -1, MONTH ) )", "₹#,##0", "07 Time Intelligence");
M("Revenue MoM %",  @"DIVIDE ( [Total Revenue] - [Revenue PM], [Revenue PM] )", "0.0%",     "07 Time Intelligence");
M("Revenue YTD",    @"TOTALYTD ( [Total Revenue], 'Date'[Date] )",            "₹#,##0", "07 Time Intelligence");
M("Orders PM",      @"CALCULATE ( [Total Orders], DATEADD ( 'Date'[Date], -1, MONTH ) )",   "#,##0", "07 Time Intelligence");

Info("Done. Created/updated 36 measures. Press Ctrl+S to save back to Power BI.");

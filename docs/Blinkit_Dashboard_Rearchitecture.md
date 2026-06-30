# Blinkit Dashboard — Data-Grounded Re-Architecture

Rebuilt to match **what this dataset can actually prove**. Every feature below is backed by real structure in the data; anything that would fake a signal has been cut and the cut is explained. Positioning: realistic + recruiter-friendly. This supersedes the earlier draft.

---

## 1. Data reality — read this first

This dataset has strong **cross-sectional** structure but almost no **time-series** structure. Design to its strengths.

**Real signal (build on these):**
- **Margin varies 15–40% by category** (Grocery 15% … Instant & Frozen 40%) → profitability analysis is meaningful.
- **On-Time delivery 69.4%**, with a real status split (On Time / Slightly / Significantly Delayed) and a real delivery-time distribution (−5 to +30 min).
- **Repeat rate 68.7%**, avg 2.3 orders/customer → retention is real.
- **Rating distribution is genuinely skewed** (1:540, 2:538, 3:1398, 4:1708, 5:816) — not uniform, so it's worth showing.
- Real **sentiment** and **feedback-topic** splits; real **marketing funnel** (impressions→clicks→conversions, ROAS, spend); **316 cities**; **11 categories**.

**No signal (cut — would be dishonest to visualize):**

| Cut | Why |
|---|---|
| Partner performance / partner drill-through | 5,000 partners for 5,000 deliveries = **1 each**. No partner has a track record. |
| MoM growth deltas, "seasonality" matrix, growth narrative | Monthly revenue is **flat** (CV 5.5%). Deltas are noise dressed as trend. |
| Orders-by-hour "peak windows" | Hour distribution is **flat** (CV 8.4%). No real peak. |
| Units-per-order / basket size | **1 item per order**, qty 1–3 random. No basket to analyze. |
| `store_id` analysis | Unique per order — junk dimension. |
| New-vs-Active acquisition trend | Registrations are **flat** (~119/mo). Keep the count, skip the trend. |

**Consequence:** keep exactly **one** revenue trend line (for completeness — it reads "revenue is stable," which is true), and build everything else around **breakdowns and operations**, not time. Scoping a dashboard to what the data supports is itself a senior signal — say so in interviews.

---

## 2. Design system (all pages)

### 2.1 Filters — top bar (industry standard)
A slim **horizontal filter bar across the top**, under the title. Synced across all pages (View → Sync slicers). Not a left rail, not the bottom.

### 2.2 Page frame (identical on all 4 pages)
Canvas **1280 × 720**, page bg `#F4F7F4`.

| Zone | Visual | X | Y | W | H |
|---|---|---|---|---|---|
| Header band | Rectangle, `#0C831F` | 0 | 0 | 1280 | 44 |
| Page title | Text box (white, Semibold 18) | 16 | 8 | 360 | 28 |
| Page navigator | Buttons → Navigator → Page navigator | 470 | 8 | 600 | 28 |
| Filter bar bg | Rectangle, `#FFFFFF` | 0 | 44 | 1280 | 44 |
| Slicer: Year | Dropdown | 16 | 50 | 150 | 32 |
| Slicer: Month | Dropdown | 174 | 50 | 150 | 32 |
| Slicer: Category | Dropdown | 332 | 50 | 180 | 32 |
| Slicer: City | Dropdown + search | 520 | 50 | 180 | 32 |
| Slicer: Segment | Dropdown | 708 | 50 | 170 | 32 |
| Reset | Button → Bookmark "Clear Filters" | 1180 | 50 | 84 | 32 |

Content area: **x 16 → 1264, y 96 → 712.** KPI row at y 96, h 92; six cards at x = 16, 224, 432, 640, 848, 1056 (w 196).

### 2.3 KPI cards — clean values, no fake deltas
Use **Card (new)**: callout value = the measure, category label = its name. **No MoM delta arrows** — the data is flat, so deltas would imply a trend that isn't there. Clean number + label is the honest choice.

### 2.4 Color & type
Primary `#0C831F` · Accent `#F8CB46` · Good `#0C831F` · Bad `#E03131` · Neutral `#5A5F5A`. Segoe UI throughout. Single-green for sequences; green-vs-grey for comparisons; never rainbow. Apply `blinkit_green_theme.json`.

### 2.5 Not-a-tutorial rules
No raw field names (`area`→City, `category`→Category, `product_name`→Product); turn off redundant axis titles. Consistent currency (`₹11.0M` / `₹2,202`). No gridlines. Title Case. Identical header/filter bar/nav on every page.

---

## 3. New measures (add to your 36 — these are all data-backed)

```dax
Active Customers = DISTINCTCOUNT ( Orders[customer_id] )          -- ordered, ≠ registered

Repeat Customers =
COUNTROWS (
    FILTER ( VALUES ( Orders[customer_id] ),
             CALCULATE ( DISTINCTCOUNT ( Orders[order_id] ) ) >= 2 )
)

Repeat Rate % = DIVIDE ( [Repeat Customers], [Active Customers] )  -- Percentage (≈69%)

Orders per Customer = DIVIDE ( [Total Orders], [Active Customers] ) -- #,0.00

Est Gross Margin =
SUMX ( 'Order Items',
       'Order Items'[quantity] * 'Order Items'[unit_price]
       * RELATED ( Products[margin_percentage] ) / 100 )           -- ₹ (≈₹1.36M)

Gross Margin % = DIVIDE ( [Est Gross Margin], [Item Revenue] )      -- Percentage (≈27%)

CAC = DIVIDE ( [Total Spend], [Total Conversions] )                -- ₹ per acquired (proxy)

Good Stock % =
DIVIDE ( [Stock Received] - [Damaged Stock], [Stock Received] )     -- Percentage
```

That's it — no MoM/delta measures (flat data), no order-hour column, no units-per-order. Your existing `Revenue PM / MoM / YTD` can stay in the model unused; just don't put them on cards.

---

## 4. The four pages (all visuals data-backed)

Shared frame from §2.2. Content area only below; KPI row at y 96.

### PAGE 1 — Executive Summary
**KPIs:** `Total Revenue` · `Total Orders` · `Average Order Value` · `Active Customers` · `On-Time Delivery %` · `Repeat Rate %`.

| Visual | Type | Fields | X | Y | W | H |
|---|---|---|---|---|---|---|
| Revenue Trend (monthly) | Line | `Year Month` × `Total Revenue` | 16 | 196 | 612 | 254 |
| Revenue by Category | Bar | `Category` × `Item Revenue`, desc | 640 | 196 | 624 | 254 |
| Top Cities by Orders | Bar | `City` × `Total Orders`, Top 10 | 16 | 462 | 612 | 250 |
| Top 10 Products | Bar | `Product` × `Item Revenue`, Top 10 | 640 | 462 | 624 | 250 |

*This is essentially your current Page 1 — keep it, just swap KPIs and move slicers up.*

### PAGE 2 — Sales & Profitability
**KPIs:** `Total Revenue` · `Item Revenue` · `Est Gross Margin` · `Gross Margin %` · `Average Order Value` · `Total Orders`.

| Visual | Type | Fields | X | Y | W | H |
|---|---|---|---|---|---|---|
| Revenue by Category | Bar | `Category` × `Item Revenue`, desc | 16 | 196 | 612 | 254 |
| Revenue vs Margin | Scatter | X `Item Revenue` · Y `Gross Margin %` · Size `Total Units` · Legend `Category` | 640 | 196 | 624 | 254 |
| Top 10 Products | Bar | `Product` × `Item Revenue` | 16 | 462 | 612 | 250 |
| Category Profitability | Table (cond. format) | `Category`, `Item Revenue`, `Gross Margin %`, `Est Gross Margin` | 640 | 462 | 624 | 250 |

*The scatter is the standout: it sorts categories into high-rev/high-margin vs high-rev/low-margin (Grocery 15%) vs grow-me (Instant & Frozen 40%). The table is your conditional-formatting + drill-through source.*

### PAGE 3 — Delivery & Inventory (Operations)
**KPIs:** `On-Time Delivery %` · `Avg Delivery Time (min)` · `Avg Delivery Distance (km)` · `Delay Rate %` · `Damaged Stock %` · `Good Stock %`.

| Visual | Type | Fields | X | Y | W | H |
|---|---|---|---|---|---|---|
| On-Time % Trend | Line | `Year Month` × `On-Time Delivery %` | 16 | 196 | 612 | 254 |
| Delivery Status | Donut | `delivery_status` × `Total Deliveries` | 640 | 196 | 300 | 254 |
| Delivery Time Distribution | Column | `delivery_time_minutes` (binned) × `Total Deliveries` | 952 | 196 | 312 | 254 |
| Damaged % by Category | Bar | `Category` × `Damaged Stock %`, desc | 16 | 462 | 612 | 250 |
| Stock Received Trend | Line | `Year Month` × `Stock Received` | 640 | 462 | 624 | 250 |

*No partner analysis (1 delivery/partner). No delay-reason chart (Traffic-only). The status split + time distribution carry the delivery story honestly.*

### PAGE 4 — Customer & Marketing
**KPIs:** `Active Customers` · `Repeat Rate %` · `Average Rating` · `Positive Sentiment %` · `ROAS` · `CAC`.

| Visual | Type | Fields | X | Y | W | H |
|---|---|---|---|---|---|---|
| Rating Distribution | Column | `rating` (1–5) × `Total Feedback` | 16 | 196 | 400 | 254 |
| Sentiment Split | Donut | `sentiment` × `Total Feedback` | 428 | 196 | 400 | 254 |
| Feedback by Topic | Bar | `feedback_category` × `Total Feedback` | 840 | 196 | 424 | 254 |
| Customers by Segment | Bar | `customer_segment` × `Active Customers` | 16 | 462 | 400 | 250 |
| ROAS by Channel | Bar | `channel` × `ROAS` | 428 | 462 | 400 | 250 |
| Marketing Funnel | Funnel | `Total Impressions` → `Total Clicks` → `Total Conversions` | 840 | 462 | 424 | 250 |

*Rating distribution is genuinely skewed (worth showing). Note on the page that marketing is a **disconnected** table — its visuals don't react to the global filters, by design.*

---

## 5. Advanced features (only the ones the data earns)

**Kept:**
1. **Top filter bar** (UI, not data-dependent) — §2.2.
2. **Page navigator** — Insert → Buttons → Navigator → Page navigator, in the header band.
3. **One drill-through:** hidden "Product Detail" page with `Category` in the Drill-through well; right-click a category/product on Pages 1–2 → Drill through. Add a Back button.
4. **Conditional formatting** on the Page 2 Category Profitability table — `Gross Margin %` as a red→green color scale (instant "which categories make money").
5. **Reset-filters button** (bookmark, "Data only").

**Cut (no data to earn them):** MoM delta cards, partner drill-through, custom hour/peak visuals, seasonality matrix. (Optional, if you want one more flourish: a custom tooltip on the category bars showing that category's **top products** — real, unlike a flat monthly trend.)

---

## 6. Migration from your current build (low rework)

1. Keep theme + your four Page-1 charts — they already match Page 1's grid.
2. Move the 4 slicers into the **top filter bar** (§2.2), convert to dropdowns, **add the Segment slicer**, delete the bottom-left block, re-sync.
3. Add the **green header band + title + page navigator**.
4. Swap Page-1 KPIs to the six listed (add `Active Customers`, `Repeat Rate %`; move Item Revenue/Rating/Sentiment/ROAS to Pages 2–4).
5. Add the §3 measures. Then **duplicate Page 1** three times as the frame and swap in Pages 2–4 content.
6. Build the drill-through page + conditional formatting + reset button.
7. Polish pass (§2.5).

---

## 7. Resume framing (honest version — this is the strong one)

- *"Scoped a 4-page Power BI suite to what the data could actually support — cut growth-trend and partner-performance views once profiling showed flat monthly revenue and one delivery per partner, and focused on the dimensions with real structure: category profitability, delivery reliability, and customer retention."*
- *"Modeled estimated gross margin by category (15–40% spread) and a revenue-vs-margin quadrant; surfaced a 69% repeat rate and a skewed rating distribution."*
- *"Built a top filter bar, drill-through detail page, conditional-formatted profitability table, and page navigation on a 9-table star schema with 40+ measures."*

> *Blinkit quick-commerce analytics suite — 4-page Power BI report (Exec / Sales & Profitability / Operations / Customer & Marketing) on a 9-table star schema, scoped to the data's real signal, with drill-through, conditional formatting, and a custom theme.*

Knowing what **not** to build is the most senior thing on this list. Lead with it.

---

### Build checklist
1. §3 measures
2. Top filter bar + header + nav (§2.2)
3. Clean-value KPI cards (§2.3)
4. Page 1 → duplicate frame → Pages 2–4 (§4)
5. Drill-through + conditional formatting + reset (§5)
6. Polish (§2.5)

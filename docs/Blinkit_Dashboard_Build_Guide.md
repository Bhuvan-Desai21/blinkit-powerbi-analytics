# Blinkit Power BI — Complete Build Guide

A working BI-consultant build sheet for the 4-page Blinkit dashboard. Everything below is grounded in your *actual* CSVs (row counts, column names, and category values were checked). Build top-to-bottom. Estimated time start-to-finish: ~30–45 min.

---

## 0. Read this first — what's broken and what the data actually is

Three things will silently wreck visuals if you don't handle them up front.

**(A) Your Revenue Trend bug — root cause found.**
`Orders[order_date]` is a **datetime with a real time component** (`2024-07-17 08:34:01` — there are 4,866 distinct clock times). Your Date table holds **pure dates** (`...00:00:00`). The relationship is "active," but `08:34:01` never equals `00:00:00`, so **zero rows match** and every order lands in the BLANK bucket → single point / empty line. Fix is in §1.

**(B) "Total Revenue" and "Revenue by Category" will never tie out — by design of this dataset.**
- `Order Items` is **1:1 with Orders** here (5,000 / 5,000, one row per order — not many).
- `Orders[order_total]` and `quantity × unit_price` from `Order Items` are **statistically unrelated** (correlation ≈ 0). Example order: `order_total = 3197.07` but item line = `1551.09`.
- So you keep **two separate revenue measures**: `Total Revenue` (order grain, the headline KPI) and `Item Revenue` (item grain, used *only* for category/product splits). Label the category chart honestly ("Revenue by Category — item level"). In a real Blinkit dataset these would reconcile; here they don't, and saying so in your README is a *plus*, not a weakness.

**(C) Delay-reason data is thin.** `reasons_if_delayed` is only `Traffic` (3,098) or blank — and it doesn't even align with delivery status. Don't build a rich "delay reasons" chart on it. Use the **delivery-status split** (On Time / Slightly Delayed / Significantly Delayed) instead. That's the honest, useful cut.

**Key grains & keys (verified):**

| Table | Rows | Grain | Key |
|---|---|---|---|
| Orders | 5,000 | one order | order_id (unique) |
| Order Items | 5,000 | one line (1:1 here) | order_id (unique) |
| Customers | 5,030 | one customer | customer_id |
| Products | 268 | one product | product_id |
| Delivery Performance | 5,000 | one delivery (1:1 w/ Orders) | order_id |
| Customer Feedback | 5,000 | one feedback | feedback_id |
| Inventory | 75,172 | product × day | product_id + date |
| Monthly Inventory | 18,105 | product × month | product_id + month |
| Marketing Performance | 5,400 | campaign × day | (disconnected) |

There is **no city column in Orders.** "Orders by City" must use `Customers[area]` (e.g., Jalna, Deoghar, Ghaziabad). Treat `area` as your city dimension.

---

## 1. Power Query — fix data types BEFORE modeling

Open Transform Data and fix these. Do it here, not with DAX.

1. **`Orders[order_date]` → Date** (this fixes the line chart).
   - If you also want true delivery *duration* later, first **duplicate the column** → name it `order_datetime` (keep as Date/Time), *then* set `order_date` to **Date**. The relationship uses `order_date`; duration calcs can use the datetime copy.
2. **`Orders[promised_delivery_time]`, `[actual_delivery_time]` → Date/Time.**
3. **`Delivery Performance[promised_time]`, `[actual_time]` → Date/Time.**
4. **`Inventory[date]`** is `dd-mm-yyyy` text (`17-03-2023`). Set locale-aware: Transform → Data Type → Using Locale → **Date**, Locale **English (UK)** (day-first). Confirm it parses.
5. **`Monthly Inventory[date]`** is `Mar-23` text. Add Column → custom: `Date.FromText("01-" & [date], [Format="dd-MMM-yy"])` or just split/parse to a real month-start date so it can join to the Date table.
6. **`Customer Feedback[feedback_date]`, `Customers[registration_date]` → Date.**
7. Set numeric types explicitly: `order_total`, `unit_price`, `price`, `mrp`, `spend`, `revenue_generated`, `roas` → **Decimal**; `quantity`, `impressions`, `clicks`, `conversions`, `delivery_time_minutes` → **Whole/Decimal** as appropriate.

After Close & Apply, your line chart will already work once §2 is correct.

---

## 2. Model — final star schema

Confirm these relationships (1 = one side / dimension, * = many / fact). All **single** cross-filter direction, all **active**.

```
Date[Date]            1 → *  Orders[order_date]
Customers[customer_id]1 → *  Orders[customer_id]
Customers[customer_id]1 → *  Customer Feedback[customer_id]
Products[product_id]  1 → *  Order Items[product_id]
Products[product_id]  1 → *  Inventory[product_id]
Products[product_id]  1 → *  Monthly Inventory[product_id]
Orders[order_id]      1 → 1  Order Items[order_id]
Orders[order_id]      1 → 1  Delivery Performance[order_id]
Orders[order_id]      1 → *  Customer Feedback[order_id]
Marketing Performance        (disconnected — intentional)
```

Two cleanups that matter:

- **Mark the Date table as a date table** (right-click Date → *Mark as date table* → `Date` column). Required for the time-intelligence measures in §3.7.
- For category/product analysis the filter must flow **Products → Order Items**. Because Order Items also relates up to Orders (1:1), and Orders relates to Date/Customers, filtering by Date or City correctly narrows Item Revenue too. Good — no bridge table needed *because* it's 1:1. (If this dataset were truly 1:many, the `SUMX` over Order Items in §3.2 still generalizes correctly.)
- Hide foreign-key columns and raw clutter from the Report view (right-click → Hide): all the `*_id` keys on fact tables, the Measures dummy column, helper columns.

---

## 3. The complete DAX measure pack

Create all of these in your `Measures` table. Names use your business-name convention. Paste as-is; table/column names match your CSVs exactly. Set formats where noted (the comment is not part of the DAX).

### 3.1 Core (Page 1 KPIs)

```dax
Total Revenue = SUM ( Orders[order_total] )                      -- format: ₹ / #,0

Total Orders = DISTINCTCOUNT ( Orders[order_id] )                -- #,0

Total Customers = DISTINCTCOUNT ( Customers[customer_id] )       -- #,0

Average Order Value =
DIVIDE ( [Total Revenue], [Total Orders] )                       -- ₹ / #,0.00
```

`Average Order Value` now uses `DIVIDE` over your measures (your earlier `AVERAGE(order_total)` gave the same number here only because Order grain is unique — `DIVIDE` is the correct, defensible version).

### 3.2 Item-level (category & product splits only)

```dax
Item Revenue =
SUMX ( 'Order Items', 'Order Items'[quantity] * 'Order Items'[unit_price] )   -- ₹

Total Units = SUM ( 'Order Items'[quantity] )                                  -- #,0

Avg Selling Price =
DIVIDE ( [Item Revenue], [Total Units] )                                       -- ₹
```

> Use **`Item Revenue`** (not `Total Revenue`) on any visual broken down by `Products[category]` / `Products[product_name]`. Title those visuals "… (item level)".

### 3.3 Delivery (Page 1 + Page 3)

```dax
Total Deliveries = COUNTROWS ( 'Delivery Performance' )

On Time Deliveries =
CALCULATE (
    COUNTROWS ( 'Delivery Performance' ),
    'Delivery Performance'[delivery_status] = "On Time"
)

On-Time Delivery % =
DIVIDE ( [On Time Deliveries], [Total Deliveries], 0 )           -- format: Percentage

Delayed Orders =
CALCULATE (
    COUNTROWS ( 'Delivery Performance' ),
    'Delivery Performance'[delivery_status] <> "On Time"
)

Delay Rate % =
DIVIDE ( [Delayed Orders], [Total Deliveries], 0 )               -- Percentage

Avg Delivery Time (min) =
AVERAGE ( 'Delivery Performance'[delivery_time_minutes] )        -- #,0.0
-- NOTE: this is minutes vs the promised window (negative = early, mean ≈ 4.4 = slightly late).
-- Label the KPI "Avg vs Promised (min)" so it isn't mistaken for total delivery duration.

Avg Delivery Distance (km) =
AVERAGE ( 'Delivery Performance'[distance_km] )                  -- #,0.00
```

### 3.4 Customer & Feedback (Page 4)

```dax
Average Rating = AVERAGE ( 'Customer Feedback'[rating] )         -- #,0.00 (scale 1–5)

Total Feedback = COUNTROWS ( 'Customer Feedback' )

Positive Feedback =
CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = "Positive" )

Negative Feedback =
CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = "Negative" )

Positive Sentiment % =
DIVIDE ( [Positive Feedback], [Total Feedback], 0 )             -- Percentage

% 5-Star =
DIVIDE (
    CALCULATE ( [Total Feedback], 'Customer Feedback'[rating] = 5 ),
    [Total Feedback], 0
)                                                               -- Percentage
```

### 3.5 Inventory (Page 4)

```dax
Stock Received = SUM ( Inventory[stock_received] )

Damaged Stock = SUM ( Inventory[damaged_stock] )

Damaged Stock % =
DIVIDE ( [Damaged Stock], [Stock Received], 0 )                 -- Percentage

Product Count = DISTINCTCOUNT ( Products[product_id] )
```

### 3.6 Marketing (Page 4 — disconnected, standalone visuals only)

```dax
Total Spend = SUM ( 'Marketing Performance'[spend] )            -- ₹

Marketing Revenue = SUM ( 'Marketing Performance'[revenue_generated] )

Total Impressions = SUM ( 'Marketing Performance'[impressions] )

Total Clicks = SUM ( 'Marketing Performance'[clicks] )

Total Conversions = SUM ( 'Marketing Performance'[conversions] )

CTR % = DIVIDE ( [Total Clicks], [Total Impressions], 0 )        -- Percentage

Conversion Rate % = DIVIDE ( [Total Conversions], [Total Clicks], 0 )  -- Percentage

ROAS = DIVIDE ( [Marketing Revenue], [Total Spend], 0 )          -- #,0.00 "x"
```

> Because Marketing is disconnected, the **Year/Month slicers won't filter it**. Put marketing visuals in their own bordered section and (optionally) give them a *separate* date slicer built on `'Marketing Performance'[date]`.

### 3.7 Time intelligence (Page 2 — needs Date marked as a date table)

```dax
Revenue PM =
CALCULATE ( [Total Revenue], DATEADD ( 'Date'[Date], -1, MONTH ) )

Revenue MoM % =
DIVIDE ( [Total Revenue] - [Revenue PM], [Revenue PM] )          -- Percentage

Revenue YTD =
TOTALYTD ( [Total Revenue], 'Date'[Date] )

Orders PM =
CALCULATE ( [Total Orders], DATEADD ( 'Date'[Date], -1, MONTH ) )
```

---

## 4. Page-by-page build recipe

For each visual: **type → fields → why**. Build in this order. Canvas: 16:9, 1280×720.

### Page 1 — Executive Overview

**KPI row (6 cards, top):** `Total Revenue`, `Total Orders`, `Total Customers`, `Average Order Value`, `On-Time Delivery %`, `Avg Delivery Time (min)`.
- Visual: **Card** (or new "KPI" card). One measure each. This is your existing test page — reuse it.

**Revenue Trend (line):** Axis = `Date[Year Month]` (or `Date[Date]` set to *Continuous*), Values = `Total Revenue`. *Why:* the headline metric over time; now works after §1. Use `Year Month` for a clean monthly line instead of 600 daily points.

**Revenue by Category (bar, horizontal):** Y = `Products[category]`, X = **`Item Revenue`**. Sort descending. *Why:* category mix; must use item-level revenue per §0(B). Title it "Revenue by Category (item level)".

**Top Products (bar):** Y = `Products[product_name]`, X = `Item Revenue`, Top-N filter = 10. *Why:* what actually sells.

**Orders by City (map or bar):** Location = `Customers[area]`, Value = `Total Orders`. Top-N 10 if using a bar. *Why:* geographic demand. (Use bar — `area` names are clean; a filled map needs lat/long it doesn't have.)

**Global slicers (left rail):** `Date[Year]`, `Date[Month Name]`, `Products[category]`, `Customers[area]`. See §4.5 for syncing.

### Page 2 — Sales Analytics

- **Revenue + MoM% combo:** line `Total Revenue` + line `Revenue MoM %` (secondary axis), axis `Date[Year Month]`.
- **Revenue YTD (line):** `Revenue YTD` by `Date[Year Month]`.
- **Category × Month matrix:** Rows `Products[category]`, Columns `Date[Month Name]`, Values `Item Revenue`, conditional-format background (green scale). *Seasonality view.*
- **Top/Bottom products:** two bars, `Item Revenue` by `product_name`, Top-N and Bottom-N 10.
- **AOV trend (line):** `Average Order Value` by `Date[Year Month]`.
- **Payment mix (donut):** Legend `Orders[payment_method]`, Values `Total Orders` (Card/Cash/Wallet/UPI are ~even — note that).

### Page 3 — Delivery Analytics

- **KPIs:** `On-Time Delivery %`, `Delay Rate %`, `Avg Delivery Time (min)`, `Avg Delivery Distance (km)`.
- **Delivery status split (donut or stacked bar):** Legend `Delivery Performance[delivery_status]`, Values `Total Deliveries`. *This replaces "delay reasons" — see §0(C).*
- **On-Time % trend (line):** `On-Time Delivery %` by `Date[Year Month]`. *Is delivery improving?*
- **Delivery partners (bar):** Y = `Delivery Performance[delivery_partner_id]`, X = `On-Time Delivery %` (or `Delayed Orders`), Top/Bottom-N 10. *Best/worst partners.*
- **Distance vs delay (scatter):** X = `Avg Delivery Distance (km)`, Y = `Avg Delivery Time (min)`, details = `delivery_partner_id`. *Does distance drive delay?*

### Page 4 — Customer & Inventory

- **Customer:** cards `Average Rating`, `Positive Sentiment %`, `Total Customers`. Donut of `Total Feedback` by `Customer Feedback[sentiment]`. Bar of `Total Feedback` by `feedback_category`. Bar of `Total Customers` by `Customers[customer_segment]` (New/Regular/Premium/Inactive).
- **Inventory:** cards `Damaged Stock %`, `Stock Received`. Bar of `Damaged Stock %` by `Products[category]` (where's spoilage worst). Line of `Stock Received` by month (`Monthly Inventory` date).
- **Marketing (separate boxed section):** cards `ROAS`, `Total Spend`, `Conversion Rate %`. Bar of `ROAS` by `'Marketing Performance'[channel]` (App/Email/Social/SMS). Remember: not filtered by the global slicers (§3.6).

### 4.5 Slicers & interactions

- Build the 4 slicers on Page 1, select all → **View → Sync slicers** → tick the pages you want them to control. Keep them visually identical across pages (same left rail).
- **Format → Edit interactions:** make slicers *filter* visuals; stop KPI cards from being cross-filtered by chart clicks if you find it distracting.
- Add a **"Reset filters" bookmark** (Bookmarks pane → clear slicers → capture) wired to a button — small touch that reads as polished.

---

## 5. Design spec — Blinkit green, minimal, modern

**Import the theme first:** View → Themes → **Browse for themes** → select `blinkit_green_theme.json` (in this folder). It sets the palette, white cards, rounded 12px corners, soft shadow, and Segoe UI typography automatically, so every visual you drop is already on-brand.

**Palette**

| Role | Hex | Use |
|---|---|---|
| Primary green | `#0C831F` | KPIs, primary bars/lines, accents |
| Blinkit yellow | `#F8CB46` | secondary/highlight series, callouts |
| Page background | `#F4F7F4` | canvas |
| Card background | `#FFFFFF` | all visual backgrounds |
| Text primary | `#1A1A1A` | titles, values |
| Text secondary | `#5A5F5A` | labels, captions |
| Bad / alert | `#E03131` | delay rate, damaged stock |

**Layout rules**
- 8px grid. Equal gutters. Don't let visuals touch.
- Top strip = title + KPI cards. Left rail = slicers (~220px). Charts fill the rest.
- One **page header** band: small Blinkit-green rectangle + page title (e.g., "Executive Overview") in Segoe UI Semibold 18–20. Repeat on every page for consistency.
- Max ~6 visuals per page. White space is a feature.
- Typography: **Segoe UI** everywhere. Titles Semibold 12–14, KPI values 28–32, labels 10.
- Turn **off** gridlines, turn **off** visual borders where the theme shadow already separates cards, round data labels (₹ no decimals on big numbers, % to 1 decimal).
- Number formatting: big money as `₹1.2M` / `₹12.3K` (Format → Display units), percentages 0–1 decimal.

**Polish checklist:** consistent title casing, aligned edges (use the alignment tools), no default "Sum of …" titles (rename every field/title), tooltips on, a single accent color used consistently for "good," and the reset button on each page.

---

## 6. Resume framing

When you write the README / talk about this in interviews, **lead with the modeling judgment, not the visuals.** The strongest talking points are the data problems you caught:

- *"Diagnosed a date-relationship failure caused by a time component in the order timestamp — fixed in Power Query rather than masking it with DAX."*
- *"Identified that order-level revenue and item-level revenue don't reconcile in the source, so I modeled two explicit revenue measures and labeled category analysis as item-level to avoid double-counting."*
- *"Chose `area` as the geographic grain and a star schema with a dedicated Date dimension and a disconnected marketing table."*
- *"Built reusable measures (DIVIDE-based ratios, time-intelligence MoM/YTD) on a marked date table."*

One-line project blurb:
> *Blinkit retail analytics dashboard (Power BI) — 4-page executive/sales/delivery/customer model over 9 tables (~50K+ rows), star schema with dedicated date dimension, 30+ DAX measures, and a custom on-brand theme.*

**Honest caveats to keep in your README** (they signal maturity): the dataset is synthetic; order_total vs item revenue don't reconcile; delay-reason field is sparse (Traffic-only); marketing is intentionally disconnected.

---

### Build order checklist
1. Power Query type fixes (§1) → Close & Apply
2. Confirm relationships + mark Date table (§2)
3. Paste measure pack (§3)
4. Import theme (§5)
5. Build Page 1 → sync slicers → Pages 2–4 (§4)
6. Polish pass (§5 checklist)


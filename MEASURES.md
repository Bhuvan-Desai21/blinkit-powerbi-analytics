# Measures Reference (Data Dictionary)

Every DAX measure in the model, grouped by display folder, with its definition, format string, and business meaning. Measures live in dedicated `Measure Table` / `Marketing Measure Table` tables (hidden dummy column, business-friendly names).

---

## 01 · Core

| Measure | Format | Description |
|---|---|---|
| **Total Revenue** | `₹#,##0` | Order-level revenue; the headline KPI. |
| **Total Orders** | `#,##0` | Distinct orders. |
| **Total Customers** | `#,##0` | All registered customers. |
| **Active Customers** | `#,##0` | Customers who placed ≥1 order (≠ registered). |
| **Average Order Value** | `₹#,##0.00` | Revenue per order. |

```dax
Total Revenue       = SUM ( Orders[order_total] )
Total Orders        = DISTINCTCOUNT ( Orders[order_id] )
Total Customers     = DISTINCTCOUNT ( Customers[customer_id] )
Active Customers    = DISTINCTCOUNT ( Orders[customer_id] )
Average Order Value = DIVIDE ( [Total Revenue], [Total Orders] )
```

## 02 · Items & Profitability

| Measure | Format | Description |
|---|---|---|
| **Item Revenue** | `₹#,##0` | Line-level revenue (qty × unit price). Use for category/product splits. |
| **Total Units** | `#,##0` | Units sold. |
| **Avg Selling Price** | `₹#,##0.00` | Item revenue per unit. |
| **Est Gross Margin** | `₹#,##0` | Item revenue × category margin %. |
| **Gross Margin %** | `0.0%` | Gross margin over item revenue (≈27.4%). |
| **Gross Margin per Order** | `₹#,##0` | Profit contribution per order. |
| **Avg Discount vs MRP %** | `0.0%` | How far below MRP items sell (catalog avg). |

```dax
Item Revenue          = SUMX ( 'Order Items', 'Order Items'[quantity] * 'Order Items'[unit_price] )
Total Units           = SUM ( 'Order Items'[quantity] )
Avg Selling Price     = DIVIDE ( [Item Revenue], [Total Units] )
Est Gross Margin      = SUMX ( 'Order Items',
                          'Order Items'[quantity] * 'Order Items'[unit_price]
                          * RELATED ( Products[margin_percentage] ) / 100 )
Gross Margin %        = DIVIDE ( [Est Gross Margin], [Item Revenue] )
Gross Margin per Order= DIVIDE ( [Est Gross Margin], [Total Orders] )
Avg Discount vs MRP % = AVERAGEX ( Products, 1 - DIVIDE ( Products[price], Products[mrp] ) )
```

## 03 · Delivery

| Measure | Format | Description |
|---|---|---|
| **Total Deliveries** | `#,##0` | Rows in Delivery Performance. |
| **On Time Deliveries** | `#,##0` | Deliveries with status = "On Time". |
| **On-Time Delivery %** | `0.0%` | On-time share (≈69.4%). |
| **Delayed Orders** | `#,##0` | Deliveries not on time. |
| **Delay Rate %** | `0.0%` | Delayed share (complement of on-time). |
| **Significantly Delayed %** | `0.0%` | Severe-failure rate (≈9.9%) — the actionable tail. |
| **Avg Delivery Time (min)** | `#,##0.0` | Minutes vs the promised window (negative = early). |
| **Avg Delivery Distance (km)** | `#,##0.00` | Mean delivery distance. |

```dax
Total Deliveries       = COUNTROWS ( 'Delivery Performance' )
On Time Deliveries     = CALCULATE ( COUNTROWS ( 'Delivery Performance' ),
                           'Delivery Performance'[delivery_status] = "On Time" )
On-Time Delivery %     = DIVIDE ( [On Time Deliveries], [Total Deliveries], 0 )
Delayed Orders         = CALCULATE ( COUNTROWS ( 'Delivery Performance' ),
                           'Delivery Performance'[delivery_status] <> "On Time" )
Delay Rate %           = DIVIDE ( [Delayed Orders], [Total Deliveries], 0 )
Significantly Delayed %= DIVIDE (
                           CALCULATE ( [Total Deliveries],
                             'Delivery Performance'[delivery_status] = "Significantly Delayed" ),
                           [Total Deliveries] )
Avg Delivery Time (min)     = AVERAGE ( 'Delivery Performance'[delivery_time_minutes] )
Avg Delivery Distance (km)  = AVERAGE ( 'Delivery Performance'[distance_km] )
```

## 04 · Customer & Feedback

| Measure | Format | Description |
|---|---|---|
| **Average Rating** | `#,##0.00` | Mean feedback rating, 1–5 (≈3.34). |
| **Total Feedback** | `#,##0` | Feedback responses. |
| **Positive / Negative Feedback** | `#,##0` | Responses by sentiment. |
| **Positive Sentiment %** | `0.0%` | Positive share. |
| **% 5-Star** | `0.0%` | Share of 5-star ratings. |
| **Repeat Customers** | `#,##0` | Customers with ≥2 orders. |
| **Repeat Rate %** | `0.0%` | Repeat share of active customers (≈68.7%). |
| **Orders per Customer** | `#,##0.00` | Orders ÷ active customers (≈2.3). |
| **New Customers** | `#,##0` | Distinct customers by signup date (inactive relationship). |

```dax
Average Rating      = AVERAGE ( 'Customer Feedback'[rating] )
Total Feedback      = COUNTROWS ( 'Customer Feedback' )
Positive Feedback   = CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = "Positive" )
Negative Feedback   = CALCULATE ( [Total Feedback], 'Customer Feedback'[sentiment] = "Negative" )
Positive Sentiment %= DIVIDE ( [Positive Feedback], [Total Feedback], 0 )
% 5-Star            = DIVIDE ( CALCULATE ( [Total Feedback], 'Customer Feedback'[rating] = 5 ),
                        [Total Feedback], 0 )
Repeat Customers    = COUNTROWS ( FILTER ( VALUES ( Orders[customer_id] ),
                        CALCULATE ( DISTINCTCOUNT ( Orders[order_id] ) ) >= 2 ) )
Repeat Rate %       = DIVIDE ( [Repeat Customers], [Active Customers] )
Orders per Customer = DIVIDE ( [Total Orders], [Active Customers] )
New Customers       = CALCULATE ( DISTINCTCOUNT ( Customers[customer_id] ),
                        USERELATIONSHIP ( Customers[registration_date], 'Date'[Date] ) )
```

## 05 · Inventory

| Measure | Format | Description |
|---|---|---|
| **Stock Received** | `#,##0` | Units received. |
| **Damaged Stock** | `#,##0` | Units damaged. |
| **Damaged Stock %** | `0.0%` | Damaged share. ⚠️ *See README — source field is unreliable (damaged > received in 39% of rows).* |
| **Good Stock %** | `0.0%` | Undamaged share (complement). |
| **Product Count** | `#,##0` | Distinct products. |

```dax
Stock Received = SUM ( Inventory[stock_received] )
Damaged Stock  = SUM ( Inventory[damaged_stock] )
Damaged Stock %= DIVIDE ( [Damaged Stock], [Stock Received], 0 )
Good Stock %   = DIVIDE ( [Stock Received] - [Damaged Stock], [Stock Received] )
Product Count  = DISTINCTCOUNT ( Products[product_id] )
```

## 06 · Marketing *(disconnected table — not filtered by global slicers)*

| Measure | Format | Description |
|---|---|---|
| **Total Spend** | `₹#,##0` | Ad spend. |
| **Marketing Revenue** | `₹#,##0` | Campaign-attributed revenue. |
| **Total Impressions / Clicks / Conversions** | `#,##0` | Funnel volumes. |
| **CTR %** | `0.0%` | Clicks ÷ impressions. |
| **Conversion Rate %** | `0.0%` | Conversions ÷ clicks. |
| **ROAS** | `#,##0.00` | Marketing revenue ÷ spend (≈1.97×). |
| **CAC** | `₹#,##0` | Spend ÷ conversions (acquisition cost proxy). |

```dax
Total Spend       = SUM ( 'Marketing Performance'[spend] )
Marketing Revenue = SUM ( 'Marketing Performance'[revenue_generated] )
Total Impressions = SUM ( 'Marketing Performance'[impressions] )
Total Clicks      = SUM ( 'Marketing Performance'[clicks] )
Total Conversions = SUM ( 'Marketing Performance'[conversions] )
CTR %             = DIVIDE ( [Total Clicks], [Total Impressions], 0 )
Conversion Rate % = DIVIDE ( [Total Conversions], [Total Clicks], 0 )
ROAS              = DIVIDE ( [Marketing Revenue], [Total Spend], 0 )
CAC               = DIVIDE ( [Total Spend], [Total Conversions] )
```

## 07 · Time Intelligence *(requires Date marked as a date table)*

| Measure | Format | Description |
|---|---|---|
| **Revenue PM / Orders PM** | `₹#,##0` / `#,##0` | Prior-month values. |
| **Revenue MoM %** | `0.0%` | Month-over-month revenue growth. |
| **Revenue YTD** | `₹#,##0` | Year-to-date revenue. |

```dax
Revenue PM    = CALCULATE ( [Total Revenue], DATEADD ( 'Date'[Date], -1, MONTH ) )
Orders PM     = CALCULATE ( [Total Orders],  DATEADD ( 'Date'[Date], -1, MONTH ) )
Revenue MoM % = DIVIDE ( [Total Revenue] - [Revenue PM], [Revenue PM] )
Revenue YTD   = TOTALYTD ( [Total Revenue], 'Date'[Date] )
```

## 08 · Helpers (hidden)

| Measure | Format | Description |
|---|---|---|
| **City Rank (Orders)** | `#,##0.000` | Tiebreaker for Top-N city ranking — order count + a tiny revenue fraction so ties resolve uniquely. |

```dax
City Rank (Orders) = [Total Orders] + DIVIDE ( [Total Revenue], 1000000000 )
```

---

*Note: `Total Customers`, `Average Order Value`, `Total Orders` use `DISTINCTCOUNT`/`DIVIDE` deliberately — the earlier `COUNT`/`AVERAGE` versions worked only because of unique keys; the current forms are the correct, defensible definitions.*

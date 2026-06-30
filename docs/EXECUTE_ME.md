# EXECUTE ME — Blinkit dashboard, exact run order

Do these in order. Honest boundary up front: **steps 1–5 are paste/click (run-once). Step 6 (the visuals) is manual drag-and-drop** — Power BI has no "import a chart" file. Everything that *can* be automated, is. Budget ~25–35 min total, most of it in step 6.

Files referenced are all in this folder.

---

## Step 1 — Fix data types (Power Query) · ~3 min
Open `my_first_powerbi.pbix` → **Transform data**. Apply the fixes in **`Blinkit_Dashboard_Build_Guide.md` §1** — the critical one is **`Orders[order_date]` → Date** (this alone fixes your blank line chart). Also fix the two inventory date columns. **Close & Apply.**

## Step 2 — Date table · ~1 min
Modeling → **New table** → paste **all** of `Date_Table.dax` → Enter.
Then the 4 clicks listed at the bottom of that file (Mark as date table + 3 Sort-by-column settings).

## Step 3 — Confirm relationships · ~2 min
Model view → match the diagram in **§2** of the guide. Ensure `Date[Date] → Orders[order_date]` is active, single direction. Hide `*_id` foreign keys.

## Step 4 — Create all 36 measures (run-once) · ~3 min
Make sure a table named exactly **`Measures`** exists (you have it).
Open **Tabular Editor** (External Tools ribbon — install the free version if it's not there: tabulareditor.com).
Paste **all** of `Create_All_Measures.csx` into the **C# Script** tab → **Run (F5)** → **Ctrl+S** to push back to Power BI.
→ All measures appear, formatted, in folders 01–07.
*No Tabular Editor?* Fall back to pasting them one at a time from guide **§3** (New measure → paste → repeat).

## Step 5 — Apply the theme · ~30 sec
View → Themes → **Browse for themes** → `blinkit_green_theme.json`.
Now every visual you add is green/white/rounded automatically.

## Step 6 — Build the 4 pages (manual) · ~20 min
Follow **§4** of the guide visual-by-visual (type → fields → why). Suggested flow:
1. Build **Page 1** completely (KPI cards + 4 charts + 4 slicers).
2. Select the 4 slicers → **View → Sync slicers** → tick pages 2–4.
3. Duplicate the page header band onto each page for consistency.
4. Build Pages 2, 3, 4.
5. Run the **polish checklist** at the end of §5 (rename titles, display units, alignment, reset button).

---

## File map
| File | What it is | Where it goes |
|---|---|---|
| `Blinkit_Dashboard_Build_Guide.md` | Full reference (fixes, DAX, per-visual recipes, design) | read alongside |
| `Date_Table.dax` | One-paste Date table | Step 2 |
| `Create_All_Measures.csx` | Creates all 36 measures at once | Step 4 |
| `blinkit_green_theme.json` | Brand theme | Step 5 |

## Why visuals can't be auto-generated here
A `.pbix` stores its report layout in a proprietary binary part. The only text-based way to author visuals is the **PBIP/TMDL** project format — you chose not to go that route (it's fragile and slower for one report). So measures/model/theme are automated; chart placement stays manual.

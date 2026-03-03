# CD_Insure / Item Protection – Design Reference

Design source: [Figma – Shoplazza (Item Protection)](https://www.figma.com/design/oMQ1YpL59gWuZuETfL0W7f/Shoplazza?node-id=36-15263&t=25QoVGScxTjfJyVo-1)

---

## 1. Admin layout

- **Left sidebar**
  - App title: "Item Protection" (or "CD_Insure") with shield icon
  - **Configuration** (active = highlighted, dashboard icon)
  - **Policy** (shield/checkmark icon)
  - **Claims** (person/avatar icon)
  - **Settlement** (dollar icon)

---

## 2. Configuration page – structure

### 2.1 Header
- Title: **Configuration**
- Short description with links: e.g. "Item protection helps shoppers buy with confidence — and resolves issues when something goes wrong. Learn more about **Item Protection** and **Claim settlement process**." (links in blue, underlined)

### 2.2 Data overview
- Subtitle: **Data overview**
- Note: "The monetary values in the data overview section are shown in US dollars."
- **Date range**: Start date / End date (with calendar icon), top right
- **Metric cards** (light gray bordered cards):
  - Insured orders (count, e.g. 189)
  - Insured order amount (e.g. 18436.74 USD)
  - Purchase rate of Worry-Free Purchase (e.g. 1.64%)
  - Revenue share (e.g. 63.45 USD)
  - Estimated cost savings (e.g. 50.99 USD)

### 2.3 Item Protection activation card
- **Title**: "Item Protection"
- **Status**: Pill badge – "Deactivated" (gray) or "Activated" (green)
- **Primary action**: Blue **Activate** button when deactivated, or gray **Deactivate** when activated
- **Checkbox**: "Default 'Item Protection' at checkout"
- **Message when deactivated**: "Item Protection has been deactivated. You no longer have the following benefits."
- **Benefits** (with icons):
  - "Increase Trust to Boost Conversion Rates" (+5%, monitor/rocket icon)
  - "Leave it with us - Chubb handles all claims" (support hands icon)

### 2.4 Extended settings (second design)
- **Checkout placement**: Radio options – "Regular" (default), "Checkout +"
- **Enable**: Checkbox "Enable Powered by CHUBB" (with Chubb logo)
- **Info banner** (orange): "Customers must actively opt in at checkout. Default pre-selection may not be permitted in some markets."
- **Checkbox**: "Offer 'Item Protection' at checkout"
- **Regions**: Checkbox "Include the following regions: European regions" with country list (UK, France, Germany, Netherlands, Spain, Italy, Belgium, Austria, Portugal, Ireland)
- **Order details**: Section "Add Item Protection to the order details"
  - Link: "View style"
  - Button: "Configure order details"
  - Text: "Add a 'Start a claim' entry point in Order details, linking customers to Chubb Care."

### 2.5 Preview (widget in context)
- **Section title**: Preview
- Simulated order summary card:
  - Product thumbnail + name, size, color, price (e.g. strikethrough $284.99, current $234.99)
  - **Subtotal**: e.g. $249.68
  - **Freight** (truck icon): e.g. $10.00
  - **Insurance fee** (shield/check icon): e.g. $4.69
  - **Total**: e.g. USD $249.68
  - Optional: loyalty line, e.g. "500 points will be awarded for order fulfillment"
- **Item protection widget** (same as checkout widget, see below) at bottom of preview card

---

## 3. Widget design (cart & checkout)

### 3.1 Visual identity
- **Icon**: Shield with checkmark (primary)
- **Title**: "Item protection" (or "Enjoy item protection" when opted-out)
- **Provider**: "Powered by Chubb" (smaller, lighter)
- **Price**: e.g. "+ $10.00" or "$4.69" on the right
- **Toggle**: Blue when on; gray when off; disabled state when not available

### 3.2 Variant A – Primary (UX-friendly)
- Slightly larger card, more copy.
- **Opted-out (default)**:
  - "Enjoy item protection"
  - "Protect your purchase from damage, loss, and theft. Learn more"
  - "+ $10.00"
  - Toggle off
- **Opted-in (selected)**:
  - "Item protection active"
  - "Your purchase is protected from damage, loss, and theft. Learn more"
  - "+ $10.00"
  - Toggle on
- **Not available (disabled)**:
  - "Item protection not available"
  - "Item protection is not available for this item. Learn more"
  - "--"
  - Toggle disabled, greyed out

### 3.3 Variant B – Compact
- Same three states; shorter copy (no "Learn more" in some placements).
- Used in Figma for **Cart** and **Checkout** integration examples.

### 3.4 In-context (Preview / Checkout)
- Card with light blue border and light blue background behind title.
- Left: Large shield + checkmark icon.
- Title: **Item protection**; "Powered by Chubb".
- **Benefits** (bullets):
  - "Compensation if the item delivered isn't as described."
  - "Covers theft & accidental damage for 30 days after delivery."
- "*T&Cs apply*" (small, italic).
- Right: Price (e.g. $4.69) + blue toggle (on/off).

### 3.5 Placement
- **Cart**: Below product list, above order summary (Subtotal, Shipping, Total); "Go to Checkout" CTA.
- **Checkout**: Inside Payment Information section; order summary can show total including protection.

---

## 4. Policy, Claims, Settlement

- **Policy**: List/table of policies (order ID, date, insurance ID, insured items, declared value, premium, currency, status) – as in IMPLEMENTATION_STEPS.
- **Claims**: List and management of claims (type, status, linked to policy).
- **Settlement**: Billing/settlement for the app (revenue share, payouts).

---

## 5. Copy and labels (from designs)

- Item Protection (feature name)
- Worry-Free Purchase (used in metrics: "Purchase rate of Worry-Free Purchase")
- Powered by Chubb
- Activate / Deactivate
- Default 'Item Protection' at checkout
- View style / Configure order details
- Start a claim / Chubb Care
- Insurance fee (in order summary)
- T&Cs apply

Implement admin UI and checkout widget to match this reference; use Figma for exact spacing, colors, and assets.

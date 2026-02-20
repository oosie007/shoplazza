"use client";

import { useEffect, useState } from "react";
import { CheckoutWidgetPreview } from "./CheckoutWidgetPreview";

const APP_URL =
  typeof window !== "undefined" ? window.location.origin : "";

type Settings = {
  activated: boolean;
  revenueShareTier: string;
  protectionPercent: number;
  pricingMode: string;
  fixedPercentAll: number;
  categoryPercents: Record<string, number>;
  excludedCategoryIds: string[];
  widgetVariant: string;
  defaultAtCheckout: boolean;
  checkoutPlacement: string;
  enablePoweredByChubb: boolean;
  offerAtCheckout: boolean;
  claimPortalConfigured: boolean;
  itemProtectionProductId?: string;
  itemProtectionVariantId?: string;
};

function getShopFromQuery(): string | null {
  if (typeof window === "undefined") return null;
  return new URLSearchParams(window.location.search).get("shop");
}

type PreviewViewport = "desktop" | "mobile";

type Category = { id: string; name: string };

export function ConfigurationContent() {
  const [shop, setShop] = useState<string | null>(null);
  const [settings, setSettings] = useState<Settings | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [previewViewport, setPreviewViewport] = useState<PreviewViewport>("desktop");
  const [previewProtectionOn, setPreviewProtectionOn] = useState(false);
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoriesFromApi, setCategoriesFromApi] = useState(false);
  const [categoriesError, setCategoriesError] = useState<string | null>(null);

  useEffect(() => {
    setShop(getShopFromQuery());
  }, []);

  useEffect(() => {
    if (!settings?.activated) setPreviewProtectionOn(false);
  }, [settings?.activated]);

  useEffect(() => {
    if (!shop) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    fetch(`${APP_URL}/api/settings?shop=${encodeURIComponent(shop)}`)
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => {
        if (!cancelled) setSettings(data ?? null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [shop]);

  useEffect(() => {
    if (!shop || settings?.pricingMode !== "per_category") {
      setCategories([]);
      setCategoriesError(null);
      return;
    }
    let cancelled = false;
    setCategoriesError(null);
    fetch(`${APP_URL}/api/categories?shop=${encodeURIComponent(shop)}`)
      .then((r) => (r.ok ? r.json() : r.json().catch(() => ({}))))
      .then((data) => {
        if (cancelled) return;
        setCategories(data?.categories ?? []);
        setCategoriesFromApi(Boolean(data?.fromApi));
        setCategoriesError(data?.error ?? null);
      })
      .catch(() => {
        if (!cancelled) {
          setCategories([]);
          setCategoriesError("Failed to load categories");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [shop, settings?.pricingMode]);

  const updateSetting = async (patch: Partial<Settings>) => {
    if (!shop || saving) return;
    setSaving(true);
    try {
      const res = await fetch(`${APP_URL}/api/settings?shop=${encodeURIComponent(shop)}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(patch),
      });
      if (res.ok) {
        const data = await res.json();
        setSettings((s) => (s ? { ...s, ...data } : null));
      }
    } finally {
      setSaving(false);
    }
  };

  const updateCategoryPercent = (categoryId: string, value: string) => {
    const num = value.trim() === "" ? undefined : Number(value);
    const next = { ...(settings?.categoryPercents ?? {}) };
    if (num === undefined || Number.isNaN(num)) {
      delete next[categoryId];
    } else {
      next[categoryId] = Math.max(0, Math.min(100, num));
    }
    updateSetting({ categoryPercents: next });
  };

  if (loading || !shop) {
    return (
      <div className="flex items-center justify-center py-12">
        <p className="text-zinc-500">
          {!shop ? "Loading…" : "Loading settings…"}
        </p>
      </div>
    );
  }

  const defaultPercent = settings?.fixedPercentAll ?? settings?.protectionPercent ?? 5;
  const samplePrice = (200 * (defaultPercent / 100)).toFixed(2);
  const subtotal = 249.68;
  const freight = 10;
  const insuranceFee = parseFloat(samplePrice);
  const total = subtotal + freight + (previewProtectionOn ? insuranceFee : 0);

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      {/* Top: Data dashboard (own section per Figma) */}
      <section>
        <h2 className="text-lg font-medium text-zinc-900">Data overview</h2>
        <p className="mt-0.5 text-sm text-zinc-500">
          The monetary values in the data overview section are shown in US dollars.
        </p>
        <div className="mt-2 flex flex-wrap items-center justify-end gap-2">
          <label className="text-sm text-zinc-600">Start date</label>
          <input
            type="date"
            className="rounded border border-zinc-300 px-2 py-1 text-sm"
          />
          <label className="text-sm text-zinc-600">End date</label>
          <input
            type="date"
            className="rounded border border-zinc-300 px-2 py-1 text-sm"
          />
        </div>
        <div className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
          <MetricCard label="Insured orders" value="0" />
          <MetricCard label="Insured order amount" value="0.00 USD" />
          <MetricCard label="Purchase rate of Item Protection" value="0%" />
          <MetricCard label="Revenue share" value="0.00 USD" />
          <MetricCard label="Estimated cost savings" value="0.00 USD" />
        </div>
      </section>

      {/* Configuration title */}
      <div>
        <h1 className="text-2xl font-semibold text-zinc-900">Configuration</h1>
        <p className="mt-1 text-sm text-zinc-600">
          Item protection helps shoppers buy with confidence.{" "}
          <a href="#" className="text-blue-600 underline hover:text-blue-700">
            Learn more
          </a>
          {shop && (
            <>
              {" · "}
              <a
                href={`${APP_URL}/api/auth?shop=${encodeURIComponent(shop)}`}
                className="text-blue-600 underline hover:text-blue-700"
              >
                Reconnect store
              </a>
              <span className="text-zinc-500"> (re-authorize to update permissions, e.g. categories)</span>
            </>
          )}
        </p>
      </div>

      {/* Same row: Config left, Preview right so preview stays visible when changing settings */}
      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 md:gap-8">
        {/* Left: Configurator */}
        <div className="space-y-6 min-w-0">
          {/* Activate + Default */}
          <section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <h2 className="text-lg font-semibold text-zinc-900">
                    Item Protection
                  </h2>
                  <span
                    className={
                      "rounded-full px-2.5 py-0.5 text-xs font-medium " +
                      (settings?.activated
                        ? "bg-green-100 text-green-800"
                        : "bg-zinc-200 text-zinc-700")
                    }
                  >
                    {settings?.activated ? "Activated" : "Deactivated"}
                  </span>
                </div>
                <label className="mt-3 flex cursor-pointer items-center gap-2">
                  <input
                    type="checkbox"
                    checked={settings?.defaultAtCheckout ?? false}
                    onChange={(e) =>
                      updateSetting({ defaultAtCheckout: e.target.checked })
                    }
                    className="h-4 w-4 rounded border-zinc-300 text-blue-600"
                  />
                  <span className="text-sm text-zinc-700">
                    Default &apos;Item Protection&apos; on at checkout
                  </span>
                </label>
              </div>
              <button
                type="button"
                onClick={() =>
                  updateSetting({ activated: !settings?.activated })
                }
                disabled={saving}
                className={
                  "shrink-0 rounded-lg px-4 py-2 text-sm font-medium transition " +
                  (settings?.activated
                    ? "border border-zinc-300 bg-white text-zinc-700 hover:bg-zinc-50"
                    : "bg-blue-600 text-white hover:bg-blue-700")
                }
              >
                {settings?.activated ? "Deactivate" : "Activate"}
              </button>
            </div>
          </section>

          {/* Pricing */}
          <section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
            <h2 className="text-lg font-medium text-zinc-900">Pricing</h2>
            <p className="mt-0.5 text-sm text-zinc-600">
              How the premium is calculated at checkout.
            </p>
            <div className="mt-3 flex flex-wrap gap-6 text-sm">
              <label className="flex items-center gap-2">
                <input
                  type="radio"
                  name="pricingMode"
                  value="fixed_percent_all"
                  checked={settings?.pricingMode !== "per_category"}
                  onChange={() =>
                    updateSetting({ pricingMode: "fixed_percent_all" })
                  }
                />
                <span>Fixed % of cart</span>
              </label>
              <label className="flex items-center gap-2">
                <input
                  type="radio"
                  name="pricingMode"
                  value="per_category"
                  checked={settings?.pricingMode === "per_category"}
                  onChange={() => updateSetting({ pricingMode: "per_category" })}
                />
                <span>% by category</span>
              </label>
            </div>
            <div className="mt-3 flex flex-wrap items-center gap-2 text-sm">
              <span>
                Default premium{" "}
                {settings?.pricingMode === "per_category"
                  ? "for uncategorized / fallback"
                  : ""}
                :
              </span>
              <input
                type="number"
                min={0}
                max={100}
                value={defaultPercent}
                onChange={(e) =>
                  updateSetting({
                    fixedPercentAll: Number(e.target.value) || 0,
                  })
                }
                className="w-16 rounded border border-zinc-300 px-2 py-1 text-right"
              />
              <span>%</span>
            </div>

            {settings?.pricingMode === "per_category" && (
              <div className="mt-4 border-t border-zinc-200 pt-4">
                <p className="text-sm font-medium text-zinc-900">
                  Premium % by category
                </p>
                <p className="mt-0.5 text-xs text-zinc-500">
                  Leave empty = category not covered. Set a number = premium % for that category.
                  {!categoriesFromApi && !categoriesError && " (Using sample categories for testing.)"}
                  {categoriesError && (
                    <span className="mt-1 block text-amber-600">
                      Could not load store categories: {categoriesError}. Reconnect store and try again, or check server logs.
                    </span>
                  )}
                </p>
                <ul className="mt-3 space-y-2">
                  {categories.map((cat) => (
                    <li
                      key={cat.id}
                      className="flex flex-wrap items-center gap-2 text-sm"
                    >
                      <span className="min-w-0 flex-1 truncate text-zinc-700">
                        {cat.name}
                      </span>
                      <input
                        type="number"
                        min={0}
                        max={100}
                        placeholder="—"
                        value={
                          settings?.categoryPercents?.[cat.id] !== undefined
                            ? String(settings.categoryPercents[cat.id])
                            : ""
                        }
                        onChange={(e) =>
                          updateCategoryPercent(cat.id, e.target.value)
                        }
                        className="w-16 rounded border border-zinc-300 px-2 py-1 text-right"
                      />
                      <span className="text-zinc-500">%</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </section>

          {/* Widget appearance */}
          <section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
            <h2 className="text-lg font-medium text-zinc-900">Widget</h2>
            <label className="mt-3 flex cursor-pointer items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={settings?.enablePoweredByChubb ?? true}
                onChange={(e) =>
                  updateSetting({ enablePoweredByChubb: e.target.checked })
                }
                className="h-4 w-4 rounded border-zinc-300 text-blue-600"
              />
              <span>Show &quot;Powered by Chubb&quot; logo</span>
            </label>
          </section>

          {/* Cart totals: we create the product automatically; merchants leave fields blank */}
          <section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
            <h2 className="text-lg font-medium text-zinc-900">Cart totals integration</h2>
            <p className="mt-1 text-sm text-zinc-700">
              <strong>No setup required.</strong> We create an &quot;Item Protection&quot; product in your store automatically (on install or the first time a customer sees the widget). The widget adds or removes that line when the customer toggles, and the total updates. Leave the fields below blank—they are filled automatically.
            </p>
            <p className="mt-2 text-sm text-zinc-500">
              If the IDs below are still empty after opening the app or loading checkout, our automatic creation may have failed (e.g. missing permission). You can then create a product named &quot;Item protection&quot; in your store and paste its product and variant IDs here as a fallback.
            </p>
            <div className="mt-3 space-y-2">
              <label className="block text-sm font-medium text-zinc-700">
                Item Protection product ID (leave blank unless fallback)
              </label>
              <input
                type="text"
                value={settings?.itemProtectionProductId ?? ""}
                onChange={(e) =>
                  updateSetting({ itemProtectionProductId: e.target.value })
                }
                placeholder="Filled automatically; or paste if using fallback"
                className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
              <label className="block text-sm font-medium text-zinc-700">
                Item Protection variant ID (leave blank unless fallback)
              </label>
              <input
                type="text"
                value={settings?.itemProtectionVariantId ?? ""}
                onChange={(e) =>
                  updateSetting({ itemProtectionVariantId: e.target.value })
                }
                placeholder="Filled automatically; or paste if using fallback"
                className="w-full rounded-md border border-zinc-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
          </section>
        </div>

        {/* Right: Checkout-style preview (product + summary + our widget) */}
        <div className="md:sticky md:top-6 md:self-start min-w-0">
          <div className="rounded-xl border border-zinc-200 bg-zinc-50/80 p-4 shadow-sm">
            <h2 className="text-lg font-medium text-zinc-900">Preview</h2>
            <p className="mt-0.5 text-sm text-zinc-500">
              How the widget will look in your checkout.
            </p>
            <div className="mt-3 flex rounded-lg bg-zinc-200 p-0.5">
              <button
                type="button"
                onClick={() => setPreviewViewport("desktop")}
                className={
                  "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition " +
                  (previewViewport === "desktop"
                    ? "bg-white text-zinc-900 shadow"
                    : "text-zinc-600 hover:text-zinc-900")
                }
              >
                Desktop
              </button>
              <button
                type="button"
                onClick={() => setPreviewViewport("mobile")}
                className={
                  "flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition " +
                  (previewViewport === "mobile"
                    ? "bg-white text-zinc-900 shadow"
                    : "text-zinc-600 hover:text-zinc-900")
                }
              >
                Mobile
              </button>
            </div>
            <div
              className={
                "mt-4 overflow-hidden rounded-lg border border-zinc-200 bg-white " +
                (previewViewport === "mobile"
                  ? "mx-auto w-[375px] max-w-full"
                  : "w-full")
              }
            >
              <div
                className={
                  "space-y-0 " +
                  (previewViewport === "mobile" ? "min-h-[480px] bg-zinc-100 p-4" : "p-4")
                }
              >
                {/* Mock checkout: product row */}
                <div className="flex gap-4 pb-4">
                  <img
                    src="/checkout-preview-blouse.png"
                    alt="Black Sleeveless Tie Neck Blouse"
                    className="h-20 w-20 shrink-0 rounded-lg object-cover"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="font-medium text-zinc-900">
                      Black Sleeveless Tie Neck Blouse
                    </p>
                    <p className="text-sm text-zinc-500">
                      Size: XS · Color: Black
                    </p>
                    <p className="mt-1 text-sm">
                      <span className="text-zinc-400 line-through">$284.99</span>{" "}
                      <span className="font-semibold text-zinc-900">$234.99</span>
                    </p>
                  </div>
                </div>
                {/* Cost breakdown: Item Protection line only when preview widget toggle is on */}
                <div className="space-y-1 border-t border-zinc-200 pt-4 text-sm">
                  <div className="flex justify-between text-zinc-600">
                    <span>Subtotal</span>
                    <span>${subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-zinc-600">
                    <span>Freight</span>
                    <span>${freight.toFixed(2)}</span>
                  </div>
                  {previewProtectionOn && (
                    <div className="flex justify-between text-zinc-600">
                      <span>Item protection</span>
                      <span>${insuranceFee.toFixed(2)}</span>
                    </div>
                  )}
                </div>
                <div className="flex justify-between border-t border-zinc-200 pt-3 text-base font-semibold">
                  <span>Total</span>
                  <span>USD ${total.toFixed(2)}</span>
                </div>
                <p className="text-xs text-zinc-500">
                  500 points will be awarded for order fulfillment
                </p>
                {/* Our actual widget */}
                <div className="mt-4 border-t border-zinc-200 pt-4">
                  <CheckoutWidgetPreview
                    price={samplePrice}
                    currencySymbol="$"
                    toggleOn={previewProtectionOn}
                    showLogo={settings?.enablePoweredByChubb ?? true}
                    isDisabled={!settings?.activated}
                    interactive={settings?.activated}
                    onToggleChange={setPreviewProtectionOn}
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-zinc-200 bg-white p-3">
      <p className="text-xs font-medium uppercase tracking-wide text-zinc-500">
        {label}
      </p>
      <p className="mt-0.5 text-base font-semibold text-zinc-900">{value}</p>
    </div>
  );
}

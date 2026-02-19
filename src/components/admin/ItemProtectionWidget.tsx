"use client";

/**
 * Item protection widget – matches Figma design (Variant B compact, opted-in/out).
 * Used in admin Preview and will be reused at checkout.
 * States: optedOut (default), optedIn (selected), notAvailable (disabled).
 */
export function ItemProtectionWidget({
  state = "optedIn",
  price = "$4.69",
  compact = true,
  showPoweredByChubb = true,
}: {
  state?: "optedOut" | "optedIn" | "notAvailable";
  price?: string;
  compact?: boolean;
  showPoweredByChubb?: boolean;
}) {
  const isDisabled = state === "notAvailable";
  const isOn = state === "optedIn";

  const titles: Record<typeof state, string> = {
    optedOut: "Enjoy item protection",
    optedIn: "Item protection active",
    notAvailable: "Item protection not available",
  };
  const descriptions: Record<typeof state, string> = {
    optedOut: "Protect your purchase from damage, loss, and theft. Learn more",
    optedIn: "Your purchase is protected from damage, loss, and theft.",
    notAvailable: "Item protection is not available for this item.",
  };

  return (
    <div
      className={
        "rounded-xl border bg-white p-4 " +
        (isOn
          ? "border-blue-200 bg-blue-50/50"
          : isDisabled
            ? "border-zinc-200 bg-zinc-50 opacity-75"
            : "border-zinc-200")
      }
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex min-w-0 flex-1 gap-3">
          <div
            className={
              "flex h-10 w-10 shrink-0 items-center justify-center rounded-lg " +
              (isOn ? "bg-blue-100 text-blue-600" : isDisabled ? "bg-zinc-200 text-zinc-500" : "bg-zinc-100 text-zinc-600")
            }
          >
            <ShieldCheckIcon className="h-5 w-5" />
          </div>
          <div className="min-w-0">
            <p className="font-semibold text-zinc-900">{titles[state]}</p>
            {showPoweredByChubb && (
              <div className="mt-0.5 flex items-center gap-1 text-xs text-zinc-500">
                <span>Powered by</span>
                <ChubbLogo className="h-3 text-zinc-700" />
              </div>
            )}
            <p className={`mt-0.5 text-sm text-zinc-600 ${compact ? "" : "mt-1"}`}>
              {descriptions[state]}
            </p>
            {!compact && isOn && (
              <ul className="mt-2 list-inside list-disc text-sm text-zinc-600">
                <li>Compensation if the item delivered isn&apos;t as described.</li>
                <li>Covers theft & accidental damage for 30 days after delivery.</li>
              </ul>
            )}
            {!compact && (
              <p className="mt-1 text-xs italic text-zinc-500">*T&Cs apply</p>
            )}
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-3">
          <span
            className={
              "text-sm font-medium " +
              (isDisabled ? "text-zinc-400" : "text-zinc-900")
            }
          >
            {isDisabled ? "--" : `+ ${price}`}
          </span>
          <Toggle on={isOn} disabled={isDisabled} />
        </div>
      </div>
    </div>
  );
}

function ShieldCheckIcon({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      fill="none"
      viewBox="0 0 24 24"
      stroke="currentColor"
      strokeWidth={2}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
      />
    </svg>
  );
}

function ChubbLogo({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 150 16"
      aria-hidden="true"
      fill="currentColor"
    >
      <path d="M21.6232 2.86698V0.011922H4.05722C1.35995 0.011922 0 1.40517 0 3.66633V12.3456C0 14.6068 1.35995 16 4.05722 16H21.6232V13.1451H3.0599V2.86698H21.6232ZM33.3414 6.54421V0.011922H30.3496V16H33.3414V9.35351H49.3209V16H52.3128V0.011922H49.3209V6.54421H33.3414ZM81.3705 0.011922V13.1451H65.6178V0.011922H62.6259V12.3456C62.6259 14.6068 63.9857 16 66.6831 16H80.3052C83.0026 16 84.3623 14.6068 84.3623 12.3456V0.011922H81.3705ZM113.079 16C116.026 16 117.386 14.7667 117.386 12.6426V10.7697C117.386 10.1758 117.182 9.90176 116.751 9.53622L114.847 7.89179L116.751 6.24727C117.182 5.88182 117.386 5.60771 117.386 5.0139V3.36947C117.386 1.24526 116.026 0.011922 113.079 0.011922H94.6744V16H113.079ZM97.6663 2.72991H114.326V6.61269H97.6663V2.72991ZM114.326 13.2821H97.6663V9.28503H114.326V13.2821ZM145.265 16C148.211 16 149.571 14.7667 149.571 12.6426V10.7697C149.571 10.1758 149.367 9.90176 148.936 9.53622L147.032 7.89179L148.936 6.24727C149.367 5.88182 149.571 5.60771 149.571 5.0139V3.36947C149.571 1.24526 148.211 0.011922 145.265 0.011922H126.86V16H145.265ZM129.852 2.72991H146.511V6.61269H129.852V2.72991ZM146.511 13.2821H129.852V9.28503H146.511V13.2821Z" />
    </svg>
  );
}

function Toggle({ on, disabled }: { on: boolean; disabled: boolean }) {
  return (
    <div
      className={
        "relative h-6 w-11 shrink-0 rounded-full transition-colors " +
        (disabled
          ? "cursor-not-allowed bg-zinc-300"
          : on
            ? "bg-blue-600"
            : "bg-zinc-300")
      }
      role="switch"
      aria-checked={on}
      aria-disabled={disabled}
    >
      <span
        className={
          "absolute top-1 h-4 w-4 rounded-full bg-white shadow transition-transform " +
          (on ? "left-6 translate-x-0" : "left-1")
        }
      />
    </div>
  );
}

/**
 * Supported countries configuration for CD Insure
 * These are the only countries where the item protection widget is available
 */

export const SUPPORTED_COUNTRIES = {
  GB: { code: "GB", name: "United Kingdom", flag: "🇬🇧" },
  FR: { code: "FR", name: "France", flag: "🇫🇷" },
  CH: { code: "CH", name: "Switzerland", flag: "🇨🇭" },
  NL: { code: "NL", name: "Netherlands", flag: "🇳🇱" },
} as const;

export type CountryCode = keyof typeof SUPPORTED_COUNTRIES;

/**
 * Only these 4 countries are allowed for shipping item protection
 */
export const ALLOWED_SHIPPING_COUNTRIES: CountryCode[] = ["GB", "FR", "CH", "NL"];

/**
 * Check if a country code is supported for store installation
 */
export function isSupportedCountry(code?: string): code is CountryCode {
  return !!code && code in SUPPORTED_COUNTRIES;
}

/**
 * Check if a country code is allowed for shipping
 * (Currently same as supported countries, but kept separate for flexibility)
 */
export function isShippingAllowed(code?: string): boolean {
  return !!code && ALLOWED_SHIPPING_COUNTRIES.includes(code as CountryCode);
}

/**
 * Get country name from code
 */
export function getCountryName(code?: string): string {
  if (!code) return "Unknown";
  const country = SUPPORTED_COUNTRIES[code.toUpperCase() as CountryCode];
  return country?.name ?? code;
}

/**
 * Widget injection point configuration
 * Determines where the Item Protection widget is injected at checkout
 */
export const WIDGET_INJECTION_POINT = "checkout" as const; // "checkout" | "cart"

/**
 * Get the default widget injection point
 */
export function getDefaultWidgetInjectionPoint(): typeof WIDGET_INJECTION_POINT {
  return WIDGET_INJECTION_POINT;
}

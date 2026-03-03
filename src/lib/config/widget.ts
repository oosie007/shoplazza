/**
 * Widget placement configuration
 * Defines where the Item Protection widget can be displayed
 */

export type WidgetInjectionPoint = "checkout" | "cart";

export interface WidgetPlacementOption {
  value: WidgetInjectionPoint;
  label: string;
  description: string;
}

/**
 * Available widget placement options for merchants to choose from
 */
export const WIDGET_PLACEMENT_OPTIONS: WidgetPlacementOption[] = [
  {
    value: "checkout",
    label: "Checkout page",
    description: "Widget appears during purchase. Customer may need to reload page to see updated total.",
  },
  {
    value: "cart",
    label: "Cart page",
    description: "Widget appears before checkout. Total updates immediately (recommended).",
  },
];

/**
 * Get widget placement option by value
 */
export function getWidgetPlacementOption(
  value: WidgetInjectionPoint
): WidgetPlacementOption | undefined {
  return WIDGET_PLACEMENT_OPTIONS.find((opt) => opt.value === value);
}

/**
 * Get default widget injection point
 */
export function getDefaultWidgetInjectionPoint(): WidgetInjectionPoint {
  return "checkout";
}

import { z } from "zod";

/** Shop query param (e.g. store.myshoplaza.com) */
export const shopParamSchema = z.string().min(1, "Missing shop").max(256);

/** PATCH /api/settings body – only allow known keys and types */
export const settingsPatchSchema = z
  .object({
    activated: z.boolean().optional(),
    revenueShareTier: z.string().optional(),
    protectionPercent: z.number().min(0).max(100).optional(),
    pricingMode: z.enum(["fixed_percent_all", "per_category"]).optional(),
    fixedPercentAll: z.number().min(0).max(100).optional(),
    categoryPercents: z.record(z.string(), z.number().min(0).max(100)).optional(),
    excludedCategoryIds: z.array(z.string()).optional(),
    widgetVariant: z.string().optional(),
    defaultAtCheckout: z.boolean().optional(),
    checkoutPlacement: z.string().optional(),
    enablePoweredByChubb: z.boolean().optional(),
    offerAtCheckout: z.boolean().optional(),
    claimPortalConfigured: z.boolean().optional(),
  });

/** POST /api/checkout/apply-fee body */
export const applyFeeSchema = z.object({
  shop: z.string().min(1),
  order_token: z.string().min(1),
  amount: z.string().optional(),
  label: z.string().optional(),
  enabled: z.boolean().optional(),
});

export type SettingsPatch = z.infer<typeof settingsPatchSchema>;
export type ApplyFeeBody = z.infer<typeof applyFeeSchema>;

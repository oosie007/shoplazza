/**
 * Backend config: which categories (collections) are eligible for item protection.
 * - Empty array = all merchant categories are eligible (merchant sets % per category or leaves empty = not covered).
 * - Non-empty = only these category IDs are shown/eligible (e.g. for testing or product restrictions).
 * Set via env APPLICABLE_CATEGORY_IDS (comma-separated) or leave unset for "all".
 */
const fromEnv =
  typeof process !== "undefined" && process.env.APPLICABLE_CATEGORY_IDS
    ? process.env.APPLICABLE_CATEGORY_IDS.split(",").map((s) => s.trim()).filter(Boolean)
    : [];

/** If non-empty, only these category IDs are eligible; empty means all categories. */
export const APPLICABLE_CATEGORY_IDS: string[] = fromEnv;

export function filterApplicableCategories<T extends { id: string }>(
  categories: T[]
): T[] {
  if (APPLICABLE_CATEGORY_IDS.length === 0) return categories;
  const set = new Set(APPLICABLE_CATEGORY_IDS);
  return categories.filter((c) => set.has(c.id));
}

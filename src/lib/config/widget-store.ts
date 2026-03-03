import fs from "fs";
import path from "path";
import { WidgetInjectionPoint } from "./widget";

const CONFIG_DIR = path.join(process.cwd(), ".widget-config");
const CONFIG_FILE_PATH = (shopDomain: string) =>
  path.join(CONFIG_DIR, `${shopDomain}.json`);

interface WidgetConfig {
  shopDomain: string;
  widgetInjectionPoint: WidgetInjectionPoint;
  updatedAt: string;
}

/**
 * Ensure config directory exists
 */
function ensureConfigDir() {
  if (!fs.existsSync(CONFIG_DIR)) {
    fs.mkdirSync(CONFIG_DIR, { recursive: true });
  }
}

/**
 * Get widget injection point for a shop from JSON file
 */
export function getWidgetInjectionPoint(
  shopDomain: string
): WidgetInjectionPoint {
  try {
    ensureConfigDir();
    const filePath = CONFIG_FILE_PATH(shopDomain);
    if (fs.existsSync(filePath)) {
      const data = JSON.parse(fs.readFileSync(filePath, "utf-8"));
      return data.widgetInjectionPoint ?? "checkout";
    }
  } catch (error) {
    console.error(
      `[widget-store] Error reading widget config for ${shopDomain}:`,
      error
    );
  }
  return "checkout";
}

/**
 * Save widget injection point for a shop to JSON file
 */
export function saveWidgetInjectionPoint(
  shopDomain: string,
  injectionPoint: WidgetInjectionPoint
): void {
  try {
    ensureConfigDir();
    const filePath = CONFIG_FILE_PATH(shopDomain);
    const config: WidgetConfig = {
      shopDomain,
      widgetInjectionPoint: injectionPoint,
      updatedAt: new Date().toISOString(),
    };
    fs.writeFileSync(filePath, JSON.stringify(config, null, 2));
  } catch (error) {
    console.error(
      `[widget-store] Error saving widget config for ${shopDomain}:`,
      error
    );
    throw error;
  }
}

/**
 * Delete widget config file for a shop
 */
export function deleteWidgetConfig(shopDomain: string): void {
  try {
    const filePath = CONFIG_FILE_PATH(shopDomain);
    if (fs.existsSync(filePath)) {
      fs.unlinkSync(filePath);
    }
  } catch (error) {
    console.error(
      `[widget-store] Error deleting widget config for ${shopDomain}:`,
      error
    );
  }
}

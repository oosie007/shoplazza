/**
 * Main entry point for the Shoplazza Add-On Widget
 * Provides a global interface for initializing and managing the widget
 */

import WidgetCore from './core/WidgetCore.js';
import CartService from './cart/CartService.js';
import ThemeManager from './ui/ThemeManager.js';

// Global widget class
class ShoplazzaAddonWidget {
  constructor(config = {}) {
    this.instances = new Map();
    this.defaultConfig = {
      debug: false,
      autoInit: true,
      autoDetectProduct: true,
      ...config,
    };
  }

  /**
   * Initialize widget with configuration
   */
  init(config = {}) {
    const finalConfig = { ...this.defaultConfig, ...config };
    
    try {
      const widget = new WidgetCore(finalConfig);
      const instanceId = this.generateInstanceId();
      
      this.instances.set(instanceId, widget);
      
      if (finalConfig.debug) {
        console.log('[Shoplazza Add-On Widget] Initialized with config:', finalConfig);
      }
      
      return {
        id: instanceId,
        widget,
        destroy: () => this.destroy(instanceId),
        getState: () => widget.getState(),
        updateConfig: (newConfig) => widget.updateConfig(newConfig),
      };
    } catch (error) {
      console.error('[Shoplazza Add-On Widget] Initialization failed:', error);
      throw error;
    }
  }

  /**
   * Initialize widget on specific product page
   */
  initForProduct(productId, config = {}) {
    return this.init({ ...config, productId, autoDetectProduct: false });
  }

  /**
   * Initialize multiple widgets
   */
  initMultiple(configs) {
    return configs.map(config => this.init(config));
  }

  /**
   * Destroy widget instance
   */
  destroy(instanceId) {
    const widget = this.instances.get(instanceId);
    if (widget) {
      widget.destroy();
      this.instances.delete(instanceId);
      return true;
    }
    return false;
  }

  /**
   * Destroy all widget instances
   */
  destroyAll() {
    this.instances.forEach((widget, id) => {
      widget.destroy();
    });
    this.instances.clear();
  }

  /**
   * Get widget instance
   */
  getInstance(instanceId) {
    return this.instances.get(instanceId);
  }

  /**
   * Get all widget instances
   */
  getAllInstances() {
    return Array.from(this.instances.values());
  }

  /**
   * Update default configuration
   */
  setDefaultConfig(config) {
    this.defaultConfig = { ...this.defaultConfig, ...config };
  }

  /**
   * Create cart service instance
   */
  createCartService(config = {}) {
    return new CartService({ ...this.defaultConfig, ...config });
  }

  /**
   * Create theme manager instance
   */
  createThemeManager(config = {}) {
    return new ThemeManager({ ...this.defaultConfig, ...config });
  }

  /**
   * Utility method to check if product has add-ons
   */
  async hasAddOns(shop, productId) {
    try {
      const response = await fetch(`/api/products/${productId}?shop=${encodeURIComponent(shop)}`);
      if (response.ok) {
        const data = await response.json();
        return data.isEnabled || false;
      }
      return false;
    } catch (error) {
      console.warn('[Shoplazza Add-On Widget] Failed to check add-ons:', error);
      return false;
    }
  }

  /**
   * Auto-initialize on page load if configured
   */
  autoInit() {
    // Check for auto-init configuration in meta tags or global variables
    const metaConfig = this.getMetaConfig();
    const globalConfig = window.ShoplazzaAddonConfig || {};
    
    const config = { ...metaConfig, ...globalConfig };
    
    if (config.autoInit !== false) {
      // Wait for DOM ready
      if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
          this.init(config);
        });
      } else {
        this.init(config);
      }
    }
  }

  /**
   * Get configuration from meta tags
   */
  getMetaConfig() {
    const config = {};
    
    const metaTags = {
      'shoplazza-addon-shop': 'shop',
      'shoplazza-addon-product-id': 'productId',
      'shoplazza-addon-api-endpoint': 'apiEndpoint',
      'shoplazza-addon-theme': 'theme',
      'shoplazza-addon-debug': 'debug',
    };

    Object.entries(metaTags).forEach(([metaName, configKey]) => {
      const meta = document.querySelector(`meta[name="${metaName}"]`);
      if (meta) {
        let value = meta.getAttribute('content');
        
        // Parse boolean values
        if (value === 'true') value = true;
        if (value === 'false') value = false;
        
        // Parse numeric values
        if (configKey === 'productId' && value) {
          value = parseInt(value, 10);
        }
        
        config[configKey] = value;
      }
    });

    return config;
  }

  /**
   * Generate unique instance ID
   */
  generateInstanceId() {
    return `shoplazza-addon-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Get version information
   */
  getVersion() {
    return '1.0.0';
  }

  /**
   * Get debug information
   */
  getDebugInfo() {
    return {
      version: this.getVersion(),
      instances: this.instances.size,
      defaultConfig: this.defaultConfig,
      userAgent: navigator.userAgent,
      url: window.location.href,
      timestamp: new Date().toISOString(),
    };
  }
}

// Create global instance
const shoplazzaAddon = new ShoplazzaAddonWidget();

// Auto-initialize if enabled
shoplazzaAddon.autoInit();

// Expose to global scope
window.ShoplazzaAddon = shoplazzaAddon;

// Also expose individual classes for advanced usage
window.ShoplazzaAddon.WidgetCore = WidgetCore;
window.ShoplazzaAddon.CartService = CartService;
window.ShoplazzaAddon.ThemeManager = ThemeManager;

// Export for module usage
export default shoplazzaAddon;
export { WidgetCore, CartService, ThemeManager };
/**
 * Cart service for handling Shoplazza cart operations
 * Manages add-ons in the cart context
 */
class CartService {
  constructor(config = {}) {
    this.config = {
      debug: config.debug || false,
      retryAttempts: config.retryAttempts || 3,
      retryDelay: config.retryDelay || 1000,
      ...config,
    };

    this.state = {
      cart: null,
      lastUpdate: null,
    };
  }

  /**
   * Get current cart contents
   */
  async getCart() {
    try {
      const response = await fetch('/cart.js', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to get cart: ${response.status}`);
      }

      this.state.cart = await response.json();
      this.state.lastUpdate = Date.now();
      
      this.log('Cart loaded', this.state.cart);
      return this.state.cart;
    } catch (error) {
      this.log(`Failed to get cart: ${error.message}`, 'error');
      throw error;
    }
  }

  /**
   * Add item to cart
   */
  async addToCart(variantId, quantity = 1, properties = {}) {
    const data = {
      id: variantId,
      quantity,
      properties,
    };

    return this.makeCartRequest('/cart/add.js', 'POST', data);
  }

  /**
   * Update cart item quantities
   */
  async updateCart(updates) {
    const data = { updates };
    return this.makeCartRequest('/cart/update.js', 'POST', data);
  }

  /**
   * Change specific cart line
   */
  async changeCartLine(line, quantity, properties = {}) {
    const data = {
      line,
      quantity,
      properties,
    };

    return this.makeCartRequest('/cart/change.js', 'POST', data);
  }

  /**
   * Remove item from cart
   */
  async removeFromCart(variantId) {
    const updates = { [variantId]: 0 };
    return this.updateCart(updates);
  }

  /**
   * Clear entire cart
   */
  async clearCart() {
    return this.makeCartRequest('/cart/clear.js', 'POST');
  }

  /**
   * Add multiple items to cart (for main product + add-ons)
   */
  async addMultipleToCart(items) {
    const results = [];
    
    for (const item of items) {
      try {
        const result = await this.addToCart(
          item.variantId,
          item.quantity,
          item.properties
        );
        results.push({ success: true, item, result });
      } catch (error) {
        this.log(`Failed to add item ${item.variantId}: ${error.message}`, 'error');
        results.push({ success: false, item, error });
      }
    }

    return results;
  }

  /**
   * Check if add-on is already in cart
   */
  async isAddOnInCart(addOnVariantId, mainProductVariantId) {
    const cart = await this.getCart();
    
    return cart.items.some(item => 
      item.variant_id === addOnVariantId && 
      item.properties._addon === 'true' &&
      item.properties._addon_for === mainProductVariantId.toString()
    );
  }

  /**
   * Get all add-on items in cart
   */
  async getAddOnItems() {
    const cart = await this.getCart();
    
    return cart.items.filter(item => 
      item.properties && item.properties._addon === 'true'
    );
  }

  /**
   * Remove add-ons for a specific product
   */
  async removeAddOnsForProduct(productVariantId) {
    const cart = await this.getCart();
    const addOnItems = cart.items.filter(item => 
      item.properties && 
      item.properties._addon === 'true' &&
      item.properties._addon_for === productVariantId.toString()
    );

    const updates = {};
    addOnItems.forEach(item => {
      updates[item.variant_id] = 0;
    });

    if (Object.keys(updates).length > 0) {
      return this.updateCart(updates);
    }

    return cart;
  }

  /**
   * Sync add-ons with cart (add missing, remove unchecked)
   */
  async syncAddOnsWithCart(mainProductVariantId, selectedAddOns, quantity = 1) {
    try {
      const cart = await this.getCart();
      const existingAddOns = cart.items.filter(item => 
        item.properties &&
        item.properties._addon === 'true' &&
        item.properties._addon_for === mainProductVariantId.toString()
      );

      const updates = {};
      const toAdd = [];

      // Check existing add-ons
      existingAddOns.forEach(item => {
        const addOnId = item.properties._addon_id;
        if (selectedAddOns.has(parseInt(addOnId, 10))) {
          // Keep this add-on, update quantity if needed
          if (item.quantity !== quantity) {
            updates[item.variant_id] = quantity;
          }
        } else {
          // Remove this add-on
          updates[item.variant_id] = 0;
        }
      });

      // Check for new add-ons to add
      for (const addOn of selectedAddOns.values()) {
        const exists = existingAddOns.some(item => 
          item.properties._addon_id === addOn.id.toString()
        );

        if (!exists && addOn.addOnVariantId) {
          toAdd.push({
            variantId: addOn.addOnVariantId,
            quantity,
            properties: {
              '_addon': 'true',
              '_addon_for': mainProductVariantId.toString(),
              '_addon_id': addOn.id.toString(),
              '_addon_title': addOn.addOn.title,
            },
          });
        }
      }

      // Apply updates
      if (Object.keys(updates).length > 0) {
        await this.updateCart(updates);
      }

      // Add new items
      if (toAdd.length > 0) {
        await this.addMultipleToCart(toAdd);
      }

      return this.getCart();
    } catch (error) {
      this.log(`Failed to sync add-ons: ${error.message}`, 'error');
      throw error;
    }
  }

  /**
   * Get cart total including add-ons
   */
  async getCartTotal() {
    const cart = await this.getCart();
    return {
      totalPrice: cart.total_price,
      itemCount: cart.item_count,
      addOnCount: cart.items.filter(item => 
        item.properties && item.properties._addon === 'true'
      ).length,
    };
  }

  /**
   * Make a cart API request with retry logic
   */
  async makeCartRequest(endpoint, method, data = null, attempt = 1) {
    try {
      const options = {
        method,
        headers: {
          'Content-Type': 'application/json',
        },
      };

      if (data && (method === 'POST' || method === 'PUT')) {
        options.body = JSON.stringify(data);
      }

      const response = await fetch(endpoint, options);

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Cart API error ${response.status}: ${errorText}`);
      }

      const result = await response.json();
      this.state.cart = result;
      this.state.lastUpdate = Date.now();

      this.log(`Cart ${method} ${endpoint}`, result);
      return result;
    } catch (error) {
      this.log(`Cart request failed (attempt ${attempt}): ${error.message}`, 'error');

      // Retry logic
      if (attempt < this.config.retryAttempts) {
        await this.delay(this.config.retryDelay * attempt);
        return this.makeCartRequest(endpoint, method, data, attempt + 1);
      }

      throw error;
    }
  }

  /**
   * Delay utility for retries
   */
  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Subscribe to cart changes
   */
  onCartChange(callback) {
    // Poll for cart changes
    const checkInterval = 5000; // 5 seconds
    
    const checkForChanges = async () => {
      try {
        const currentCart = await this.getCart();
        if (this.state.lastUpdate && currentCart) {
          callback(currentCart);
        }
      } catch (error) {
        this.log(`Error checking cart changes: ${error.message}`, 'error');
      }
    };

    const intervalId = setInterval(checkForChanges, checkInterval);
    
    // Return cleanup function
    return () => clearInterval(intervalId);
  }

  /**
   * Format price for display
   */
  formatPrice(cents, currency = 'USD') {
    const amount = cents / 100;
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount);
  }

  /**
   * Get add-on summary for cart display
   */
  async getAddOnSummary() {
    const cart = await this.getCart();
    const addOns = cart.items.filter(item => 
      item.properties && item.properties._addon === 'true'
    );

    const summary = {
      count: addOns.length,
      totalPrice: addOns.reduce((sum, item) => sum + item.final_line_price, 0),
      items: addOns.map(item => ({
        id: item.id,
        variantId: item.variant_id,
        title: item.properties._addon_title || item.title,
        price: item.final_price,
        quantity: item.quantity,
        linePrice: item.final_line_price,
        forProduct: item.properties._addon_for,
      })),
    };

    return summary;
  }

  /**
   * Validate cart before checkout
   */
  async validateCart() {
    const cart = await this.getCart();
    const issues = [];

    // Check for orphaned add-ons (add-ons without main product)
    const addOns = cart.items.filter(item => 
      item.properties && item.properties._addon === 'true'
    );

    const mainProducts = cart.items.filter(item => 
      !item.properties || item.properties._addon !== 'true'
    );

    addOns.forEach(addOn => {
      const mainProductExists = mainProducts.some(product => 
        product.variant_id.toString() === addOn.properties._addon_for
      );

      if (!mainProductExists) {
        issues.push({
          type: 'orphaned_addon',
          item: addOn,
          message: `Add-on "${addOn.title}" has no corresponding main product`,
        });
      }
    });

    return {
      valid: issues.length === 0,
      issues,
    };
  }

  /**
   * Clean up orphaned add-ons
   */
  async cleanupOrphanedAddOns() {
    const validation = await this.validateCart();
    
    if (!validation.valid) {
      const orphanedAddOns = validation.issues
        .filter(issue => issue.type === 'orphaned_addon')
        .map(issue => issue.item);

      if (orphanedAddOns.length > 0) {
        const updates = {};
        orphanedAddOns.forEach(item => {
          updates[item.variant_id] = 0;
        });

        await this.updateCart(updates);
        this.log(`Cleaned up ${orphanedAddOns.length} orphaned add-ons`);
      }
    }

    return this.getCart();
  }

  /**
   * Logging utility
   */
  log(message, level = 'info') {
    if (!this.config.debug) return;

    const prefix = '[Cart Service]';
    switch (level) {
      case 'error':
        console.error(prefix, message);
        break;
      case 'warn':
        console.warn(prefix, message);
        break;
      default:
        console.log(prefix, message);
    }
  }

  /**
   * Get current state
   */
  getState() {
    return { ...this.state };
  }
}

export default CartService;
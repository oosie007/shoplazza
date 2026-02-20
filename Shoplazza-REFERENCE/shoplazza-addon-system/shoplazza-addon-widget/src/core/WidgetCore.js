/**
 * Core widget class for Shoplazza Add-On system
 * Handles widget initialization, configuration, and lifecycle management
 */
class WidgetCore {
  constructor(config = {}) {
    this.config = {
      // Default configuration
      apiEndpoint: config.apiEndpoint || '',
      shop: config.shop || '',
      productId: config.productId || null,
      theme: config.theme || 'default',
      position: config.position || 'below-price',
      animation: config.animation || 'highlight',
      debug: config.debug || false,
      // Widget behavior
      autoInit: config.autoInit !== false,
      autoDetectProduct: config.autoDetectProduct !== false,
      showDescription: config.showDescription !== false,
      showImage: config.showImage || false,
      // Styling
      primaryColor: config.primaryColor || '#007bff',
      textColor: config.textColor || '#333333',
      borderColor: config.borderColor || '#e1e8ed',
      selectedBackgroundColor: config.selectedBackgroundColor || '#e7f3ff',
      customCss: config.customCss || '',
      // Callbacks
      onInit: config.onInit || null,
      onAddOnToggle: config.onAddOnToggle || null,
      onCartUpdate: config.onCartUpdate || null,
      onError: config.onError || null,
      ...config,
    };

    this.state = {
      initialized: false,
      addOns: [],
      selectedAddOns: new Set(),
      cartItems: [],
      loading: false,
      error: null,
    };

    this.elements = {
      container: null,
      widgets: [],
    };

    // Auto-initialize if enabled
    if (this.config.autoInit) {
      this.init();
    }
  }

  /**
   * Initialize the widget
   */
  async init() {
    try {
      this.log('Initializing Shoplazza Add-On Widget...');

      // Validate configuration
      if (!this.validateConfig()) {
        throw new Error('Invalid widget configuration');
      }

      // Wait for DOM to be ready
      await this.waitForDOM();

      // Detect product if auto-detection is enabled
      if (this.config.autoDetectProduct && !this.config.productId) {
        this.config.productId = this.detectProductId();
      }

      if (!this.config.productId) {
        throw new Error('Product ID not found. Cannot initialize widget.');
      }

      // Load add-on configuration
      await this.loadAddOnConfiguration();

      // Create widget elements
      this.createWidgetElements();

      // Apply styling
      this.applyStyles();

      // Set up event listeners
      this.setupEventListeners();

      // Load cart state
      await this.loadCartState();

      this.state.initialized = true;
      this.log('Widget initialized successfully');

      // Call initialization callback
      if (this.config.onInit) {
        this.config.onInit(this);
      }
    } catch (error) {
      this.handleError('Failed to initialize widget', error);
    }
  }

  /**
   * Validate widget configuration
   */
  validateConfig() {
    const required = ['shop'];
    
    for (const field of required) {
      if (!this.config[field]) {
        this.log(`Missing required configuration: ${field}`, 'error');
        return false;
      }
    }

    return true;
  }

  /**
   * Wait for DOM to be ready
   */
  waitForDOM() {
    return new Promise((resolve) => {
      if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', resolve);
      } else {
        resolve();
      }
    });
  }

  /**
   * Detect product ID from current page
   */
  detectProductId() {
    // Try multiple methods to detect product ID
    const methods = [
      // From meta tags
      () => {
        const meta = document.querySelector('meta[name="product-id"]');
        return meta ? meta.getAttribute('content') : null;
      },
      // From Shoplazza page data
      () => {
        if (window.ShoplazzaAnalytics && window.ShoplazzaAnalytics.meta) {
          return window.ShoplazzaAnalytics.meta.product?.id;
        }
        return null;
      },
      // From URL patterns
      () => {
        const match = window.location.pathname.match(/\/products\/(\d+)/);
        return match ? match[1] : null;
      },
      // From form data
      () => {
        const form = document.querySelector('form[action*="/cart/add"]');
        if (form) {
          const productInput = form.querySelector('input[name="id"]');
          return productInput ? productInput.value : null;
        }
        return null;
      },
    ];

    for (const method of methods) {
      try {
        const productId = method();
        if (productId) {
          this.log(`Detected product ID: ${productId}`);
          return parseInt(productId, 10);
        }
      } catch (error) {
        this.log(`Product detection method failed: ${error.message}`, 'warn');
      }
    }

    this.log('Could not detect product ID', 'warn');
    return null;
  }

  /**
   * Load add-on configuration from API
   */
  async loadAddOnConfiguration() {
    try {
      this.state.loading = true;
      
      // Check if demo mode is enabled with mock data
      if (this.config.demoMode && this.config.mockAddOns) {
        this.state.addOns = this.config.mockAddOns;
        this.log('Demo mode: Using mock add-on configuration', this.config.mockAddOns);
        return;
      }
      
      const headers = {
        'Content-Type': 'application/json'
      };
      
      // Add demo mode headers if in demo mode
      if (this.config.demoMode) {
        headers['X-Demo-Mode'] = 'true';
        headers['X-Shoplazza-Hmac-Sha256'] = 'demo-signature';
      }
      
      const response = await fetch(`${this.config.apiEndpoint}/api/products/${this.config.productId}?shop=${encodeURIComponent(this.config.shop)}`, {
        method: 'GET',
        headers: headers
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.isEnabled) {
          this.state.addOns = [data];
          this.log('Add-on configuration loaded', data);
        } else {
          this.log('Add-on is disabled for this product');
        }
      } else if (response.status === 404) {
        this.log('No add-on configuration found for this product');
      } else {
        throw new Error(`API error: ${response.status}`);
      }
    } catch (error) {
      this.handleError('Failed to load add-on configuration', error);
    } finally {
      this.state.loading = false;
    }
  }

  /**
   * Create widget DOM elements
   */
  createWidgetElements() {
    if (this.state.addOns.length === 0) {
      this.log('No add-ons to display');
      return;
    }

    // Find insertion point
    const insertionPoint = this.findInsertionPoint();
    if (!insertionPoint) {
      throw new Error('Could not find suitable insertion point for widget');
    }

    // Create container
    this.elements.container = document.createElement('div');
    this.elements.container.className = 'shoplazza-addon-container';
    this.elements.container.dataset.theme = this.config.theme;

    // Create widgets for each add-on
    this.state.addOns.forEach((addOn, index) => {
      const widget = this.createAddOnWidget(addOn, index);
      this.elements.widgets.push(widget);
      this.elements.container.appendChild(widget);
    });

    // Insert into DOM
    this.insertWidget(insertionPoint, this.elements.container);

    this.log('Widget elements created and inserted');
  }

  /**
   * Find the best insertion point for the widget
   */
  findInsertionPoint() {
    const positions = {
      'above-price': [
        '.product-price',
        '.price',
        '[class*="price"]',
      ],
      'below-price': [
        '.product-price',
        '.price',
        '[class*="price"]',
      ],
      'above-add-to-cart': [
        'button[type="submit"][name="add"]',
        '.product-form__cart',
        '.product-form__buttons',
        '.add-to-cart',
      ],
      'below-add-to-cart': [
        'button[type="submit"][name="add"]',
        '.product-form__cart',
        '.product-form__buttons',
        '.add-to-cart',
      ],
    };

    const selectors = positions[this.config.position] || positions['below-price'];
    
    for (const selector of selectors) {
      const element = document.querySelector(selector);
      if (element) {
        return { element, position: this.config.position };
      }
    }

    // Fallback to product form
    const productForm = document.querySelector('form[action*="/cart/add"]');
    if (productForm) {
      return { element: productForm, position: 'below-add-to-cart' };
    }

    return null;
  }

  /**
   * Insert widget into DOM at the specified position
   */
  insertWidget(insertionPoint, widget) {
    const { element, position } = insertionPoint;

    switch (position) {
      case 'above-price':
      case 'above-add-to-cart':
        element.parentNode.insertBefore(widget, element);
        break;
      case 'below-price':
      case 'below-add-to-cart':
      default:
        element.parentNode.insertBefore(widget, element.nextSibling);
        break;
    }
  }

  /**
   * Create a single add-on widget
   */
  createAddOnWidget(addOn, index) {
    const widget = document.createElement('div');
    widget.className = 'shoplazza-addon-widget';
    widget.dataset.addOnId = addOn.id;
    widget.dataset.productId = addOn.productId;

    const isSelected = this.state.selectedAddOns.has(addOn.id);

    widget.innerHTML = `
      <div class="addon-container ${isSelected ? 'selected' : ''}">
        <label class="addon-label">
          <input type="checkbox" class="addon-checkbox" ${isSelected ? 'checked' : ''}>
          <span class="addon-checkmark"></span>
          <div class="addon-content">
            <div class="addon-main">
              <span class="addon-title">${this.escapeHtml(addOn.addOn.displayText)}</span>
              <span class="addon-price">+${this.escapeHtml(addOn.addOn.price)}</span>
            </div>
            ${this.config.showDescription && addOn.addOn.description ? `
              <div class="addon-description">${this.escapeHtml(addOn.addOn.description)}</div>
            ` : ''}
            ${this.config.showImage && addOn.addOn.imageUrl ? `
              <div class="addon-image">
                <img src="${this.escapeHtml(addOn.addOn.imageUrl)}" alt="${this.escapeHtml(addOn.addOn.title)}" loading="lazy">
              </div>
            ` : ''}
          </div>
        </label>
      </div>
    `;

    return widget;
  }

  /**
   * Apply styles to the widget
   */
  applyStyles() {
    if (document.getElementById('shoplazza-addon-styles')) {
      return; // Styles already applied
    }

    const style = document.createElement('style');
    style.id = 'shoplazza-addon-styles';
    style.textContent = this.generateCSS();
    document.head.appendChild(style);

    this.log('Styles applied');
  }

  /**
   * Generate CSS for the widget
   */
  generateCSS() {
    return `
      .shoplazza-addon-container {
        margin: 15px 0;
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      }

      .shoplazza-addon-widget {
        margin-bottom: 10px;
      }

      .addon-container {
        border: 1px solid ${this.config.borderColor};
        border-radius: 6px;
        padding: 12px;
        transition: all 0.2s ease;
        background: #fff;
        cursor: pointer;
      }

      .addon-container:hover {
        border-color: ${this.config.primaryColor};
        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      }

      .addon-container.selected {
        border-color: ${this.config.primaryColor};
        background-color: ${this.config.selectedBackgroundColor};
      }

      .addon-label {
        display: flex;
        align-items: flex-start;
        cursor: pointer;
        margin: 0;
      }

      .addon-checkbox {
        margin: 0;
        margin-right: 10px;
        margin-top: 2px;
      }

      .addon-content {
        flex: 1;
      }

      .addon-main {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 4px;
      }

      .addon-title {
        font-weight: 500;
        color: ${this.config.textColor};
        font-size: 14px;
      }

      .addon-price {
        font-weight: 600;
        color: ${this.config.primaryColor};
        font-size: 14px;
      }

      .addon-description {
        color: #666;
        font-size: 12px;
        line-height: 1.4;
        margin-top: 4px;
      }

      .addon-image {
        margin-top: 8px;
      }

      .addon-image img {
        max-width: 60px;
        max-height: 40px;
        border-radius: 4px;
      }

      /* Theme variations */
      .shoplazza-addon-container[data-theme="minimal"] .addon-container {
        border: none;
        border-bottom: 1px solid ${this.config.borderColor};
        border-radius: 0;
        padding: 8px 0;
      }

      .shoplazza-addon-container[data-theme="rounded"] .addon-container {
        border-radius: 12px;
      }

      /* Animation classes */
      .addon-price-highlight {
        animation: priceHighlight 0.6s ease;
      }

      @keyframes priceHighlight {
        0%, 100% { background-color: transparent; }
        50% { background-color: ${this.config.primaryColor}20; }
      }

      /* Custom CSS */
      ${this.config.customCss}
    `;
  }

  /**
   * Set up event listeners
   */
  setupEventListeners() {
    // Add-on toggle events
    this.elements.container.addEventListener('change', (event) => {
      if (event.target.classList.contains('addon-checkbox')) {
        this.handleAddOnToggle(event);
      }
    });

    // Cart form interception
    this.interceptCartForms();

    this.log('Event listeners set up');
  }

  /**
   * Handle add-on toggle
   */
  async handleAddOnToggle(event) {
    const checkbox = event.target;
    const widget = checkbox.closest('.shoplazza-addon-widget');
    const addOnId = parseInt(widget.dataset.addOnId, 10);
    const isChecked = checkbox.checked;

    try {
      const container = checkbox.closest('.addon-container');
      container.classList.toggle('selected', isChecked);

      if (isChecked) {
        this.state.selectedAddOns.add(addOnId);
      } else {
        this.state.selectedAddOns.delete(addOnId);
      }

      // Trigger price animation if enabled
      if (this.config.animation === 'highlight') {
        this.animatePriceChange();
      }

      // Call toggle callback
      if (this.config.onAddOnToggle) {
        this.config.onAddOnToggle(addOnId, isChecked, this);
      }

      this.log(`Add-on ${addOnId} ${isChecked ? 'selected' : 'deselected'}`);
    } catch (error) {
      this.handleError('Failed to toggle add-on', error);
    }
  }

  /**
   * Animate price change
   */
  animatePriceChange() {
    const priceElements = document.querySelectorAll('.addon-price');
    priceElements.forEach((element) => {
      element.classList.remove('addon-price-highlight');
      // Force reflow
      element.offsetHeight; // eslint-disable-line no-unused-expressions
      element.classList.add('addon-price-highlight');
    });
  }

  /**
   * Intercept cart forms to add selected add-ons
   */
  interceptCartForms() {
    const forms = document.querySelectorAll('form[action*="/cart/add"]');
    
    forms.forEach((form) => {
      form.addEventListener('submit', (event) => {
        this.handleCartFormSubmit(event, form);
      });
    });
  }

  /**
   * Handle cart form submission
   */
  async handleCartFormSubmit(event, form) {
    if (this.state.selectedAddOns.size === 0) {
      return; // No add-ons selected, proceed normally
    }

    event.preventDefault();

    try {
      // Get main product data
      const formData = new FormData(form);
      const mainProductId = formData.get('id');
      const quantity = parseInt(formData.get('quantity') || '1', 10);

      // Add main product to cart
      await this.addToCart(mainProductId, quantity);

      // Add selected add-ons
      for (const addOnId of this.state.selectedAddOns) {
        const addOn = this.state.addOns.find(a => a.id === addOnId);
        if (addOn && addOn.addOn.addOnVariantId) {
          await this.addToCart(addOn.addOn.addOnVariantId, quantity, {
            '_addon': 'true',
            '_addon_for': mainProductId,
            '_addon_title': addOn.addOn.title,
          });
        }
      }

      // Call cart update callback
      if (this.config.onCartUpdate) {
        this.config.onCartUpdate(this.state.selectedAddOns, this);
      }

      // Redirect to cart or show success message
      if (form.action && form.action.includes('/cart/add')) {
        window.location.href = '/cart';
      }
    } catch (error) {
      this.handleError('Failed to add items to cart', error);
    }
  }

  /**
   * Add item to cart using Shoplazza's cart API
   */
  async addToCart(variantId, quantity, properties = {}) {
    const response = await fetch('/cart/add.js', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        id: variantId,
        quantity,
        properties,
      }),
    });

    if (!response.ok) {
      throw new Error(`Failed to add item to cart: ${response.status}`);
    }

    return response.json();
  }

  /**
   * Load current cart state
   */
  async loadCartState() {
    try {
      const response = await fetch('/cart.js');
      if (response.ok) {
        this.state.cartItems = await response.json();
      }
    } catch (error) {
      this.log('Failed to load cart state', 'warn');
    }
  }

  /**
   * Handle errors
   */
  handleError(message, error) {
    this.state.error = error;
    this.log(`${message}: ${error.message}`, 'error');

    if (this.config.onError) {
      this.config.onError(message, error, this);
    }
  }

  /**
   * Logging utility
   */
  log(message, level = 'info') {
    if (!this.config.debug) return;

    const prefix = '[Shoplazza Add-On Widget]';
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
   * Escape HTML to prevent XSS
   */
  escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Destroy the widget
   */
  destroy() {
    if (this.elements.container) {
      this.elements.container.remove();
    }

    const styles = document.getElementById('shoplazza-addon-styles');
    if (styles) {
      styles.remove();
    }

    this.state.initialized = false;
    this.log('Widget destroyed');
  }

  /**
   * Get current state
   */
  getState() {
    return { ...this.state };
  }

  /**
   * Update configuration
   */
  updateConfig(newConfig) {
    this.config = { ...this.config, ...newConfig };
    
    if (this.state.initialized) {
      // Re-apply styles if styling config changed
      const styleKeys = ['primaryColor', 'textColor', 'borderColor', 'selectedBackgroundColor', 'customCss'];
      if (styleKeys.some(key => newConfig.hasOwnProperty(key))) {
        const existingStyles = document.getElementById('shoplazza-addon-styles');
        if (existingStyles) {
          existingStyles.textContent = this.generateCSS();
        }
      }
    }
  }
}

export default WidgetCore;
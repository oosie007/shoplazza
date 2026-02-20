/**
 * Theme manager for widget styling and appearance
 * Handles different themes and customization options
 */
class ThemeManager {
  constructor(config = {}) {
    this.config = {
      theme: config.theme || 'default',
      primaryColor: config.primaryColor || '#007bff',
      textColor: config.textColor || '#333333',
      borderColor: config.borderColor || '#e1e8ed',
      selectedBackgroundColor: config.selectedBackgroundColor || '#e7f3ff',
      customCss: config.customCss || '',
      debug: config.debug || false,
      ...config,
    };

    this.themes = this.initializeThemes();
    this.styleElement = null;
  }

  /**
   * Initialize predefined themes
   */
  initializeThemes() {
    return {
      default: {
        name: 'Default',
        styles: {
          container: {
            margin: '15px 0',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          },
          widget: {
            marginBottom: '10px',
          },
          addonContainer: {
            border: `1px solid ${this.config.borderColor}`,
            borderRadius: '6px',
            padding: '12px',
            transition: 'all 0.2s ease',
            background: '#fff',
            cursor: 'pointer',
          },
          addonContainerHover: {
            borderColor: this.config.primaryColor,
            boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
          },
          addonContainerSelected: {
            borderColor: this.config.primaryColor,
            backgroundColor: this.config.selectedBackgroundColor,
          },
        },
      },
      minimal: {
        name: 'Minimal',
        styles: {
          container: {
            margin: '10px 0',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          },
          widget: {
            marginBottom: '8px',
          },
          addonContainer: {
            border: 'none',
            borderBottom: `1px solid ${this.config.borderColor}`,
            borderRadius: '0',
            padding: '8px 0',
            transition: 'all 0.2s ease',
            background: 'transparent',
            cursor: 'pointer',
          },
          addonContainerHover: {
            backgroundColor: '#f8f9fa',
          },
          addonContainerSelected: {
            backgroundColor: this.config.selectedBackgroundColor,
            borderBottomColor: this.config.primaryColor,
          },
        },
      },
      rounded: {
        name: 'Rounded',
        styles: {
          container: {
            margin: '15px 0',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          },
          widget: {
            marginBottom: '12px',
          },
          addonContainer: {
            border: `2px solid ${this.config.borderColor}`,
            borderRadius: '16px',
            padding: '16px',
            transition: 'all 0.3s ease',
            background: '#fff',
            cursor: 'pointer',
          },
          addonContainerHover: {
            borderColor: this.config.primaryColor,
            transform: 'translateY(-1px)',
            boxShadow: '0 4px 8px rgba(0,0,0,0.12)',
          },
          addonContainerSelected: {
            borderColor: this.config.primaryColor,
            backgroundColor: this.config.selectedBackgroundColor,
            transform: 'translateY(-1px)',
          },
        },
      },
      compact: {
        name: 'Compact',
        styles: {
          container: {
            margin: '8px 0',
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          },
          widget: {
            marginBottom: '6px',
          },
          addonContainer: {
            border: `1px solid ${this.config.borderColor}`,
            borderRadius: '4px',
            padding: '8px',
            transition: 'all 0.15s ease',
            background: '#fff',
            cursor: 'pointer',
          },
          addonContainerHover: {
            borderColor: this.config.primaryColor,
          },
          addonContainerSelected: {
            borderColor: this.config.primaryColor,
            backgroundColor: this.config.selectedBackgroundColor,
          },
        },
      },
    };
  }

  /**
   * Apply theme styles to the page
   */
  applyTheme() {
    if (this.styleElement) {
      this.styleElement.remove();
    }

    this.styleElement = document.createElement('style');
    this.styleElement.id = 'shoplazza-addon-theme-styles';
    this.styleElement.textContent = this.generateCSS();
    document.head.appendChild(this.styleElement);

    this.log(`Applied theme: ${this.config.theme}`);
  }

  /**
   * Generate CSS for the current theme
   */
  generateCSS() {
    const theme = this.themes[this.config.theme] || this.themes.default;
    
    return `
      /* Shoplazza Add-On Widget - ${theme.name} Theme */
      .shoplazza-addon-container {
        margin: ${theme.styles.container.margin};
        font-family: ${theme.styles.container.fontFamily};
      }

      .shoplazza-addon-widget {
        margin-bottom: ${theme.styles.widget.marginBottom};
      }

      .addon-container {
        ${this.stylesToCSS(theme.styles.addonContainer)}
      }

      .addon-container:hover {
        ${this.stylesToCSS(theme.styles.addonContainerHover)}
      }

      .addon-container.selected {
        ${this.stylesToCSS(theme.styles.addonContainerSelected)}
      }

      .addon-label {
        display: flex;
        align-items: flex-start;
        cursor: pointer;
        margin: 0;
        width: 100%;
      }

      .addon-checkbox {
        margin: 0;
        margin-right: 10px;
        margin-top: 2px;
        accent-color: ${this.config.primaryColor};
      }

      .addon-checkmark {
        /* Custom checkbox styling can be added here */
      }

      .addon-content {
        flex: 1;
        min-width: 0;
      }

      .addon-main {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 4px;
        gap: 8px;
      }

      .addon-title {
        font-weight: 500;
        color: ${this.config.textColor};
        font-size: ${this.getFontSize('title')};
        line-height: 1.3;
        flex: 1;
      }

      .addon-price {
        font-weight: 600;
        color: ${this.config.primaryColor};
        font-size: ${this.getFontSize('price')};
        white-space: nowrap;
      }

      .addon-description {
        color: #666;
        font-size: ${this.getFontSize('description')};
        line-height: 1.4;
        margin-top: 4px;
      }

      .addon-image {
        margin-top: 8px;
      }

      .addon-image img {
        max-width: ${this.getImageSize()};
        max-height: 40px;
        border-radius: ${this.getImageBorderRadius()};
        object-fit: cover;
      }

      /* Loading state */
      .addon-container.loading {
        opacity: 0.6;
        pointer-events: none;
      }

      .addon-container.loading::after {
        content: '';
        position: absolute;
        top: 50%;
        right: 12px;
        width: 16px;
        height: 16px;
        border: 2px solid ${this.config.primaryColor};
        border-top: 2px solid transparent;
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }

      /* Error state */
      .addon-container.error {
        border-color: #dc3545;
        background-color: #fff5f5;
      }

      /* Disabled state */
      .addon-container.disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .addon-container.disabled .addon-checkbox {
        pointer-events: none;
      }

      /* Animations */
      .addon-price-highlight {
        animation: priceHighlight 0.6s ease;
      }

      @keyframes priceHighlight {
        0%, 100% { background-color: transparent; }
        50% { background-color: ${this.config.primaryColor}20; }
      }

      @keyframes spin {
        0% { transform: translate(-50%, -50%) rotate(0deg); }
        100% { transform: translate(-50%, -50%) rotate(360deg); }
      }

      /* Responsive design */
      @media (max-width: 480px) {
        .shoplazza-addon-container {
          margin: 10px 0;
        }

        .addon-container {
          padding: 10px;
        }

        .addon-main {
          flex-direction: column;
          align-items: flex-start;
          gap: 4px;
        }

        .addon-title {
          font-size: 13px;
        }

        .addon-price {
          font-size: 13px;
        }

        .addon-description {
          font-size: 11px;
        }
      }

      /* Accessibility */
      .addon-container:focus-within {
        outline: 2px solid ${this.config.primaryColor};
        outline-offset: 2px;
      }

      .addon-checkbox:focus {
        outline: none;
      }

      /* High contrast mode support */
      @media (prefers-contrast: high) {
        .addon-container {
          border-width: 2px;
        }

        .addon-title {
          font-weight: 600;
        }

        .addon-price {
          font-weight: 700;
        }
      }

      /* Reduced motion support */
      @media (prefers-reduced-motion: reduce) {
        .addon-container,
        .addon-container:hover,
        .addon-container.selected {
          transition: none;
        }

        .addon-price-highlight {
          animation: none;
        }
      }

      /* Dark mode support */
      @media (prefers-color-scheme: dark) {
        .addon-container {
          background: #1a1a1a;
          border-color: #404040;
          color: #ffffff;
        }

        .addon-title {
          color: #ffffff;
        }

        .addon-description {
          color: #cccccc;
        }

        .addon-container.selected {
          background-color: ${this.config.primaryColor}20;
        }
      }

      /* Custom CSS */
      ${this.config.customCss}
    `;
  }

  /**
   * Convert style object to CSS string
   */
  stylesToCSS(styles) {
    return Object.entries(styles)
      .map(([property, value]) => `${this.camelToKebab(property)}: ${value};`)
      .join('\n        ');
  }

  /**
   * Convert camelCase to kebab-case
   */
  camelToKebab(str) {
    return str.replace(/([a-z0-9]|(?=[A-Z]))([A-Z])/g, '$1-$2').toLowerCase();
  }

  /**
   * Get font size based on theme
   */
  getFontSize(element) {
    const sizes = {
      compact: {
        title: '13px',
        price: '13px',
        description: '11px',
      },
      default: {
        title: '14px',
        price: '14px',
        description: '12px',
      },
    };

    const themeKey = this.config.theme === 'compact' ? 'compact' : 'default';
    return sizes[themeKey][element] || sizes.default[element];
  }

  /**
   * Get image size based on theme
   */
  getImageSize() {
    return this.config.theme === 'compact' ? '50px' : '60px';
  }

  /**
   * Get image border radius based on theme
   */
  getImageBorderRadius() {
    const radiusMap = {
      rounded: '8px',
      minimal: '2px',
      compact: '3px',
      default: '4px',
    };

    return radiusMap[this.config.theme] || radiusMap.default;
  }

  /**
   * Update theme configuration
   */
  updateConfig(newConfig) {
    this.config = { ...this.config, ...newConfig };
    
    // Reinitialize themes with new colors
    this.themes = this.initializeThemes();
    
    // Reapply styles
    this.applyTheme();
    
    this.log('Theme configuration updated');
  }

  /**
   * Switch to a different theme
   */
  switchTheme(themeName) {
    if (this.themes[themeName]) {
      this.config.theme = themeName;
      this.applyTheme();
      this.log(`Switched to theme: ${themeName}`);
    } else {
      this.log(`Theme not found: ${themeName}`, 'warn');
    }
  }

  /**
   * Get available themes
   */
  getAvailableThemes() {
    return Object.keys(this.themes).map(key => ({
      key,
      name: this.themes[key].name,
    }));
  }

  /**
   * Add custom theme
   */
  addCustomTheme(name, styles) {
    this.themes[name] = {
      name: name.charAt(0).toUpperCase() + name.slice(1),
      styles,
    };
    
    this.log(`Added custom theme: ${name}`);
  }

  /**
   * Remove theme styles
   */
  removeStyles() {
    if (this.styleElement) {
      this.styleElement.remove();
      this.styleElement = null;
      this.log('Theme styles removed');
    }
  }

  /**
   * Get current theme configuration
   */
  getCurrentTheme() {
    return {
      name: this.config.theme,
      config: { ...this.config },
      theme: this.themes[this.config.theme],
    };
  }

  /**
   * Generate theme preview CSS
   */
  generatePreviewCSS(themeName, customConfig = {}) {
    const originalConfig = { ...this.config };
    this.config = { ...this.config, ...customConfig, theme: themeName };
    
    const css = this.generateCSS();
    
    // Restore original config
    this.config = originalConfig;
    
    return css;
  }

  /**
   * Logging utility
   */
  log(message, level = 'info') {
    if (!this.config.debug) return;

    const prefix = '[Theme Manager]';
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
}

export default ThemeManager;
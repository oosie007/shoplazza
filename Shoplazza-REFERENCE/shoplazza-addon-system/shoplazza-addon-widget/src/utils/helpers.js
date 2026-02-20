/**
 * Utility functions for the Shoplazza Add-On Widget
 */

/**
 * Debounce function to limit function calls
 */
export function debounce(func, wait, immediate = false) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      timeout = null;
      if (!immediate) func.apply(this, args);
    };
    const callNow = immediate && !timeout;
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
    if (callNow) func.apply(this, args);
  };
}

/**
 * Throttle function to limit function calls
 */
export function throttle(func, limit) {
  let inThrottle;
  return function executedFunction(...args) {
    if (!inThrottle) {
      func.apply(this, args);
      inThrottle = true;
      setTimeout(() => { inThrottle = false; }, limit);
    }
  };
}

/**
 * Deep merge objects
 */
export function deepMerge(target, source) {
  const output = { ...target };
  
  if (isObject(target) && isObject(source)) {
    Object.keys(source).forEach((key) => {
      if (isObject(source[key])) {
        if (!(key in target)) {
          Object.assign(output, { [key]: source[key] });
        } else {
          output[key] = deepMerge(target[key], source[key]);
        }
      } else {
        Object.assign(output, { [key]: source[key] });
      }
    });
  }
  
  return output;
}

/**
 * Check if value is an object
 */
export function isObject(item) {
  return item && typeof item === 'object' && !Array.isArray(item);
}

/**
 * Escape HTML to prevent XSS
 */
export function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

/**
 * Format price for display
 */
export function formatPrice(cents, currency = 'USD', locale = 'en-US') {
  const amount = cents / 100;
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
  }).format(amount);
}

/**
 * Parse price string to cents
 */
export function parsePriceToCents(priceString) {
  const cleaned = priceString.replace(/[^0-9.-]+/g, '');
  const amount = parseFloat(cleaned);
  return Math.round(amount * 100);
}

/**
 * Validate email address
 */
export function isValidEmail(email) {
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return re.test(email);
}

/**
 * Generate unique ID
 */
export function generateId(prefix = 'id') {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

/**
 * Get viewport dimensions
 */
export function getViewport() {
  return {
    width: Math.max(document.documentElement.clientWidth || 0, window.innerWidth || 0),
    height: Math.max(document.documentElement.clientHeight || 0, window.innerHeight || 0),
  };
}

/**
 * Check if element is in viewport
 */
export function isInViewport(element) {
  const rect = element.getBoundingClientRect();
  const viewport = getViewport();
  
  return (
    rect.top >= 0 &&
    rect.left >= 0 &&
    rect.bottom <= viewport.height &&
    rect.right <= viewport.width
  );
}

/**
 * Smooth scroll to element
 */
export function scrollToElement(element, offset = 0) {
  const elementPosition = element.getBoundingClientRect().top + window.pageYOffset;
  const targetPosition = elementPosition - offset;
  
  window.scrollTo({
    top: targetPosition,
    behavior: 'smooth',
  });
}

/**
 * Wait for element to appear in DOM
 */
export function waitForElement(selector, timeout = 5000) {
  return new Promise((resolve, reject) => {
    const element = document.querySelector(selector);
    if (element) {
      resolve(element);
      return;
    }

    const observer = new MutationObserver((mutations, obs) => {
      const foundElement = document.querySelector(selector);
      if (foundElement) {
        obs.disconnect();
        resolve(foundElement);
      }
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });

    setTimeout(() => {
      observer.disconnect();
      reject(new Error(`Element ${selector} not found within ${timeout}ms`));
    }, timeout);
  });
}

/**
 * Create DOM element with attributes
 */
export function createElement(tag, attributes = {}, children = []) {
  const element = document.createElement(tag);
  
  Object.entries(attributes).forEach(([key, value]) => {
    if (key === 'className') {
      element.className = value;
    } else if (key === 'textContent') {
      element.textContent = value;
    } else if (key === 'innerHTML') {
      element.innerHTML = value;
    } else if (key.startsWith('data-')) {
      element.setAttribute(key, value);
    } else {
      element[key] = value;
    }
  });
  
  children.forEach((child) => {
    if (typeof child === 'string') {
      element.appendChild(document.createTextNode(child));
    } else if (child instanceof Element) {
      element.appendChild(child);
    }
  });
  
  return element;
}

/**
 * Add event listener with cleanup
 */
export function addEventListenerWithCleanup(element, event, handler, options = {}) {
  element.addEventListener(event, handler, options);
  
  return () => {
    element.removeEventListener(event, handler, options);
  };
}

/**
 * Local storage wrapper with error handling
 */
export const storage = {
  get(key, defaultValue = null) {
    try {
      const item = localStorage.getItem(key);
      return item ? JSON.parse(item) : defaultValue;
    } catch (error) {
      console.warn('Failed to get from localStorage:', error);
      return defaultValue;
    }
  },

  set(key, value) {
    try {
      localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch (error) {
      console.warn('Failed to set localStorage:', error);
      return false;
    }
  },

  remove(key) {
    try {
      localStorage.removeItem(key);
      return true;
    } catch (error) {
      console.warn('Failed to remove from localStorage:', error);
      return false;
    }
  },

  clear() {
    try {
      localStorage.clear();
      return true;
    } catch (error) {
      console.warn('Failed to clear localStorage:', error);
      return false;
    }
  },
};

/**
 * Cookie utilities
 */
export const cookies = {
  get(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
      return parts.pop().split(';').shift();
    }
    return null;
  },

  set(name, value, days = 7) {
    const expires = new Date();
    expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
    document.cookie = `${name}=${value};expires=${expires.toUTCString()};path=/`;
  },

  remove(name) {
    document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;`;
  },
};

/**
 * URL utilities
 */
export const url = {
  getParams() {
    return new URLSearchParams(window.location.search);
  },

  getParam(name) {
    return this.getParams().get(name);
  },

  setParam(name, value) {
    const params = this.getParams();
    params.set(name, value);
    const newUrl = `${window.location.pathname}?${params.toString()}`;
    window.history.replaceState(null, '', newUrl);
  },

  removeParam(name) {
    const params = this.getParams();
    params.delete(name);
    const newUrl = `${window.location.pathname}?${params.toString()}`;
    window.history.replaceState(null, '', newUrl);
  },
};

/**
 * Feature detection utilities
 */
export const features = {
  get touchDevice() {
    return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  },

  get localStorage() {
    try {
      localStorage.setItem('test', 'test');
      localStorage.removeItem('test');
      return true;
    } catch (error) {
      return false;
    }
  },

  get fetch() {
    return typeof fetch !== 'undefined';
  },

  get intersection() {
    return 'IntersectionObserver' in window;
  },

  get mutation() {
    return 'MutationObserver' in window;
  },
};

/**
 * Performance utilities
 */
export const perf = {
  mark(name) {
    if (performance && performance.mark) {
      performance.mark(name);
    }
  },

  measure(name, startMark, endMark) {
    if (performance && performance.measure) {
      performance.measure(name, startMark, endMark);
      return performance.getEntriesByName(name)[0];
    }
    return null;
  },

  now() {
    return performance && performance.now ? performance.now() : Date.now();
  },
};

/**
 * CSS utilities
 */
export const css = {
  hasClass(element, className) {
    return element.classList.contains(className);
  },

  addClass(element, className) {
    element.classList.add(className);
  },

  removeClass(element, className) {
    element.classList.remove(className);
  },

  toggleClass(element, className) {
    element.classList.toggle(className);
  },

  getStyle(element, property) {
    return window.getComputedStyle(element)[property];
  },

  setStyle(element, styles) {
    Object.entries(styles).forEach(([property, value]) => {
      element.style[property] = value;
    });
  },
};

/**
 * Animation utilities
 */
export const animate = {
  fadeIn(element, duration = 300) {
    element.style.opacity = '0';
    element.style.display = 'block';
    
    const start = perf.now();
    
    const fade = (timestamp) => {
      const elapsed = timestamp - start;
      const progress = Math.min(elapsed / duration, 1);
      
      element.style.opacity = progress.toString();
      
      if (progress < 1) {
        requestAnimationFrame(fade);
      }
    };
    
    requestAnimationFrame(fade);
  },

  fadeOut(element, duration = 300) {
    const start = perf.now();
    const initialOpacity = parseFloat(element.style.opacity) || 1;
    
    const fade = (timestamp) => {
      const elapsed = timestamp - start;
      const progress = Math.min(elapsed / duration, 1);
      
      element.style.opacity = (initialOpacity * (1 - progress)).toString();
      
      if (progress < 1) {
        requestAnimationFrame(fade);
      } else {
        element.style.display = 'none';
      }
    };
    
    requestAnimationFrame(fade);
  },

  slideDown(element, duration = 300) {
    element.style.height = '0px';
    element.style.overflow = 'hidden';
    element.style.display = 'block';
    
    const targetHeight = element.scrollHeight;
    const start = perf.now();
    
    const slide = (timestamp) => {
      const elapsed = timestamp - start;
      const progress = Math.min(elapsed / duration, 1);
      
      element.style.height = `${targetHeight * progress}px`;
      
      if (progress < 1) {
        requestAnimationFrame(slide);
      } else {
        element.style.height = '';
        element.style.overflow = '';
      }
    };
    
    requestAnimationFrame(slide);
  },

  slideUp(element, duration = 300) {
    const initialHeight = element.offsetHeight;
    const start = perf.now();
    
    element.style.overflow = 'hidden';
    
    const slide = (timestamp) => {
      const elapsed = timestamp - start;
      const progress = Math.min(elapsed / duration, 1);
      
      element.style.height = `${initialHeight * (1 - progress)}px`;
      
      if (progress < 1) {
        requestAnimationFrame(slide);
      } else {
        element.style.display = 'none';
        element.style.height = '';
        element.style.overflow = '';
      }
    };
    
    requestAnimationFrame(slide);
  },
};

export default {
  debounce,
  throttle,
  deepMerge,
  isObject,
  escapeHtml,
  formatPrice,
  parsePriceToCents,
  isValidEmail,
  generateId,
  getViewport,
  isInViewport,
  scrollToElement,
  waitForElement,
  createElement,
  addEventListenerWithCleanup,
  storage,
  cookies,
  url,
  features,
  perf,
  css,
  animate,
};
// Lightweight checkout widget script for Shoplazza.
// Load this script on every checkout step (Contact, Shipping, Payment) so the widget appears automatically.
// Requires: window.CD_INSURE_APP_URL (your app URL) and window.SHOPLAZZA_SHOP_DOMAIN (or we derive from location.hostname).
// Debug on mobile: add ?cd_debug=1 to checkout URL, or on same store run localStorage.setItem('cd_insure_debug','1') then open checkout on phone.

(function () {
  const shopDomain = window.SHOPLAZZA_SHOP_DOMAIN || (typeof location !== "undefined" && location.hostname ? location.hostname : "");
  const hasCheckoutAPI = typeof CheckoutAPI !== "undefined";
  const APP_BASE_URL = window.CD_INSURE_APP_URL || "";

  var debugEnabled = false;
  var debugEl = null;
  try {
    debugEnabled = (typeof location !== "undefined" && (location.search.indexOf("cd_debug=1") !== -1 || location.hash === "#cd_debug")) ||
      (typeof localStorage !== "undefined" && localStorage.getItem("cd_insure_debug") === "1");
  } catch (e) {}
  function debugLog(msg, isError) {
    if (!debugEnabled) return;
    if (typeof console !== "undefined") {
      if (isError && console.warn) console.warn("[CD Insure]", msg);
      else if (console.log) console.log("[CD Insure]", msg);
    }
    try {
      if (!debugEl && typeof document !== "undefined") {
        debugEl = document.createElement("div");
        debugEl.id = "cd-insure-debug";
        debugEl.style.cssText = "position:fixed;bottom:8px;right:8px;max-width:90%;max-height:200px;overflow:auto;background:rgba(0,0,0,0.88);color:#0f0;font:11px monospace;padding:8px;border-radius:6px;z-index:999999;white-space:pre-wrap;word-break:break-all;";
        document.body.appendChild(debugEl);
      }
      if (debugEl) {
        var line = document.createElement("div");
        line.style.color = isError ? "#f88" : "#afa";
        line.textContent = (typeof performance !== "undefined" && performance.now ? "t+" + Math.round(performance.now()) + "ms " : "") + msg;
        debugEl.appendChild(line);
        debugEl.scrollTop = debugEl.scrollHeight;
      }
    } catch (e) {}
  }

  if (!shopDomain) {
    debugLog("No shopDomain – exit", true);
    return;
  }
  debugLog("script loaded shop=" + shopDomain + " appUrl=" + (APP_BASE_URL ? "set" : "MISSING"));
  if (typeof console !== "undefined" && console.log) {
    console.log("[CD Insure] checkout-widget.js loaded, shop=" + shopDomain + ", appUrl=" + (APP_BASE_URL || "MISSING"));
  }

  let settings = null;
  let premiumAmount = 0;
  let toggleOn = false;
  var pkgSet404Warned = false;

  function fetchSettings() {
    if (!APP_BASE_URL) {
      debugLog("fetchSettings: no APP_BASE_URL", true);
      return Promise.resolve(null);
    }
    debugLog("fetchSettings start " + APP_BASE_URL + "/api/public-settings");
    return fetch(
      APP_BASE_URL +
        "/api/public-settings?shop=" +
        encodeURIComponent(shopDomain)
    )
      .then((r) => {
        if (r.ok) {
          debugLog("fetchSettings ok " + r.status);
          return r.json();
        }
        debugLog("fetchSettings fail " + r.status + " " + r.statusText, true);
        return null;
      })
      .catch(function (err) {
        debugLog("fetchSettings network err " + (err && err.message ? err.message : String(err)), true);
        return null;
      });
  }

  /** Optional map productId -> categoryId from backend when checkout doesn't provide category per product. */
  function computePremium(prices, products, productIdToCategory) {
    if (!settings) return 0;
    const mode = settings.pricingMode || "fixed_percent_all";
    const defaultPercent = settings.fixedPercentAll || 0;
    const categoryPercents = settings.categoryPercents || {};
    const excludedCategoryIds = settings.excludedCategoryIds || [];
    const lookup = productIdToCategory || {};

    if (mode === "fixed_percent_all") {
      let total = parseFloat(
        prices.totalPrice || prices.subtotalPrice ||
        prices.total_price || prices.subtotal_price ||
        prices.originalTotalPrice || prices.original_total_price ||
        "0"
      );
      if (total <= 0 && products && products.length) {
        for (const p of products) {
          const linePrice = parseFloat(p.finalLinePrice || p.linePrice || p.price || "0") || 0;
          const qty = parseInt(p.quantity || "1", 10) || 1;
          total += linePrice * qty;
        }
      }
      const premium = +(total * (defaultPercent / 100)).toFixed(2);
      if (total > 0 && premium === 0 && defaultPercent > 0) {
        return 0.01;
      }
      return premium;
    }

    // per_category: use % per product category; fallback to defaultPercent if category unknown or not set
    let total = 0;
    const categoryPercentKeys = Object.keys(categoryPercents);
    const lookupKeys = Object.keys(lookup);
    for (const p of products || []) {
      const linePrice = parseFloat(p.finalLinePrice || p.linePrice || "0");
      const pid = p.id != null ? String(p.id) : (p.productId != null ? String(p.productId) : null);
      // Resolve category: from product object first, then from API map (try pid and raw id/productId for key mismatch)
      let categoryId = p.categoryId != null ? String(p.categoryId) : (p.category_id != null ? String(p.category_id) : (p.collectionId != null ? String(p.collectionId) : (p.collection_id != null ? String(p.collection_id) : null)));
      if (!categoryId && pid) {
        categoryId = lookup[pid] || (p.id != null ? lookup[String(p.id)] : undefined) || (p.productId != null ? lookup[String(p.productId)] : undefined) || null;
      }
      let percent = defaultPercent;
      if (categoryId) {
        if (excludedCategoryIds.indexOf(categoryId) >= 0) percent = 0;
        else if (categoryPercents[categoryId] != null) percent = Number(categoryPercents[categoryId]);
      }
      if (typeof console !== "undefined" && console.log) {
        console.log("[CD Insure] computePremium line:", { pid, categoryId, mapKeys: lookupKeys, categoryPercentKeys, percent, linePrice, matched: categoryPercents[categoryId] != null });
      }
      total += linePrice * (percent / 100);
    }
    return +total.toFixed(2);
  }

  /** Fetch productId -> categoryId from app when pricing is per_category (checkout doesn't send category per line). */
  function fetchProductCategoryMap(products) {
    if (!APP_BASE_URL || !settings || settings.pricingMode !== "per_category" || !(products && products.length)) return Promise.resolve({});
    const ids = products.map(function (p) { return p.id != null ? p.id : p.productId; }).filter(Boolean);
    if (ids.length === 0) return Promise.resolve({});
    if (typeof console !== "undefined" && console.log) {
      console.log("[CD Insure] products from checkout:", products.map(function (p) { return { id: p.id, productId: p.productId, title: p.productTitle || p.title }; }));
      console.log("[CD Insure] productIds sent to API:", ids);
    }
    return fetch(APP_BASE_URL + "/api/product-categories?shop=" + encodeURIComponent(shopDomain) + "&productIds=" + ids.join(","))
      .then(function (r) {
        return r.ok ? r.json() : {};
      })
      .then(function (map) {
        if (typeof console !== "undefined" && console.log) {
          var keys = map && typeof map === "object" ? Object.keys(map) : [];
          console.log("[CD Insure] productId -> categoryId map from API:", map, "keys:", keys);
        }
        return map;
      })
      .catch(function (err) {
        if (typeof console !== "undefined" && console.warn) {
          console.warn("[CD Insure] fetchProductCategoryMap failed:", err);
        }
        return {};
      });
  }

  /** Get order/checkout token from URL (e.g. /checkout/2407954194541497895892?step=...) */
  function getOrderToken() {
    const m = typeof location !== "undefined" && location.pathname && location.pathname.match(/\/checkout\/([^/?]+)/);
    return m ? m[1] : null;
  }

  /** Base URL for store checkout API (we run on the store's checkout page, same origin). */
  function getStoreOrigin() {
    if (typeof location !== "undefined" && location.origin) return location.origin;
    return "https://" + shopDomain;
  }

  /**
   * Build the payload for POST /api/checkout/price (same as the working Worry-Free flow).
   * Uses CheckoutAPI when available; otherwise tries page globals (e.g. __CHECKOUT_STATE__).
   */
  function getPricePayload() {
    const orderToken = getOrderToken();
    if (!orderToken) return null;
    var stepMatch = typeof location !== "undefined" && location.search && location.search.match(/step=([^&]+)/);
    const step = hasCheckoutAPI && CheckoutAPI.step && typeof CheckoutAPI.step.getStep === "function"
      ? CheckoutAPI.step.getStep()
      : (stepMatch ? stepMatch[1] : "contact_information");
    const prices = hasCheckoutAPI && CheckoutAPI.store && CheckoutAPI.store.getPrices ? CheckoutAPI.store.getPrices() : null;
    const address = hasCheckoutAPI && CheckoutAPI.address && CheckoutAPI.address.getShippingAddress ? CheckoutAPI.address.getShippingAddress() : null;
    const totalTipReceived = (prices && prices.totalTipReceived) ? String(prices.totalTipReceived) : "0.00";

    var shippingAddress = {
      id: (address && address.id) || "",
      first_name: (address && address.firstName) || "",
      last_name: (address && address.lastName) || "",
      email: (address && address.email) || "",
      phone: (address && address.phone) || "",
      country_code: (address && address.countryCode) || "",
      country: (address && address.country) || "",
      area: (address && address.area) || "",
      address: (address && address.address) || (address && address.address1) || "",
      address1: (address && address.address1) || (address && address.address) || "",
      company: (address && address.company) || "",
      phone_area_code: (address && address.phoneAreaCode) || "00",
      latitude: (address && address.latitude) || "",
      longitude: (address && address.longitude) || "",
      source: (address && address.source) || "",
      tags: (address && address.tags) || "",
      email_or_phone: (address && address.emailOrPhone) || "",
      cpf: (address && address.cpf) || "",
      id_number: (address && address.idNumber) || "",
      gender: (address && address.gender) || "",
      province: (address && address.province) || "ALL",
      province_code: (address && address.provinceCode) || "ALL",
      city: (address && address.city) || "",
      zip: (address && address.zip) || "",
    };

    var payload = {
      order_token: orderToken,
      calculate_shipping_line: true,
      total_tip_received: totalTipReceived,
      step: step,
      config: { checkout_business_type: 0 },
      shipping_address: shippingAddress,
    };

    if (hasCheckoutAPI && CheckoutAPI.store && typeof CheckoutAPI.store.getShippingLine === "function") {
      try {
        var line = CheckoutAPI.store.getShippingLine();
        if (line) payload.shipping_line = line;
      } catch (e) {}
    }
    if (!payload.shipping_line && typeof window.__CHECKOUT_SHIPPING_LINE__ === "object" && window.__CHECKOUT_SHIPPING_LINE__ != null) {
      payload.shipping_line = window.__CHECKOUT_SHIPPING_LINE__;
    }
    if (!payload.shipping_line && typeof window.__CHECKOUT_STATE__ === "object" && window.__CHECKOUT_STATE__ != null) {
      var state = window.__CHECKOUT_STATE__;
      if (state.shipping_line) payload.shipping_line = state.shipping_line;
      if (state.shippingLine) payload.shipping_line = state.shippingLine;
    }
    return payload;
  }

  /**
   * 1) Try pkg_set (store may 404 if app has not registered a checkout package).
   * 2) POST /api/checkout/price with current state; when pkg_set is 404 we also try
   *    sending additional_prices in the price body so the total might still update.
   */
  function applyPremiumViaStoreCheckout(enabled) {
    const orderToken = getOrderToken();
    if (!orderToken) return;
    const origin = getStoreOrigin();
    var pricePayload = getPricePayload();
    if (!pricePayload) return;

    if (enabled) {
      pricePayload.additional_prices = [
        { name: "cd_insure_item_protection", price: premiumAmount.toFixed(2), fee_title: "Item protection" },
      ];
    } else {
      pricePayload.additional_prices = [];
    }
    pricePayload.addtional_prices = pricePayload.additional_prices;

    function doPriceRequest() {
      return fetch(origin + "/api/checkout/price", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(pricePayload),
        credentials: "same-origin",
      }).then(function (res) {
        if (res && res.ok && hasCheckoutAPI && CheckoutAPI.store && typeof CheckoutAPI.store.onPricesChange === "function") {
          res.clone().json().then(function (pricesFromServer) {
            try {
              if (pricesFromServer) CheckoutAPI.store.onPricesChange(pricesFromServer);
            } catch (e) {}
          }).catch(function () {
            try {
              var prices = CheckoutAPI.store.getPrices && CheckoutAPI.store.getPrices();
              if (prices) CheckoutAPI.store.onPricesChange(prices);
            } catch (e2) {}
          });
        }
        return res;
      });
    }

    var pkgPayload = { order_token: orderToken, checked: enabled ? 1 : 0 };
    if (settings && settings.checkoutPkgKey) pkgPayload.package_key = settings.checkoutPkgKey;
    fetch(origin + "/api/checkout/pkg_set", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(pkgPayload),
      credentials: "same-origin",
    })
      .then(function (res) {
        if (res && res.status === 404 && !pkgSet404Warned && typeof console !== "undefined" && console.warn) {
          pkgSet404Warned = true;
          console.warn("[CD Insure] pkg_set returned 404 – expected unless a checkout package is registered. Using Cart API + price refetch instead.");
        }
        return doPriceRequest();
      })
      .catch(function () { return doPriceRequest(); });
  }

  /** Show a short message + "Refresh page" link so the customer can see the updated total. No automatic reload. */
  function showRefreshHint(kind) {
    try {
      var root = typeof document !== "undefined" && document.querySelector("#cd-insure-widget-root");
      if (!root) return;
      var existing = root.querySelector(".ip-refresh-hint");
      if (existing) existing.remove();
      var wrap = root.querySelector(".ip-wrap");
      if (!wrap) return;
      var msg = kind === "added"
        ? "Item protection added. Refresh the page to see the updated total."
        : "Item protection removed. Refresh the page to see the updated total.";
      var div = document.createElement("div");
      div.className = "ip-refresh-hint";
      div.style.cssText = "margin-top:8px;font-family:'Lato',sans-serif;font-size:12px;line-height:16px;color:#6F7175;";
      div.innerHTML = msg.replace("Refresh the page", "<a href=\"#\" style=\"color:#007AB3;text-decoration:underline;\">Refresh the page</a>");
      var link = div.querySelector("a");
      if (link) {
        link.addEventListener("click", function (e) { e.preventDefault(); if (typeof location !== "undefined" && location.reload) location.reload(); });
      }
      wrap.appendChild(div);
    } catch (e) {}
  }

  /**
   * Build a CheckoutPrices-shaped object from price API response so the checkout UI can re-render (like Worry Free).
   */
  function toCheckoutPrices(prices, items) {
    var p = prices || {};
    var str = function (v) { return v != null ? String(v) : "0"; };
    return {
      subtotalPrice: str(p.subtotal_price || p.subtotalPrice),
      totalPrice: str(p.total_price || p.total || p.totalPrice),
      shippingPrice: str(p.shipping_price || p.shippingPrice),
      taxPrice: str(p.tax_price || p.taxPrice),
      discountCodePrice: str(p.discount_code_price || p.discountCodePrice),
      discountPrice: str(p.discount_price || p.discountPrice),
      discountLineItemPrice: str(p.discount_line_item_price || p.discountLineItemPrice),
      paymentDue: str(p.payment_due != null ? p.payment_due : (p.total_price || p.total || p.totalPrice)),
      paidTotal: str(p.paid_total || p.paidTotal),
      giftCardPrice: str(p.gift_card_price || p.giftCardPrice),
      totalTipReceived: str(p.total_tip_received || p.totalTipReceived),
      discountShippingPrice: str(p.discount_shipping_price || p.discountShippingPrice),
      additionalPrices: Array.isArray(p.additionalPrices) ? p.additionalPrices : (Array.isArray(p.additional_prices) ? p.additional_prices : []),
      line_items: Array.isArray(items) ? items : [],
    };
  }

  /**
   * After we get new prices (like after Worry Free's pkg_set + price), try every plausible way the
   * checkout might refresh the order summary so only the cart updates, not the full page.
   */
  function triggerCheckoutRefresh(checkoutPrices) {
    try {
      if (typeof window === "undefined" || !window.dispatchEvent) return;
      var detail = { prices: checkoutPrices };
      var events = [
        "shoplazza:cart:updated",
        "shoplazza:price:updated",
        "checkout:price:updated",
        "priceUpdated",
        "pricesChange",
      ];
      for (var i = 0; i < events.length; i++) {
        try {
          window.dispatchEvent(new CustomEvent(events[i], { detail: detail }));
        } catch (e) {}
      }
      if (hasCheckoutAPI && CheckoutAPI.store) {
        var store = CheckoutAPI.store;
        var fns = ["refresh", "refreshPrices", "updatePrices", "setPrices", "reloadSummary", "refreshCart"];
        for (var j = 0; j < fns.length; j++) {
          if (typeof store[fns[j]] === "function") {
            try {
              store[fns[j]](checkoutPrices);
            } catch (e) {}
          }
        }
      }
      var globals = ["__checkoutUpdatePrices", "__CHECKOUT_REFRESH", "ShoplazzaCheckoutRefresh", "checkoutPriceUpdated"];
      for (var k = 0; k < globals.length; k++) {
        var g = typeof window !== "undefined" && window[globals[k]];
        if (typeof g === "function") {
          try {
            g(checkoutPrices);
          } catch (e) {}
        }
      }
    } catch (e) {}
  }

  /**
   * After Cart API add/remove, refetch price and notify checkout so the order summary can refresh (cart-only, like Worry Free).
   */
  function refreshCheckoutPriceAfterCartChange() {
    var orderToken = getOrderToken();
    if (!orderToken) return;
    var payload = getPricePayload();
    if (!payload) return;
    var origin = getStoreOrigin();
    setTimeout(function () {
      fetch(origin + "/api/checkout/price", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        credentials: "same-origin",
      }).then(function (res) {
        if (typeof console !== "undefined" && console.log) {
          console.log("[CD Insure] Price refetch status " + (res ? res.status : "none"));
        }
        if (res && res.ok && hasCheckoutAPI && CheckoutAPI.store) {
          res.json().then(function (pricesFromServer) {
            try {
              if (!pricesFromServer) return;
              var data = pricesFromServer.data || pricesFromServer;
              var prices = data.prices || data;
              var items = data.line_items || data.lineItems || [];
              var total = prices.total_price != null ? prices.total_price : prices.total;
              var count = Array.isArray(items) ? items.length : 0;
              if (typeof console !== "undefined" && console.log) {
                console.log("[CD Insure] Price response total=" + total + " line_items=" + count + ", notifying checkout");
              }
              var checkoutPrices = toCheckoutPrices(prices, items);
              if (typeof CheckoutAPI.store.onPricesChange === "function") {
                CheckoutAPI.store.onPricesChange(checkoutPrices);
              }
              // Trigger the same kind of refresh Worry Free gets after pkg_set + price (no console, just cart update).
              triggerCheckoutRefresh(checkoutPrices);
            } catch (e) {
              if (typeof console !== "undefined" && console.warn) console.warn("[CD Insure] onPricesChange error", e);
            }
          }).catch(function (err) {
            if (typeof console !== "undefined" && console.warn) console.warn("[CD Insure] Price response parse error", err);
          });
        } else if (res && !res.ok && typeof console !== "undefined" && console.warn) {
          res.text().then(function (t) { console.warn("[CD Insure] Price refetch " + res.status, t.slice(0, 200)); }).catch(function () {});
        }
      }).catch(function (err) {
        if (typeof console !== "undefined" && console.warn) console.warn("[CD Insure] Price refetch failed", err && err.message ? err.message : err);
      });
    }, 400);
  }

  /**
   * Add or remove Item Protection as a cart line item using Shoplazza Cart API.
   * When itemProtectionProductId and itemProtectionVariantId are set in settings,
   * toggle ON adds the product to cart; toggle OFF removes it. Cart totals update automatically.
   * @see https://www.shoplazza.dev/docs/cart-api-reference
   */
  function applyPremiumViaCartAPI(enabled) {
    const productId = settings && settings.itemProtectionProductId;
    const variantId = settings && settings.itemProtectionVariantId;
    if (!productId || !variantId) {
      if (enabled && typeof console !== "undefined" && console.warn) {
        console.warn("[CD Insure] Cart API skipped – no Item Protection product/variant ID in app settings. Reinstall the app so we can create the product and save IDs, or add them in the app admin under Cart totals integration.");
      }
      return;
    }
    const base = (typeof window !== "undefined" && window.SHOPLAZZA && window.SHOPLAZZA.routes && window.SHOPLAZZA.routes.root)
      ? window.SHOPLAZZA.routes.root
      : getStoreOrigin();
    const cartUrl = base + "/api/cart";

    if (enabled) {
      if (typeof console !== "undefined" && console.log) {
        console.log("[CD Insure] Cart API: POST " + cartUrl + " with product_id=" + productId + " variant_id=" + variantId);
      }
      fetch(cartUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Accept": "application/json",
        },
        body: JSON.stringify({
          product_id: productId,
          variant_id: variantId,
          quantity: 1,
          refer_info: { source: "add_to_cart" },
        }),
        credentials: "same-origin",
      })
        .then(function (res) {
          if (typeof console !== "undefined" && console.log) {
            console.log("[CD Insure] Cart API response: status " + (res ? res.status : "none"));
          }
          if (res && res.ok) {
            debugLog("Cart API: added Item Protection line");
            if (typeof console !== "undefined" && console.log) {
              console.log("[CD Insure] Item Protection line added (200). Refetching price so Cart Transform total appears.");
            }
            refreshCheckoutPriceAfterCartChange();
            setTimeout(function () { refreshCheckoutPriceAfterCartChange(); }, 1200);
            showRefreshHint("added");
          } else {
            debugLog("Cart API add failed " + (res ? res.status : "no res"), true);
            if (res && typeof console !== "undefined" && console.warn) {
              res.text().then(function (body) {
                console.warn("[CD Insure] Cart POST " + res.status + " response:", body.slice(0, 300));
              }).catch(function () {});
            }
          }
        })
        .catch(function (err) {
          if (typeof console !== "undefined" && console.warn) {
            console.warn("[CD Insure] Cart API fetch error:", err && err.message ? err.message : err);
          }
          debugLog("Cart API add err " + (err && err.message ? err.message : String(err)), true);
        });
      return;
    }

    // Remove: get cart, find our product line, then DELETE
    fetch(cartUrl, {
      method: "GET",
      headers: { "Content-Type": "application/json", "Accept": "application/json" },
      credentials: "same-origin",
    })
      .then(function (res) { return res.ok ? res.json() : null; })
      .then(function (data) {
        var cart = data && data.cart;
        var items = cart && cart.line_items;
        if (!Array.isArray(items)) return;
        var line = items.find(function (item) {
          var pid = item.product_id != null ? String(item.product_id) : (item.productId != null ? String(item.productId) : "");
          return pid === String(productId);
        });
        if (!line) {
          debugLog("Cart API: no Item Protection line to remove");
          return;
        }
        var vid = line.variant_id != null ? line.variant_id : line.variantId;
        var id = line.id;
        if (!vid) return;
        return fetch(cartUrl + "/" + encodeURIComponent(vid), {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ id: id, product_id: productId, variant_id: vid }),
          credentials: "same-origin",
        });
      })
      .then(function (res) {
        if (res && res.ok) {
          debugLog("Cart API: removed Item Protection line");
          refreshCheckoutPriceAfterCartChange();
          showRefreshHint("removed");
        }
      })
      .catch(function (err) {
        debugLog("Cart API remove err " + (err && err.message ? err.message : String(err)), true);
      });
  }

  /** Notify our backend for logging / future server-side fee API. */
  function applyPremiumViaBackend(enabled) {
    if (!APP_BASE_URL || !shopDomain) return;
    const orderToken = getOrderToken();
    if (!orderToken) return;
    fetch(APP_BASE_URL + "/api/checkout/apply-fee", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        shop: shopDomain,
        order_token: orderToken,
        amount: premiumAmount.toFixed(2),
        label: "Item protection",
        enabled: !!enabled,
      }),
    }).catch(function () {});
  }

  const FEE_LABEL = "Item protection";

  function applyPremium(enabled) {
    var payload = enabled
      ? [{ label: FEE_LABEL, amount: premiumAmount.toFixed(2) }]
      : [];
    if (hasCheckoutAPI && CheckoutAPI.store && typeof CheckoutAPI.store.setAdditionalPrices === "function") {
      CheckoutAPI.store.setAdditionalPrices(payload);
    }
    if (hasCheckoutAPI && CheckoutAPI.store && typeof CheckoutAPI.store.updateAdditionalPrices === "function") {
      CheckoutAPI.store.updateAdditionalPrices(payload);
    }
    if (typeof window.PaymentEC === "object" && window.PaymentEC != null && typeof window.PaymentEC.setAdditionalPrices === "function") {
      window.PaymentEC.setAdditionalPrices(payload);
    }
    // Cart API: add/remove Item Protection as a line item when product/variant IDs are configured
    applyPremiumViaCartAPI(enabled);
    applyPremiumViaStoreCheckout(enabled);
    applyPremiumViaBackend(enabled);
  }

  /** "Powered by Chubb" combined SVG from Figma (180×22). */
  function getPoweredByChubbSvg() {
    return '<svg class="ip-powered-by-chubb" viewBox="0 0 180 22" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M2.322 12.784V16H1.164V7.402H3.702C4.246 7.402 4.718 7.466 5.118 7.594C5.522 7.718 5.856 7.896 6.12 8.128C6.384 8.36 6.58 8.64 6.708 8.968C6.84 9.296 6.906 9.662 6.906 10.066C6.906 10.466 6.836 10.832 6.696 11.164C6.556 11.496 6.35 11.782 6.078 12.022C5.81 12.262 5.476 12.45 5.076 12.586C4.676 12.718 4.218 12.784 3.702 12.784H2.322ZM2.322 11.86H3.702C4.034 11.86 4.326 11.816 4.578 11.728C4.834 11.64 5.048 11.518 5.22 11.362C5.392 11.202 5.522 11.012 5.61 10.792C5.698 10.572 5.742 10.33 5.742 10.066C5.742 9.518 5.572 9.09 5.232 8.782C4.896 8.474 4.386 8.32 3.702 8.32H2.322V11.86ZM10.4962 9.826C10.9402 9.826 11.3402 9.9 11.6962 10.048C12.0562 10.196 12.3602 10.406 12.6082 10.678C12.8602 10.95 13.0522 11.28 13.1842 11.668C13.3202 12.052 13.3882 12.482 13.3882 12.958C13.3882 13.438 13.3202 13.87 13.1842 14.254C13.0522 14.638 12.8602 14.966 12.6082 15.238C12.3602 15.51 12.0562 15.72 11.6962 15.868C11.3402 16.012 10.9402 16.084 10.4962 16.084C10.0522 16.084 9.65016 16.012 9.29016 15.868C8.93416 15.72 8.63016 15.51 8.37816 15.238C8.12616 14.966 7.93216 14.638 7.79616 14.254C7.66016 13.87 7.59216 13.438 7.59216 12.958C7.59216 12.482 7.66016 12.052 7.79616 11.668C7.93216 11.28 8.12616 10.95 8.37816 10.678C8.63016 10.406 8.93416 10.196 9.29016 10.048C9.65016 9.9 10.0522 9.826 10.4962 9.826ZM10.4962 15.25C11.0962 15.25 11.5442 15.05 11.8402 14.65C12.1362 14.246 12.2842 13.684 12.2842 12.964C12.2842 12.24 12.1362 11.676 11.8402 11.272C11.5442 10.868 11.0962 10.666 10.4962 10.666C10.1922 10.666 9.92816 10.718 9.70416 10.822C9.48016 10.926 9.29216 11.076 9.14016 11.272C8.99216 11.468 8.88016 11.71 8.80416 11.998C8.73216 12.282 8.69616 12.604 8.69616 12.964C8.69616 13.324 8.73216 13.646 8.80416 13.93C8.88016 14.214 8.99216 14.454 9.14016 14.65C9.29216 14.842 9.48016 14.99 9.70416 15.094C9.92816 15.198 10.1922 15.25 10.4962 15.25ZM13.9121 9.922H14.7521C14.8401 9.922 14.9121 9.944 14.9681 9.988C15.0241 10.032 15.0621 10.084 15.0821 10.144L16.2461 14.056C16.2781 14.2 16.3081 14.34 16.3361 14.476C16.3641 14.608 16.3881 14.742 16.4081 14.878C16.4401 14.742 16.4761 14.608 16.5161 14.476C16.5561 14.34 16.5981 14.2 16.6421 14.056L17.9261 10.12C17.9461 10.06 17.9801 10.01 18.0281 9.97C18.0801 9.93 18.1441 9.91 18.2201 9.91H18.6821C18.7621 9.91 18.8281 9.93 18.8801 9.97C18.9321 10.01 18.9681 10.06 18.9881 10.12L20.2421 14.056C20.2861 14.196 20.3241 14.334 20.3561 14.47C20.3921 14.606 20.4261 14.74 20.4581 14.872C20.4781 14.74 20.5041 14.602 20.5361 14.458C20.5681 14.314 20.6021 14.18 20.6381 14.056L21.8261 10.144C21.8461 10.08 21.8841 10.028 21.9401 9.988C21.9961 9.944 22.0621 9.922 22.1381 9.922H22.9421L20.9741 16H20.1281C20.0241 16 19.9521 15.932 19.9121 15.796L18.5681 11.674C18.5361 11.582 18.5101 11.49 18.4901 11.398C18.4701 11.302 18.4501 11.208 18.4301 11.116C18.4101 11.208 18.3901 11.302 18.3701 11.398C18.3501 11.494 18.3241 11.588 18.2921 11.68L16.9301 15.796C16.8861 15.932 16.8041 16 16.6841 16H15.8801L13.9121 9.922ZM26.2976 9.826C26.6616 9.826 26.9976 9.888 27.3056 10.012C27.6136 10.132 27.8796 10.308 28.1036 10.54C28.3276 10.768 28.5016 11.052 28.6256 11.392C28.7536 11.728 28.8176 12.112 28.8176 12.544C28.8176 12.712 28.7996 12.824 28.7636 12.88C28.7276 12.936 28.6596 12.964 28.5596 12.964H24.5156C24.5236 13.348 24.5756 13.682 24.6716 13.966C24.7676 14.25 24.8996 14.488 25.0676 14.68C25.2356 14.868 25.4356 15.01 25.6676 15.106C25.8996 15.198 26.1596 15.244 26.4476 15.244C26.7156 15.244 26.9456 15.214 27.1376 15.154C27.3336 15.09 27.5016 15.022 27.6416 14.95C27.7816 14.878 27.8976 14.812 27.9896 14.752C28.0856 14.688 28.1676 14.656 28.2356 14.656C28.3236 14.656 28.3916 14.69 28.4396 14.758L28.7396 15.148C28.6076 15.308 28.4496 15.448 28.2656 15.568C28.0816 15.684 27.8836 15.78 27.6716 15.856C27.4636 15.932 27.2476 15.988 27.0236 16.024C26.7996 16.064 26.5776 16.084 26.3576 16.084C25.9376 16.084 25.5496 16.014 25.1936 15.874C24.8416 15.73 24.5356 15.522 24.2756 15.25C24.0196 14.974 23.8196 14.634 23.6756 14.23C23.5316 13.826 23.4596 13.362 23.4596 12.838C23.4596 12.414 23.5236 12.018 23.6516 11.65C23.7836 11.282 23.9716 10.964 24.2156 10.696C24.4596 10.424 24.7576 10.212 25.1096 10.06C25.4616 9.904 25.8576 9.826 26.2976 9.826ZM26.3216 10.612C25.8056 10.612 25.3996 10.762 25.1036 11.062C24.8076 11.358 24.6236 11.77 24.5516 12.298H27.8576C27.8576 12.05 27.8236 11.824 27.7556 11.62C27.6876 11.412 27.5876 11.234 27.4556 11.086C27.3236 10.934 27.1616 10.818 26.9696 10.738C26.7816 10.654 26.5656 10.612 26.3216 10.612ZM30.1846 16V9.922H30.7966C30.9126 9.922 30.9926 9.944 31.0366 9.988C31.0806 10.032 31.1106 10.108 31.1266 10.216L31.1986 11.164C31.4066 10.74 31.6626 10.41 31.9666 10.174C32.2746 9.934 32.6346 9.814 33.0466 9.814C33.2146 9.814 33.3666 9.834 33.5026 9.874C33.6386 9.91 33.7646 9.962 33.8806 10.03L33.7426 10.828C33.7146 10.928 33.6526 10.978 33.5566 10.978C33.5006 10.978 33.4146 10.96 33.2986 10.924C33.1826 10.884 33.0206 10.864 32.8126 10.864C32.4406 10.864 32.1286 10.972 31.8766 11.188C31.6286 11.404 31.4206 11.718 31.2526 12.13V16H30.1846ZM37.4304 9.826C37.7944 9.826 38.1304 9.888 38.4384 10.012C38.7464 10.132 39.0124 10.308 39.2364 10.54C39.4604 10.768 39.6344 11.052 39.7584 11.392C39.8864 11.728 39.9504 12.112 39.9504 12.544C39.9504 12.712 39.9324 12.824 39.8964 12.88C39.8604 12.936 39.7924 12.964 39.6924 12.964H35.6484C35.6564 13.348 35.7084 13.682 35.8044 13.966C35.9004 14.25 36.0324 14.488 36.2004 14.68C36.3684 14.868 36.5684 15.01 36.8004 15.106C37.0324 15.198 37.2924 15.244 37.5804 15.244C37.8484 15.244 38.0784 15.214 38.2704 15.154C38.4664 15.09 38.6344 15.022 38.7744 14.95C38.9144 14.878 39.0304 14.812 39.1224 14.752C39.2184 14.688 39.3004 14.656 39.3684 14.656C39.4564 14.656 39.5244 14.69 39.5724 14.758L39.8724 15.148C39.7404 15.308 39.5824 15.448 39.3984 15.568C39.2144 15.684 39.0164 15.78 38.8044 15.856C38.5964 15.932 38.3804 15.988 38.1564 16.024C37.9324 16.064 37.7104 16.084 37.4904 16.084C37.0704 16.084 36.6824 16.014 36.3264 15.874C35.9744 15.73 35.6684 15.522 35.4084 15.25C35.1524 14.974 34.9524 14.634 34.8084 14.23C34.6644 13.826 34.5924 13.362 34.5924 12.838C34.5924 12.414 34.6564 12.018 34.7844 11.65C34.9164 11.282 35.1044 10.964 35.3484 10.696C35.5924 10.424 35.8904 10.212 36.2424 10.06C36.5944 9.904 36.9904 9.826 37.4304 9.826ZM37.4544 10.612C36.9384 10.612 36.5324 10.762 36.2364 11.062C35.9404 11.358 35.7564 11.77 35.6844 12.298H38.9904C38.9904 12.05 38.9564 11.824 38.8884 11.62C38.8204 11.412 38.7204 11.234 38.5884 11.086C38.4564 10.934 38.2944 10.818 38.1024 10.738C37.9144 10.654 37.6984 10.612 37.4544 10.612ZM45.5954 16C45.4434 16 45.3474 15.926 45.3074 15.778L45.2114 15.04C44.9514 15.356 44.6534 15.61 44.3174 15.802C43.9854 15.99 43.6034 16.084 43.1714 16.084C42.8234 16.084 42.5074 16.018 42.2234 15.886C41.9394 15.75 41.6974 15.552 41.4974 15.292C41.2974 15.032 41.1434 14.708 41.0354 14.32C40.9274 13.932 40.8734 13.486 40.8734 12.982C40.8734 12.534 40.9334 12.118 41.0534 11.734C41.1734 11.346 41.3454 11.01 41.5694 10.726C41.7974 10.442 42.0734 10.22 42.3974 10.06C42.7214 9.896 43.0894 9.814 43.5014 9.814C43.8734 9.814 44.1914 9.878 44.4554 10.006C44.7194 10.13 44.9554 10.306 45.1634 10.534V7.162H46.2314V16H45.5954ZM43.5254 15.22C43.8734 15.22 44.1774 15.14 44.4374 14.98C44.7014 14.82 44.9434 14.594 45.1634 14.302V11.362C44.9674 11.098 44.7514 10.914 44.5154 10.81C44.2834 10.702 44.0254 10.648 43.7414 10.648C43.1734 10.648 42.7374 10.85 42.4334 11.254C42.1294 11.658 41.9774 12.234 41.9774 12.982C41.9774 13.378 42.0114 13.718 42.0794 14.002C42.1474 14.282 42.2474 14.514 42.3794 14.698C42.5114 14.878 42.6734 15.01 42.8654 15.094C43.0574 15.178 43.2774 15.22 43.5254 15.22ZM50.3768 16V7.162H51.4508V10.798C51.7028 10.506 51.9908 10.272 52.3148 10.096C52.6428 9.916 53.0168 9.826 53.4368 9.826C53.7888 9.826 54.1068 9.892 54.3908 10.024C54.6748 10.156 54.9168 10.354 55.1168 10.618C55.3168 10.878 55.4708 11.202 55.5788 11.59C55.6868 11.974 55.7408 12.418 55.7408 12.922C55.7408 13.37 55.6808 13.788 55.5608 14.176C55.4408 14.56 55.2668 14.894 55.0388 15.178C54.8148 15.458 54.5388 15.68 54.2108 15.844C53.8868 16.004 53.5208 16.084 53.1128 16.084C52.7208 16.084 52.3868 16.008 52.1108 15.856C51.8388 15.704 51.6008 15.492 51.3968 15.22L51.3428 15.772C51.3108 15.924 51.2188 16 51.0668 16H50.3768ZM53.0888 10.678C52.7408 10.678 52.4348 10.758 52.1708 10.918C51.9108 11.078 51.6708 11.304 51.4508 11.596V14.536C51.6428 14.8 51.8548 14.986 52.0868 15.094C52.3228 15.202 52.5848 15.256 52.8728 15.256C53.4408 15.256 53.8768 15.054 54.1808 14.65C54.4848 14.246 54.6368 13.67 54.6368 12.922C54.6368 12.526 54.6008 12.186 54.5288 11.902C54.4608 11.618 54.3608 11.386 54.2288 11.206C54.0968 11.022 53.9348 10.888 53.7428 10.804C53.5508 10.72 53.3328 10.678 53.0888 10.678ZM58.6736 17.794C58.6376 17.874 58.5916 17.938 58.5356 17.986C58.4836 18.034 58.4016 18.058 58.2896 18.058H57.4976L58.6076 15.646L56.0996 9.922H57.0236C57.1156 9.922 57.1876 9.946 57.2396 9.994C57.2916 10.038 57.3296 10.088 57.3536 10.144L58.9796 13.972C59.0156 14.06 59.0456 14.148 59.0696 14.236C59.0976 14.324 59.1216 14.414 59.1416 14.506C59.1696 14.414 59.1976 14.324 59.2256 14.236C59.2536 14.148 59.2856 14.058 59.3216 13.966L60.8996 10.144C60.9236 10.08 60.9636 10.028 61.0196 9.988C61.0796 9.944 61.1436 9.922 61.2116 9.922H62.0636L58.6736 17.794Z" fill="#191919"/><path d="M89.4614 7.47104V5.5082H77.7134C75.9095 5.5082 75 6.46605 75 8.02059V13.9876C75 15.5421 75.9095 16.5 77.7134 16.5H89.4614V14.5372H77.0464V7.47104H89.4614ZM97.2984 9.99914V5.5082H95.2975V16.5H97.2984V11.9305H107.985V16.5H109.986V5.5082H107.985V9.99914H97.2984ZM129.42 5.5082V14.5372H118.884V5.5082H116.884V13.9876C116.884 15.5421 117.793 16.5 119.597 16.5H128.707C130.511 16.5 131.421 15.5421 131.421 13.9876V5.5082H129.42ZM150.626 16.5C152.597 16.5 153.506 15.6521 153.506 14.1918V12.9041C153.506 12.4958 153.37 12.3074 153.082 12.0561L151.809 10.9256L153.082 9.79499C153.37 9.54374 153.506 9.35529 153.506 8.94705V7.8165C153.506 6.35612 152.597 5.5082 150.626 5.5082H138.317V16.5H150.626ZM140.318 7.37681H151.46V10.0462H140.318V7.37681ZM151.46 14.6314H140.318V11.8834H151.46V14.6314ZM172.151 16.5C174.122 16.5 175.032 15.6521 175.032 14.1918V12.9041C175.032 12.4958 174.895 12.3074 174.607 12.0561L173.334 10.9256L174.607 9.79499C174.895 9.54374 175.032 9.35529 175.032 8.94705V7.8165C175.032 6.35612 174.122 5.5082 172.151 5.5082H159.843V16.5H172.151ZM161.844 7.37681H172.985V10.0462H161.844V7.37681ZM172.985 14.6314H161.844V11.8834H172.985V14.6314Z" fill="#191919"/><path d="M178.691 6.90769C178.752 6.90367 178.806 6.89229 178.853 6.87344C178.9 6.85465 178.937 6.8245 178.966 6.78278C178.994 6.74124 179.008 6.68411 179.008 6.61165C179.008 6.54992 178.997 6.50027 178.975 6.46257C178.953 6.42511 178.924 6.3949 178.886 6.37197C178.848 6.34921 178.806 6.33369 178.76 6.32564C178.713 6.31759 178.664 6.31363 178.612 6.31363H178.235V6.91375H178.507C178.569 6.91375 178.631 6.91172 178.691 6.90769ZM178.028 7.86432V6.13235H178.67C178.859 6.13235 178.997 6.1733 179.084 6.25521C179.171 6.33714 179.214 6.45599 179.214 6.61165C179.214 6.68686 179.203 6.75261 179.181 6.80904C179.159 6.86539 179.129 6.91311 179.09 6.95204C179.051 6.99095 179.006 7.02182 178.956 7.04463C178.905 7.06749 178.852 7.08424 178.794 7.09492L179.292 7.86432H179.051L178.581 7.09492H178.235V7.86432H178.028ZM178.065 5.81413C177.916 5.88128 177.788 5.9726 177.679 6.08806C177.57 6.20347 177.485 6.33912 177.424 6.49479C177.363 6.65056 177.333 6.81848 177.333 6.99835C177.333 7.17829 177.363 7.3461 177.424 7.50181C177.485 7.6576 177.57 7.79319 177.679 7.90866C177.788 8.02412 177.916 8.11538 178.065 8.18254C178.215 8.24969 178.377 8.28324 178.553 8.28324C178.727 8.28324 178.888 8.24969 179.038 8.18254C179.187 8.11538 179.316 8.02412 179.424 7.90866C179.533 7.79319 179.619 7.6576 179.681 7.50181C179.743 7.3461 179.774 7.17829 179.774 6.99835C179.774 6.81848 179.743 6.65056 179.681 6.49479C179.619 6.33912 179.533 6.20347 179.424 6.08806C179.316 5.9726 179.187 5.88128 179.038 5.81413C178.888 5.74703 178.727 5.71348 178.553 5.71348C178.377 5.71348 178.215 5.74703 178.065 5.81413ZM179.125 5.61481C179.301 5.69132 179.454 5.79738 179.584 5.93297C179.713 6.06863 179.815 6.22774 179.889 6.41024C179.963 6.59292 180 6.78884 180 6.99835C180 7.21049 179.963 7.40718 179.889 7.58834C179.815 7.76962 179.713 7.92739 179.584 8.0617C179.454 8.19601 179.301 8.30133 179.125 8.37782C178.949 8.45436 178.758 8.49257 178.553 8.49257C178.349 8.49257 178.158 8.45436 177.982 8.37782C177.806 8.30133 177.653 8.19601 177.523 8.0617C177.393 7.92739 177.292 7.76962 177.218 7.58834C177.144 7.40718 177.107 7.21049 177.107 6.99835C177.107 6.78884 177.144 6.59292 177.218 6.41024C177.292 6.22774 177.393 6.06863 177.523 5.93297C177.653 5.79738 177.806 5.69132 177.982 5.61481C178.158 5.53827 178.349 5.5 178.553 5.5C178.758 5.5 178.949 5.53827 179.125 5.61481Z" fill="#191919"/></svg>';
  }

  /** Shield with checkmark from Figma SVG (extracted paths, viewBox fits 15–38 x 19–45). */
  function getShieldCheckSvg() {
    return '<svg class="ip-shield-icon" viewBox="15 19 24 27" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M26.56 44.5H26.34C26 44.4 23.54 43.56 20.95 40.73L20.84 40.52L22.08 39.58L22.19 39.79C24.09 41.89 25.89 42.82 26.57 43.03C28.03 42.41 35.11 38.74 36.44 25.88C31.39 25.67 27.91 22.74 26.57 21.38C25.22 22.74 21.75 25.67 16.7 25.88C17.04 29.53 17.94 32.78 19.28 35.5C19.5 35.92 19.84 36.44 20.07 36.97C20.41 37.49 20.63 37.8 20.63 37.91L19.39 38.85C19.28 38.74 18.94 38.22 18.6 37.7C18.26 37.18 18.05 36.66 17.82 36.23C16.36 33.09 15.35 29.43 15.01 25.25C15.01 25.04 15.12 24.83 15.23 24.73C15.34 24.63 15.57 24.42 15.79 24.52H15.9C21.96 24.52 25.77 19.92 25.77 19.81C25.99 19.6 26.22 19.5 26.56 19.5C26.78 19.5 27.01 19.6 27.23 19.81C27.23 19.81 31.04 24.52 37.1 24.52H37.21C37.43 24.52 37.66 24.63 37.77 24.73C37.88 24.83 37.99 25.04 37.99 25.25C36.64 40.94 27.22 44.4 26.77 44.5H26.55H26.56Z" fill="#191919"/><path d="M25.2 36.17L21.58 32.66L22.71 31.56L25.2 33.97L31.53 27.83L32.54 28.93L25.2 36.17Z" fill="#191919"/></svg>';
  }

  function getCurrencySymbol() {
    try {
      if (hasCheckoutAPI && CheckoutAPI.store && CheckoutAPI.store.getOrderInfo) {
        var info = CheckoutAPI.store.getOrderInfo();
        if (info && info.currencySymbol) return info.currencySymbol;
      }
    } catch (e) {}
    return "$";
  }

  function renderWidget(container) {
    if (!settings) return;
    const state = toggleOn ? "on" : "off";
    const priceStr = premiumAmount > 0 ? premiumAmount.toFixed(2) : "—";
    const showLogo = settings.enablePoweredByChubb !== false;
    const currencySymbol = getCurrencySymbol();
    const hasPercent = (settings.fixedPercentAll || 0) > 0;
    const isDisabled = premiumAmount <= 0 && !hasPercent;

    container.innerHTML = `
      <style>
        @import url('https://fonts.googleapis.com/css2?family=Lato:wght@400;700&display=swap');
        #cd-insure-widget-root .ip-wrap{width:100%;max-width:100%;box-sizing:border-box;padding:0 14px;}
        @media (min-width:640px){#cd-insure-widget-root .ip-wrap{padding:0;}}
        #cd-insure-widget-root .ip-card{box-sizing:border-box;display:flex;flex-direction:column;align-items:stretch;padding:14px 14px 10px;gap:12px;width:100%;max-width:100%;background:#FFFFFF;border:1px solid #D9D9D9;border-radius:5px;font-family:'Lato',sans-serif;}
        @media (min-width:640px){#cd-insure-widget-root .ip-card{max-width:100%;}}
        #cd-insure-widget-root .ip-card-on{}
        #cd-insure-widget-root .ip-card-disabled{opacity:0.65;pointer-events:none;}
        #cd-insure-widget-root .ip-header{display:flex;flex-direction:row;justify-content:space-between;align-items:center;gap:12px;padding:0;}
        #cd-insure-widget-root .ip-header-left{display:flex;flex-direction:row;align-items:center;gap:10px;min-width:0;}
        #cd-insure-widget-root .ip-shield{display:flex;align-items:flex-start;padding:0;width:25px;height:25px;flex-shrink:0;}
        #cd-insure-widget-root .ip-shield .ip-shield-icon{width:25px;height:25px;}
        #cd-insure-widget-root .ip-header-text{display:flex;flex-direction:column;justify-content:center;align-items:flex-start;gap:0;}
        #cd-insure-widget-root .ip-safe-purchase{font-family:'Lato',sans-serif;font-weight:700;font-size:10px;line-height:18px;color:#808080;}
        #cd-insure-widget-root .ip-title-row{font-family:'Lato',sans-serif;font-weight:700;font-size:16px;line-height:18px;color:#222222;}
        #cd-insure-widget-root .ip-title-row .ip-price-inline{font-weight:400;}
        #cd-insure-widget-root .ip-list{margin:0;padding:0 0 0 20px;list-style-type:disc;list-style-position:outside;font-family:'Lato',sans-serif;font-weight:400;font-size:12px;line-height:16px;color:#6F7175;}
        #cd-insure-widget-root .ip-list li{margin-bottom:4px;display:list-item;}
        #cd-insure-widget-root .ip-not-available{margin:0;padding:0;list-style:none;font-family:'Lato',sans-serif;font-size:12px;line-height:16px;color:#6F7175;}
        #cd-insure-widget-root .ip-learn{font-family:'Lato',sans-serif;font-weight:700;font-size:12px;line-height:22px;}
        #cd-insure-widget-root .ip-learn a{color:#007AB3;text-decoration:underline;}
        #cd-insure-widget-root .ip-footer{box-sizing:border-box;display:flex;flex-direction:row;justify-content:space-between;align-items:center;gap:8px;padding:0;border-top:1px solid #E5E5E5;}
        #cd-insure-widget-root .ip-tcs{font-family:'Lato',sans-serif;font-weight:400;font-size:12px;line-height:14px;color:#808080;}
        #cd-insure-widget-root .ip-powered{display:flex;align-items:center;}
        #cd-insure-widget-root .ip-powered-by-chubb{height:22px;width:180px;}
        #cd-insure-widget-root .ip-toggle{width:48px;height:28px;border-radius:1000px;border:none;position:relative;cursor:pointer;background:#808080;padding:0;flex-shrink:0;}
        #cd-insure-widget-root .ip-toggle.on{background:#191919;}
        #cd-insure-widget-root .ip-toggle-thumb{position:absolute;top:4px;left:4px;width:20px;height:20px;border-radius:1000px;background:#FFFFFF;transition:transform .15s ease;}
        #cd-insure-widget-root .ip-toggle.on .ip-toggle-thumb{transform:translateX(24px);left:0;}
      </style>
      <div class="ip-wrap">
        <div class="ip-card ${state === "on" ? "ip-card-on" : ""} ${isDisabled ? "ip-card-disabled" : ""}">
          <div class="ip-header">
            <div class="ip-header-left">
              <div class="ip-shield">${getShieldCheckSvg()}</div>
              <div class="ip-header-text">
                <div class="ip-safe-purchase">Safe Purchase</div>
                <div class="ip-title-row">Item protection <span class="ip-price-inline">for ${currencySymbol}${priceStr}</span></div>
              </div>
            </div>
            ${!isDisabled ? `<button class="ip-toggle ${state === "on" ? "on" : ""}" type="button" aria-pressed="${state === "on"}"><span class="ip-toggle-thumb"></span></button>` : ""}
          </div>
          <div class="ip-body">
            ${!isDisabled
              ? `<ul class="ip-list">
              <li>Provides you compensation should the item delivered not be as described</li>
              <li>Covers your item against theft and accidental for 30 days after you receive it</li>
              <li>Claims are handled directly with Chubb.</li>
            </ul>`
              : `<p class="ip-not-available">Item protection is not available for this order.</p>`}
            <div class="ip-learn"><a href="#" target="_blank" rel="noopener">Learn more</a></div>
          </div>
          <div class="ip-footer">
            <span class="ip-tcs">*T&amp;Cs apply</span>
            ${showLogo ? `<span class="ip-powered">${getPoweredByChubbSvg()}</span>` : ""}
          </div>
        </div>
      </div>
    `;

    const btn = container.querySelector(".ip-toggle");
    if (btn && !isDisabled) {
      btn.addEventListener("click", () => {
        toggleOn = !toggleOn;
        applyPremium(toggleOn);
        // Update only toggle state in DOM to avoid re-injecting styles (prevents font flicker)
        btn.classList.toggle("on", toggleOn);
        btn.setAttribute("aria-pressed", toggleOn);
        container.querySelector(".ip-card").classList.toggle("ip-card-on", toggleOn);
      });
    }
    var learnLink = container.querySelector(".ip-learn a");
    if (learnLink && settings.learnMoreUrl) {
      learnLink.href = settings.learnMoreUrl;
    }
  }

  /** Get the best container to mount the widget. On contact/shipping we prefer left column (like competitor). */
  function getMountTarget() {
    const step = hasCheckoutAPI && CheckoutAPI.step && typeof CheckoutAPI.step.getStep === "function"
      ? CheckoutAPI.step.getStep()
      : null;
    const isContactOrShipping = step === "contact_information" || step === "shipping_method";

    // On Contact Information / Shipping: prefer left column (below form), same as "Worry-Free Delivery".
    if (isContactOrShipping) {
      const form = document.querySelector("form[action*='checkout']") || document.querySelector(".checkout-body form") || document.querySelector("form");
      if (form) {
        const parent = form.parentNode;
        if (parent) return parent;
      }
      const leftCol = document.querySelector("[class*='checkout-body']") || document.querySelector("[class*='checkout-form']") || document.querySelector("main");
      if (leftCol) return leftCol;
    }

    // Right column: order summary (all steps).
    const summaryCandidates = document.querySelectorAll(
      ".checkout-summary, [class*='checkout-summary']"
    );
    if (summaryCandidates.length) {
      const summary = summaryCandidates[summaryCandidates.length - 1];
      if (summary) return summary;
    }
    const orderSummary = document.querySelector(".order-summary-dropdown-content") ||
      document.querySelector("[id*='summary'], [class*='summary']");
    if (orderSummary) return orderSummary;
    return document.body;
  }

  function mount(retryCount) {
    try {
      retryCount = retryCount || 0;
      const maxRetries = 12;
      debugLog("mount() retry=" + retryCount + " CheckoutAPI=" + (hasCheckoutAPI ? "yes" : "no"));
      let root = document.querySelector("#cd-insure-widget-root");
      if (!root) {
        const target = getMountTarget();
        const step = hasCheckoutAPI && CheckoutAPI.step && typeof CheckoutAPI.step.getStep === "function" ? CheckoutAPI.step.getStep() : null;
        const preferLeft = step === "contact_information" || step === "shipping_method";
        if (preferLeft && (!target || target === document.body) && retryCount < maxRetries) {
          debugLog("mount: waiting for target (retry " + (retryCount + 1) + ")");
          setTimeout(function () { mount(retryCount + 1); }, 300);
          return;
        }
        root = document.createElement("div");
        root.id = "cd-insure-widget-root";
        root.style.marginTop = "16px";
        (target || document.body).appendChild(root);
        debugLog("mount: root created");
      }

      fetchSettings().then((cfg) => {
      settings = cfg;

      if (!settings) {
        debugLog("settings null – placeholder", true);
        if (typeof console !== "undefined" && console.warn) {
          console.warn("[CD Insure] Settings not loaded (check app URL and that store is installed). Showing placeholder.");
        }
        root.innerHTML = "<div style=\"padding:12px;border:1px solid #e4e4e7;border-radius:12px;background:#fafafa;font-size:13px;color:#6b7280;\">Item protection – loading settings… <br><small>If this stays, set CD_INSURE_APP_URL and ensure your app is running.</small></div>";
        return;
      }
      if (settings.offerAtCheckout === false) {
        debugLog("offerAtCheckout=false – hide");
        root.innerHTML = "";
        return;
      }

      // If app is active but product IDs are missing, backend may still be creating the product. Refetch once.
      if (settings.activated && !settings.itemProtectionProductId && !window.__cd_insure_settings_refetched) {
        window.__cd_insure_settings_refetched = true;
        setTimeout(function () {
          fetchSettings().then(function (cfg2) {
            if (cfg2 && cfg2.itemProtectionProductId && cfg2.itemProtectionVariantId) {
              settings = cfg2;
              debugLog("Refetched settings – product IDs now available");
              if (typeof console !== "undefined" && console.log) {
                console.log("[CD Insure] Refetched settings – Item Protection product IDs now available. Toggle will add/remove cart line.");
              }
              if (hasCheckoutAPI && CheckoutAPI.store && CheckoutAPI.store.getPrices) {
                var prices = CheckoutAPI.store.getPrices();
                var products = CheckoutAPI.summary && CheckoutAPI.summary.getProductList ? CheckoutAPI.summary.getProductList() : [];
                fetchProductCategoryMap(products).then(function (map) {
                  premiumAmount = computePremium(prices, products, map);
                  renderWidget(root);
                });
              } else {
                renderWidget(root);
              }
            }
          });
        }, 2000);
      }

      if (hasCheckoutAPI && CheckoutAPI.store && CheckoutAPI.store.getPrices) {
        const prices = CheckoutAPI.store.getPrices();
        const products =
          CheckoutAPI.summary && CheckoutAPI.summary.getProductList
            ? CheckoutAPI.summary.getProductList()
            : [];
        if (typeof console !== "undefined" && console.log && settings.pricingMode === "per_category") {
          console.log("[CD Insure] pricingMode=per_category, categoryPercents keys:", settings.categoryPercents ? Object.keys(settings.categoryPercents) : []);
        }
        fetchProductCategoryMap(products).then(function (map) {
          premiumAmount = computePremium(prices, products, map);
          debugLog("premium=" + premiumAmount + " rendering");
          if (typeof console !== "undefined" && console.log) {
            console.log("[CD Insure] premiumAmount=" + premiumAmount);
          }
          if (settings.defaultAtCheckout === true && !toggleOn) {
            toggleOn = true;
            applyPremium(true);
          }
          renderWidget(root);
        });
      } else {
        // No CheckoutAPI in this context (e.g. testing from console) – just use a fixed sample amount.
        premiumAmount = 4.69;
        debugLog("no CheckoutAPI – sample premium, rendering");
        if (settings.defaultAtCheckout === true && !toggleOn) {
          toggleOn = true;
          applyPremium(true);
        }
        renderWidget(root);
      }

      if (hasCheckoutAPI && CheckoutAPI.store.onPricesChange) {
        CheckoutAPI.store.onPricesChange((newPrices) => {
          const products =
            CheckoutAPI.summary && CheckoutAPI.summary.getProductList
              ? CheckoutAPI.summary.getProductList()
              : [];
          fetchProductCategoryMap(products).then(function (map) {
            premiumAmount = computePremium(newPrices, products, map);
            if (toggleOn) applyPremium(true);
            renderWidget(root);
          });
        });
      }
      // Re-mount when step changes so widget appears on contact_information and shipping_method too.
      if (hasCheckoutAPI && CheckoutAPI.step && typeof CheckoutAPI.step.onStepChange === "function") {
        CheckoutAPI.step.onStepChange(function () {
          if (document.querySelector("#cd-insure-widget-root")) return;
          mount();
        });
      }
    }).catch(function (err) {
      debugLog("mount fetchSettings catch " + (err && err.message ? err.message : String(err)), true);
      if (typeof console !== "undefined" && console.warn) {
        console.warn("[CD Insure] mount failed:", err);
      }
      if (typeof root !== "undefined" && root) {
        root.innerHTML = "<div style=\"padding:12px;border:1px solid #e4e4e7;border-radius:12px;background:#fafafa;font-size:13px;color:#6b7280;\">Item protection – error loading. Check console.</div>";
      }
    });
    } catch (err) {
      debugLog("mount threw " + (err && err.message ? err.message : String(err)), true);
      if (typeof console !== "undefined" && console.warn) {
        console.warn("[CD Insure] mount threw:", err);
      }
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", function () { mount(); });
  } else {
    try { mount(); } catch (e) {
      if (typeof console !== "undefined" && console.warn) console.warn("[CD Insure] initial mount threw:", e);
    }
  }
})();


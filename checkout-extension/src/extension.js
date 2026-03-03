/**
 * CD Insure Item Protection – Checkout UI Extension
 * Renders the widget based on the configured injection point (checkout or cart page).
 * Uses Shoplazza extend() to inject a container and load our app's checkout-widget.js.
 */
(function () {
  // APP_URL is replaced at build time (see build.js)
  var APP_URL = "__APP_URL__";
  if (!APP_URL || APP_URL.indexOf("__") === 0) {
    APP_URL = "https://your-app-url.com";
  }

  var widgetHtml =
    '<div id="cd-insure-widget-root" style="margin-top:16px"></div>' +
    "<script>window.CD_INSURE_APP_URL=\"" + APP_URL + "\";window.SHOPLAZZA_SHOP_DOMAIN=window.location.hostname;</script>" +
    "<script src=\"" + APP_URL + "/checkout-widget.js\"><\/script>";

  // Get the shop domain from the current page
  var shopDomain = window.location.hostname;

  // Fetch the store settings to determine where to inject the widget
  var settingsUrl = APP_URL + "/api/public-settings/" + encodeURIComponent(shopDomain);
  fetch(settingsUrl)
    .then(function (res) {
      return res.ok ? res.json() : null;
    })
    .then(function (settings) {
      if (!settings) {
        // Default to checkout if settings not found
        injectAtCheckout();
        return;
      }

      var injectionPoint = settings.widgetInjectionPoint || "checkout";
      if (injectionPoint === "cart") {
        injectAtCart();
      } else {
        injectAtCheckout();
      }
    })
    .catch(function () {
      // Default to checkout on error
      injectAtCheckout();
    });

  function injectAtCheckout() {
    if (typeof extend === "function") {
      extend({
        extensionPoint: "Checkout::ContactInformation::RenderAfter",
        component: widgetHtml,
      });
      extend({
        extensionPoint: "Checkout::SectionPayment::RenderBefore",
        component: widgetHtml,
      });
    }
  }

  function injectAtCart() {
    if (typeof extend === "function") {
      // Inject on the cart page (below the cart summary, before order actions)
      extend({
        extensionPoint: "Cart::Summary::RenderAfter",
        component: widgetHtml,
      });
    }
  }
})();

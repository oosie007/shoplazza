/**
 * CD Insure Item Protection – Checkout UI Extension
 * Renders the widget at Contact Information and Payment steps.
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
})();

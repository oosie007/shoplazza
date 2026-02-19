/**
 * ESM entry for environments that provide extend from 'shoplazza-extension-ui'.
 * Use this if the Shoplazza CLI template uses import.
 * Build replaces __APP_URL__ with your app URL.
 */
var APP_URL = "__APP_URL__";
if (!APP_URL || APP_URL.indexOf("__") === 0) {
  APP_URL = "https://your-app-url.com";
}

var widgetHtml =
  '<div id="cd-insure-widget-root" style="margin-top:16px"></div>' +
  "<script>window.CD_INSURE_APP_URL=\"" + APP_URL + "\";window.SHOPLAZZA_SHOP_DOMAIN=window.location.hostname;</script>" +
  "<script src=\"" + APP_URL + "/checkout-widget.js\"><\/script>";

export function runExtension(extend) {
  if (typeof extend !== "function") return;
  extend({
    extensionPoint: "Checkout::ContactInformation::RenderAfter",
    component: widgetHtml,
  });
  extend({
    extensionPoint: "Checkout::SectionPayment::RenderBefore",
    component: widgetHtml,
  });
}

// If used as default export (e.g. import ext from './extension.esm')
export default function (extend) {
  runExtension(extend);
}

import { extend } from 'shoplazza-extension-ui';

// App URL where checkout-widget.js is served.
const APP_URL = 'https://shoplazza-nu.vercel.app';

const widgetHtml =
  '<div id="cd-insure-widget-root" data-extension="itemprotect" style="margin-top:16px"></div>' +
  `<script>window.CD_INSURE_APP_URL="${APP_URL}";window.SHOPLAZZA_SHOP_DOMAIN=window.location.hostname;</script>` +
  `<script src="${APP_URL}/checkout-widget.js"><\/script>`;

// Right column: after order summary (Subtotal, Shipping, Total) = below the totals.
extend({ extensionPoint: 'Checkout::Summary::RenderAfter', component: widgetHtml });
// Also below totals, before payment section (Payment step).
extend({ extensionPoint: 'Checkout::SectionPayment::RenderBefore', component: widgetHtml });

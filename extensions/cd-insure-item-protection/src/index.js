import { extend } from 'shoplazza-extension-ui';

// App URL where checkout-widget.js is served. Must match .env.local NEXT_PUBLIC_APP_URL and be reachable (e.g. ngrok running).
// If the widget doesn't appear: update this URL, restart "npm run dev:extension", then hard-refresh checkout.
const APP_URL = 'https://cb35-102-204-245-19.ngrok-free.app';

const widgetHtml =
  '<div id="cd-insure-widget-root" style="margin-top:16px"></div>' +
  `<script>window.CD_INSURE_APP_URL="${APP_URL}";window.SHOPLAZZA_SHOP_DOMAIN=window.location.hostname;</script>` +
  `<script src="${APP_URL}/checkout-widget.js"><\/script>`;

// ContactInformation::RenderAfter = where the widget used to show (top of contact section)
extend({
  extensionPoint: 'Checkout::ContactInformation::RenderAfter',
  component: widgetHtml,
});

extend({
  extensionPoint: 'Checkout::SectionPayment::RenderBefore',
  component: widgetHtml,
});

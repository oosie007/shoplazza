import { extend } from 'shoplazza-extension-ui';

// App URL where checkout-widget.js is served. Set via npm run inject:extension-url (from .env.local NEXT_PUBLIC_APP_URL) or replace manually.
const APP_URL = 'https://shoplazza-nu.vercel.app';

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

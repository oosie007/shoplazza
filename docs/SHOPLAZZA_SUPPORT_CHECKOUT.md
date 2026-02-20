# Shoplazza support: checkout extension and package

**For merchants:** We do **not** ask you to create an "Item Protection" product. The fee is added via a **checkout package** (add-on) that we register with Shoplazza once. Once that's active, every store with our app gets the line and total update automatically.

There is **no visible option** in Partner Center or in Shoplazza’s public docs to:

1. **Link a checkout extension to your app** so it runs on live checkout (including incognito) when the app is installed.
2. **Register a checkout package** for your app so the store exposes `POST /api/checkout/pkg_set` and the order total updates when the customer toggles the widget.

If you’ve deployed your extension via the CLI and the widget only appears with a preview link (or in dev mode), you need Shoplazza to do one or both of the above. Use support to get the current, correct steps.

---

## Who to contact

- **Partner Center support:** [partners@shoplazza.com](mailto:partners@shoplazza.com)  
- Or use the support/help option inside [Partner Center](https://partners.shoplazza.com) (e.g. Help, Live chat, or Contact).

---

## Copy-paste request (link extension + checkout package)

You can send something like this (adjust names/IDs if needed):

**Subject:** Link checkout extension to app + register checkout package

**Body:**

We have a Shoplazza app and a checkout UI extension that we deploy via the CLI (`shoplazza checkout push` and `shoplazza checkout deploy`). The extension shows an “Item protection” widget on checkout.

We cannot find in Partner Center or in the help docs how to:

1. **Link the checkout extension to our app** so that when a merchant installs our app, the extension runs on their store’s checkout for all visitors (including incognito). Right now the widget only appears when we use the preview link from the CLI; it does not appear in a normal or incognito checkout. Our extension name/ID: **cd-insure-item-protection-v2** (we can provide the extension ID from the CLI if needed). Where in Partner Center do we attach this extension to our app, or what do we need to submit for you to link it?

2. **Register a checkout package** for our app so the store accepts our fee and updates the order total. The store currently returns 404 for `POST /api/checkout/pkg_set` with the message “Register a checkout package in Partner Center.” What exact steps do we need to follow in Partner Center (or with you) to register this checkout package?

Our app is installed on the test store **oostest.myshoplaza.com**. We have already deployed the extension via the CLI.

Thank you.

---

After they reply, you can update this doc or your internal runbook with the actual steps they give you.

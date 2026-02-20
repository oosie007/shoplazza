# Cart Transform: Partner API vs Store API (from Shoplazza-REFERENCE)

The **Shoplazza-REFERENCE** folder (excluded from git) shows how cart transform was implemented in one day. The critical difference: **Create Function** uses the **Partner API**, not the store’s OpenAPI.

## Two APIs

| Step | API | Base URL | Auth | Endpoint |
|------|-----|----------|------|----------|
| **Create function** | **Partner API** | `https://partners.shoplazza.com/openapi/2024-07` | **Partner token** (client_credentials) + header `app-client-id` | `POST /functions` |
| **Bind to shop** | **Store API** | `https://{shop}.myshoplaza.com/openapi/2024-07` | **Store access token** (OAuth) | `POST /function/cart-transform` |

We were only calling the **store** API for both create and bind. The store does not expose “create function”; that lives on the **Partner API**. Hence 404 when POSTing to `https://{shop}/openapi/2024-07/function`.

## Partner token

- **URL:** `POST https://partners.shoplazza.com/partner/oauth/token`
- **Body (JSON):** `{ "client_id": "<ClientId>", "client_secret": "<ClientSecret>", "grant_type": "client_credentials" }`
- **Response:** `{ "access_token": "..." }`

Use the same app **Client ID** and **Client Secret** from Partner Center (the ones used for store OAuth). No shop parameter.

## Create function (Partner API)

- **URL:** `POST https://partners.shoplazza.com/openapi/2024-07/functions`
- **Headers:** `Access-Token: <partner_token>`, `app-client-id: <ClientId>`
- **Body:** **multipart/form-data**
  - `namespace` = `cart_transform`
  - `name` = e.g. `item-protection-cart-transform`
  - `file` = WASM binary (the reference compiles JS to WASM with Javy and uploads this)
  - `source_code` = JavaScript source string (reference sends both)

The reference builds a **WASM** from JavaScript (Javy) and sends it as `file`. If the API accepts `source_code` only, we can skip WASM for a first version.

## Bind (Store API) – unchanged

- **URL:** `POST https://{shop}/openapi/2024-07/function/cart-transform`
- **Headers:** `Access-Token: <store_access_token>`
- **Body (JSON):** `{ "function_id": "<id from Create>" }`

## Flow in the reference

1. **Startup or on demand:** Get partner token → GET Partner API `/functions` to find existing by name → if exists, PATCH to update; else POST to create (multipart with WASM + source_code). Store function_id (e.g. in DB as “global function”).
2. **Per merchant install:** Use store access token → POST store API `.../function/cart-transform` with `function_id` to bind that global function to the shop.

## What we changed in our app

- We now obtain a **partner token** (client_credentials) and call **Partner API** `POST /functions` with multipart (`namespace`, `name`, `source_code`; optionally `file` when we have WASM).
- We then **bind** with the store token to `https://{shop}/openapi/2024-07/function/cart-transform` with `function_id`.
- **Shoplazza-REFERENCE** remains excluded from git via `.gitignore` (`/Shoplazza-REFERENCE`).

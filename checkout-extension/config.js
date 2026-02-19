// Set your app URL here or via APP_URL / NEXT_PUBLIC_APP_URL when building.
// This is the URL where your Next.js app (and checkout-widget.js) is served.
const path = require("path");
const fs = require("fs");
let envUrl = process.env.APP_URL || process.env.NEXT_PUBLIC_APP_URL;
if (!envUrl) {
  const envPath = path.join(__dirname, "..", ".env.local");
  if (fs.existsSync(envPath)) {
    const match = fs.readFileSync(envPath, "utf8").match(/NEXT_PUBLIC_APP_URL=(.+)/);
    if (match) envUrl = match[1].replace(/^["']|["']$/g, "").trim();
  }
}
module.exports = {
  APP_URL: envUrl || "https://your-app-url.com",
};

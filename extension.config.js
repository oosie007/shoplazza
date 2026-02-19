// Used by Shoplazza CLI (npm run dev:extension). Set SHOPLAZZA_DEV_* in .env.local – never commit secrets.
const path = require("path");
const fs = require("fs");
const envPath = path.join(__dirname, ".env.local");
if (fs.existsSync(envPath)) {
  const content = fs.readFileSync(envPath, "utf8");
  content.split("\n").forEach((line) => {
    const m = line.match(/^([^#=]+)=(.*)$/);
    if (m && !process.env[m[1].trim()]) process.env[m[1].trim()] = m[2].replace(/^["']|["']$/g, "").trim();
  });
}
module.exports = {
  token: process.env.SHOPLAZZA_DEV_TOKEN || "",
  store: process.env.SHOPLAZZA_DEV_STORE || "https://oostest.myshoplaza.com/",
};

const fs = require("fs");
const path = require("path");

console.log("[build] Building itemprotect_extension...");

// Create dist folder if it doesn't exist
if (!fs.existsSync("dist")) {
  fs.mkdirSync("dist", { recursive: true });
}

// Copy src/index.js to dist/index.js
const srcPath = path.join(__dirname, "src", "index.js");
const distPath = path.join(__dirname, "dist", "index.js");

try {
  const srcCode = fs.readFileSync(srcPath, "utf8");
  fs.writeFileSync(distPath, srcCode, "utf8");
  console.log("[build] ✓ Extension built to dist/index.js");
} catch (err) {
  console.error("[build] ✗ Error building extension:", err.message);
  process.exit(1);
}

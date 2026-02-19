const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

const dir = __dirname;
const zipName = "cd-insure-checkout-extension.zip";
const zipPath = path.join(dir, zipName);

if (!fs.existsSync(path.join(dir, "dist", "extension.js"))) {
  console.error("Run npm run build first.");
  process.exit(1);
}

try {
  fs.existsSync(zipPath) && fs.unlinkSync(zipPath);
  execSync(
    `zip -r "${zipPath}" extension.json dist/extension.js`,
    { cwd: dir, stdio: "inherit" }
  );
  console.log("Created:", zipPath);
} catch (e) {
  console.error("zip command failed. On Windows use 7-Zip or add extension.json + dist/extension.js manually.");
  process.exit(1);
}

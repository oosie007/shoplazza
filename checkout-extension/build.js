const fs = require("fs");
const path = require("path");

const config = require("./config.js");
const APP_URL = (config.APP_URL || "").replace(/\/$/, "");

const distDir = path.join(__dirname, "dist");
if (!fs.existsSync(distDir)) {
  fs.mkdirSync(distDir, { recursive: true });
}

function build(file) {
  const srcPath = path.join(__dirname, "src", file);
  const distPath = path.join(distDir, file);
  let code = fs.readFileSync(srcPath, "utf8");
  code = code.replace(/__APP_URL__/g, APP_URL);
  fs.writeFileSync(distPath, code, "utf8");
  return distPath;
}

build("extension.js");
build("extension.esm.js");

console.log("Built checkout extension with APP_URL:", APP_URL || "(not set)");
console.log("Output: dist/extension.js, dist/extension.esm.js");

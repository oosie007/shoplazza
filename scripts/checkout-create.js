#!/usr/bin/env node
/**
 * Run shoplazza checkout create with automated inputs.
 * Usage: node scripts/checkout-create.js [projectDir]
 * Default: repo root (.) so CLI finds extension.config.js and extensions/
 */
const { spawn } = require("child_process");
const path = require("path");

const projectDir = path.resolve(__dirname, "..", process.argv[2] || ".");
const inputs = [
  "cd-insure-item-protection",           // project name
  "https://oostest.myshoplaza.com/",     // store URL
];

const child = spawn("shoplazza", ["checkout", "create"], {
  cwd: projectDir,
  stdio: ["pipe", "inherit", "inherit"],
  shell: true,
});

let index = 0;
function sendNext() {
  if (index < inputs.length) {
    child.stdin.write(inputs[index] + "\n");
    index++;
    setTimeout(sendNext, 800);
  } else {
    child.stdin.end();
  }
}

child.on("spawn", () => setTimeout(sendNext, 1500));
child.on("error", (err) => {
  console.error(err);
  process.exit(1);
});
child.on("close", (code) => process.exit(code || 0));

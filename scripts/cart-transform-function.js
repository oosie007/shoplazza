/**
 * Javy-compatible Cart Transform script. Single source for WASM build.
 * Keep in sync with CART_TRANSFORM_FUNCTION_CODE in item-protection-product.ts.
 * Build: npm run build:cart-transform-wasm
 */
function readInput() {
  var chunkSize = 1024;
  var inputChunks = [];
  var totalBytes = 0;
  while (1) {
    var buffer = new Uint8Array(chunkSize);
    var bytesRead = Javy.IO.readSync(0, buffer);
    totalBytes += bytesRead;
    if (bytesRead === 0) break;
    inputChunks.push(buffer.subarray(0, bytesRead));
  }
  var combined = new Uint8Array(totalBytes);
  var offset = 0;
  for (var i = 0; i < inputChunks.length; i++) {
    combined.set(inputChunks[i], offset);
    offset += inputChunks[i].length;
  }
  return JSON.parse(new TextDecoder().decode(combined));
}
function writeOutput(obj) {
  var json = JSON.stringify(obj);
  var bytes = new TextEncoder().encode(json);
  Javy.IO.writeSync(1, bytes);
}
var input = readInput();
var cart = input.cart || {};
var lineItems = cart.line_items || [];
var protectionLineId = null;
var subtotalOther = 0;
var percent = 20;
for (var i = 0; i < lineItems.length; i++) {
  var line = lineItems[i];
  var product = line.product || {};
  var title = (product.title || product.product_title || "").trim();
  if (title === "Item protection") {
    protectionLineId = String(line.id || line.item_id || "");
    var mfs = product.metafields || [];
    for (var j = 0; j < mfs.length; j++) {
      if (mfs[j].namespace === "cd_insure" && mfs[j].key === "percent") {
        var v = parseFloat(mfs[j].value);
        if (!isNaN(v) && v >= 0 && v <= 100) percent = v;
        break;
      }
    }
  } else {
    var price = parseFloat(product.price || product.price_amount || line.price || line.final_price || "0") || 0;
    var qty = parseInt(String(line.quantity || "1"), 10) || 1;
    subtotalOther += price * qty;
  }
}
var result = { operations: { update: [] } };
if (protectionLineId) {
  var premium = Math.round(subtotalOther * percent / 100 * 100) / 100;
  premium = Math.max(0, Math.min(999999999, premium));
  result.operations.update.push({
    id: protectionLineId,
    price: { adjustment_fixed_price: premium.toFixed(2) }
  });
}
writeOutput(result);

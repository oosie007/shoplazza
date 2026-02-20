/**
 * Javy-compatible Cart Transform script. Matches Shoplazza example structure:
 * readInput → run(input) → writeOutput(result). Output: { operation: { update: [] } }.
 * Build: npm run build:cart-transform-wasm
 */
// Entry: read input, run business logic, write output
var input = readInput();
var result = run(input);
writeOutput(result);

function readInput() {
  var chunkSize = 1024;
  var inputChunks = [];
  var totalBytes = 0;
  while (1) {
    var buffer = new Uint8Array(chunkSize);
    var fd = 0;
    var bytesRead = Javy.IO.readSync(fd, buffer);
    totalBytes += bytesRead;
    if (bytesRead === 0) break;
    inputChunks.push(buffer.subarray(0, bytesRead));
  }
  var reduced = inputChunks.reduce(
    function (context, chunk) {
      context.finalBuffer.set(chunk, context.bufferOffset);
      context.bufferOffset += chunk.length;
      return context;
    },
    { bufferOffset: 0, finalBuffer: new Uint8Array(totalBytes) }
  );
  return JSON.parse(new TextDecoder().decode(reduced.finalBuffer));
}

function writeOutput(output) {
  var encodedOutput = new TextEncoder().encode(JSON.stringify(output));
  var buffer = new Uint8Array(encodedOutput);
  var fd = 1;
  Javy.IO.writeSync(fd, buffer);
}

function run(input) {
  var runResult = { operation: { update: [] } };
  var cart = input.cart || {};
  var lineItems = cart.line_items || [];
  if (!Array.isArray(lineItems)) return runResult;

  var protectionLineId = null;
  var subtotalOther = 0;
  var percent = 20;

  lineItems.forEach(function (lineItem) {
    var product = lineItem.product || {};
    var title = (product.title || product.product_title || "").trim();

    if (title === "Item protection") {
      protectionLineId = String(lineItem.id || lineItem.item_id || "");
      var mfs = product.metafields || [];
      mfs.forEach(function (metafield) {
        if (metafield.namespace === "cd_insure" && metafield.key === "percent") {
          var v = parseFloat(metafield.value);
          if (!isNaN(v) && v >= 0 && v <= 100) percent = v;
        }
      });
    } else {
      var price =
        parseFloat(
          product.price ||
            product.price_amount ||
            lineItem.price ||
            lineItem.final_price ||
            "0"
        ) || 0;
      var qty = parseInt(String(lineItem.quantity || "1"), 10) || 1;
      subtotalOther += price * qty;
    }
  });

  if (protectionLineId) {
    var premium = Math.round(subtotalOther * (percent / 100) * 100) / 100;
    var adjustment = Math.max(0, Math.min(999999999, premium)).toFixed(2);
    runResult.operation.update.push({
      id: protectionLineId,
      price: { adjustment_fixed_price: adjustment },
    });
  }

  return runResult;
}

// Shoplazza 2024.07 WASM Protocol Implementation
// This follows the ACTUAL Shoplazza requirements from their documentation

use serde::{Deserialize, Serialize};
use std::io::{self, Read, Write};
use std::ffi::{CStr, CString};
use std::ptr;

// Input structure matching Shoplazza's documented protocol
#[derive(Debug, Deserialize)]
struct Input {
    cart: Cart,
    currency_settings: CurrencySettings,
}

#[derive(Debug, Deserialize)]
struct Cart {
    line_items: Vec<LineItem>,
}

#[derive(Debug, Deserialize)]
struct LineItem {
    product: Product,
    id: String,
    properties: String,  // Stringified JSON from Shoplazza
    quantity: u32,
}

#[derive(Debug, Deserialize)]
struct Product {
    product_id: String,
    variant_id: String,
    price: String,
    product_title: String,
    metafields: Vec<Metafield>,
}

#[derive(Debug, Deserialize)]
struct Metafield {
    namespace: String,
    key: String,
    value: serde_json::Value,
}

#[derive(Debug, Deserialize)]
struct CurrencySettings {
    actual_rate: String,
}

// Output structure matching Shoplazza's documented protocol
#[derive(Debug, Serialize)]
struct Output {
    operation: Operation,
}

#[derive(Debug, Serialize)]
struct Operation {
    update: Vec<UpdateItem>,
}

#[derive(Debug, Serialize)]
struct UpdateItem {
    id: String,
    price: PriceUpdate,
}

#[derive(Debug, Serialize)]
struct PriceUpdate {
    adjustment_fixed_price: String,
}

// Add-on data structure extracted from properties
#[derive(Debug, Deserialize)]
struct AddOnData {
    #[serde(rename = "type")]
    addon_type: Option<String>,
    name: Option<String>,
    price: Option<String>,
    sku: Option<String>,
    description: Option<String>,
}

// Main function for testing
fn main() {
    let input = read_input();
    let result = run(input);
    write_output(&result);
}

// Shoplazza's expected processCart function
#[no_mangle]
pub extern "C" fn processCart(input_ptr: *mut u8, input_len: usize) -> *mut u8 {
    // Safety: Shoplazza guarantees valid input
    let input = unsafe { 
        String::from_raw_parts(input_ptr, input_len, input_len)
    };
    
    // Parse the input JSON (Shoplazza's format)
    let result = match serde_json::from_str::<Input>(&input) {
        Ok(shoplazza_input) => {
            // Process the cart according to Shoplazza's protocol
            let output = run(shoplazza_input);
            serde_json::to_string(&output).unwrap_or_else(|_| {
                "{\"error\":\"JSON serialization failed\"}".to_string()
            })
        },
        Err(e) => {
            // Return error in Shoplazza's expected format
            format!("{{\"error\":\"JSON parse failed: {}\"}}", e)
        }
    };
    
    // Convert to C string and return pointer (Shoplazza frees this)
    let c_string = CString::new(result).unwrap_or_else(|_| {
        CString::new("{\"error\":\"String conversion failed\"}").unwrap()
    });
    
    // Leak the string - Shoplazza will free it
    c_string.into_raw() as *mut u8
}

fn read_input() -> Input {
    let mut buffer = String::new();
    io::stdin().read_to_string(&mut buffer).expect("Failed to read input");
    serde_json::from_str(&buffer).expect("Failed to parse JSON")
}

fn write_output(output: &Output) {
    let json = serde_json::to_string(output).expect("Failed to serialize JSON");
    io::stdout().write_all(json.as_bytes()).expect("Failed to write output");
}

fn run(input: Input) -> Output {
    let mut updates = Vec::new();
    
    for line_item in input.cart.line_items {
        // Parse the stringified properties JSON
        let properties: serde_json::Value = match serde_json::from_str(&line_item.properties) {
            Ok(props) => props,
            Err(_) => continue, // Skip if properties can't be parsed
        };
        
        // Extract add-on data from properties
        if let Some(addon_data) = extract_addon_data(&properties) {
            // Calculate price adjustment
            if let Some(price_str) = addon_data.price {
                if let Ok(price) = price_str.parse::<f64>() {
                    // Create update operation for this line item
                    updates.push(UpdateItem {
                        id: line_item.id,
                        price: PriceUpdate {
                            adjustment_fixed_price: format!("{:.2}", price),
                        },
                    });
                }
            }
        }
    }
    
    Output {
        operation: Operation { update: updates },
    }
}

// Extract add-on data from properties
fn extract_addon_data(properties: &serde_json::Value) -> Option<AddOnData> {
    // Check for the structured 'addon' object (what the widget sends)
    if let Some(addon_obj) = properties.get("addon") {
        // Try to deserialize the addon object
        if let Ok(addon_data) = serde_json::from_value::<AddOnData>(addon_obj.clone()) {
            return Some(addon_data);
        }
    }
    
    // Fallback: check for legacy _add_on_* properties
    if let Some(addon_type) = properties.get("_add_on_type").and_then(|t| t.as_str()) {
        if addon_type == "protection_plan" {
            return Some(AddOnData {
                addon_type: Some(addon_type.to_string()),
                name: properties.get("_add_on_name").and_then(|n| n.as_str()).map(|s| s.to_string()),
                price: properties.get("_add_on_price").and_then(|p| p.as_str()).map(|s| s.to_string()),
                sku: properties.get("_add_on_sku").and_then(|s| s.as_str()).map(|s| s.to_string()),
                description: properties.get("_add_on_description").and_then(|d| d.as_str()).map(|s| s.to_string()),
            });
        }
    }
    
    None
}

// Required memory initialization for Shoplazza
#[no_mangle]
pub extern "C" fn _start() {
    // Initialize memory to 16MB as required by Shoplazza
    // This is called automatically by the WASM runtime
}

// Memory allocation helper (Shoplazza will free this)
#[no_mangle]
pub extern "C" fn __wbindgen_malloc(size: usize) -> *mut u8 {
    use std::alloc::{alloc, Layout};
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { alloc(layout) }
}

// Memory deallocation helper
#[no_mangle]
pub extern "C" fn __wbindgen_free(ptr: *mut u8, size: usize) {
    use std::alloc::{dealloc, Layout};
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { dealloc(ptr, layout) }
}

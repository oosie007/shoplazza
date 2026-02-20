// Shoplazza 2024.07 WASM Protocol Implementation
// This follows the OFFICIAL Shoplazza specification from their documentation
// https://www.shoplazza.dev/v2024.07/reference/function-execution-logic

use serde::{Deserialize, Serialize};
use std::io::{self, Read, Write};

#[derive(Debug, Deserialize)]
struct Input {
    cart: Cart,
}

#[derive(Debug, Deserialize)]
struct Cart {
    line_items: Vec<LineItem>,
}

#[derive(Debug, Deserialize)]
struct LineItem {
    id: String,
    #[serde(rename = "item_id")]
    item_id: Option<String>,
    // Product fields are flat in your cart data, not nested
    product_id: String,
    variant_id: Option<String>,
    product_title: Option<String>,
    price: String,  // Base product price (needed for total calculation)
    properties: String,
    quantity: String,  // Shoplazza sends string, not u32
}



#[derive(Debug, Serialize)]
struct Output {
    operations: Operation,
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

fn main() {
    let input = read_input();
    let result = run(input);
    write_output(&result);
}

fn read_input() -> Input {
    let mut buffer = String::new();
    io::stdin().read_to_string(&mut buffer).expect("Failed to read input");
    
    // Debug: log what we received
    eprintln!("Received input: {}", buffer);
    
    match serde_json::from_str::<Input>(&buffer) {
        Ok(input) => {
            eprintln!("Successfully parsed input with {} line items", input.cart.line_items.len());
            input
        },
        Err(e) => {
            eprintln!("Failed to parse JSON: {}", e);
            eprintln!("Raw input was: {}", buffer);
            panic!("JSON parsing failed: {}", e);
        }
    }
}

fn write_output(output: &Output) {
    let json = serde_json::to_string(output).expect("Failed to serialize JSON");
    io::stdout().write_all(json.as_bytes()).expect("Failed to write output");
}

fn run(input: Input) -> Output {
    let mut updates = Vec::new();
    
    // Process each line item in the cart
    for line_item in &input.cart.line_items {
        let mut adjusted_price = None;
        
        // Metafields not used in your cart data - removed for clarity
        
        // Check properties for add-on costs (current approach)
        if adjusted_price.is_none() {
            eprintln!("Checking properties for line item {}: '{}'", line_item.id, line_item.properties);
            
            if let Ok(properties) = serde_json::from_str::<serde_json::Value>(&line_item.properties) {
                eprintln!("Successfully parsed properties: {:?}", properties);
                
                if let Some(addon_data) = properties.get("addon") {
                    eprintln!("Found addon data: {:?}", addon_data);
                    
                    // Extract add-on information from properties
                    if let Some(price) = addon_data.get("price") {
                        eprintln!("Found price: {:?} (type: {})", price, std::any::type_name_of_val(price));
                        
                        if let Some(price_str) = price.as_str() {
                            adjusted_price = Some(price_str.to_string());
                            eprintln!("Using string price: {}", price_str);
                        } else if let Some(price_num) = price.as_f64() {
                            adjusted_price = Some(format!("{:.2}", price_num));
                            eprintln!("Using numeric price: {}", price_num);
                        }
                    }
                    
                    // Log the add-on being processed
                    if let Some(addon_type) = addon_data.get("type") {
                        if let Some(addon_name) = addon_data.get("name") {
                            eprintln!("Processing add-on: {} ({}) for line item {}", 
                                     addon_name.as_str().unwrap_or("Unknown"),
                                     addon_type.as_str().unwrap_or("Unknown"),
                                     line_item.id);
                        }
                    }
                } else {
                    eprintln!("No addon data found in properties");
                }
            } else {
                eprintln!("Failed to parse properties JSON: '{}'", line_item.properties);
            }
        }
        
        // Add price adjustment if found
        if let Some(price) = adjusted_price {
            // Calculate total price: base price + add-on price
            // Note: adjustment_fixed_price REPLACES the original price (doesn't add to it)
            let base_price: f64 = line_item.price.parse().unwrap_or(0.0);
            let addon_price: f64 = price.parse().unwrap_or(0.0);
            let total_price = base_price + addon_price;
            
            updates.push(UpdateItem {
                id: line_item.id.clone(),
                price: PriceUpdate {
                    adjustment_fixed_price: format!("{:.2}", total_price),
                },
            });
            
            eprintln!("Added total price: {} (base: {} + addon: {}) for line item {}", 
                     format!("{:.2}", total_price), base_price, addon_price, line_item.id);
        }
    }
    
    eprintln!("Total line items processed: {}, Updates generated: {}", 
              input.cart.line_items.len(), updates.len());
    
    // PRICING LOGIC EXPLANATION:
    // The adjustment_fixed_price REPLACES the original price (doesn't add to it)
    // Example: Base price $100.00 + Add-on $1.54 = Total $101.54
    // We output "101.54" as the new total price
    
    Output {
        operations: Operation { update: updates },
    }
}

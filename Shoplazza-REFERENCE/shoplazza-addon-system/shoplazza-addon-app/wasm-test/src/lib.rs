use wasm_bindgen::prelude::*;
use serde::{Deserialize, Serialize};

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
    product: Product,
    properties: String,
    quantity: u32,
}

#[derive(Debug, Deserialize)]
struct Product {
    product_id: String,
    variant_id: Option<String>,
    product_title: Option<String>,
    metafields: Vec<Metafield>,
}

#[derive(Debug, Deserialize)]
struct Metafield {
    namespace: String,
    key: String,
    value: String,
}

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

#[wasm_bindgen]
pub fn processCart(input: &str) -> String {
    // Parse the input JSON
    let input_data: Input = match serde_json::from_str(input) {
        Ok(data) => data,
        Err(e) => {
            return format!("{{\"error\":\"Failed to parse input: {}\"}}", e);
        }
    };
    
    // Process the cart using the same logic as lib-shoplazza.rs
    let result = run(input_data);
    
    // Serialize the result to JSON string
    match serde_json::to_string(&result) {
        Ok(json) => json,
        Err(e) => format!("{{\"error\":\"Failed to serialize output: {}\"}}", e),
    }
}

fn run(input: Input) -> Output {
    let mut updates = Vec::new();
    
    // Process each line item in the cart
    for line_item in &input.cart.line_items {
        let mut adjusted_price = None;
        
        // First check metafields for price adjustments (legacy support)
        for metafield in &line_item.product.metafields {
            if metafield.namespace == "cdh_shoplazza_addon" && metafield.key == "addon_selected" {
                if metafield.value == "true" {
                    // This is a legacy metafield approach - we'll use a default add-on price
                    adjusted_price = Some("19.99".to_string());
                }
            }
        }
        
        // Check properties for add-on costs (current approach)
        if adjusted_price.is_none() {
            if let Ok(properties) = serde_json::from_str::<serde_json::Value>(&line_item.properties) {
                if let Some(addon_data) = properties.get("addon") {
                    // Extract add-on information from properties
                    if let Some(price) = addon_data.get("price") {
                        if let Some(price_str) = price.as_str() {
                            adjusted_price = Some(price_str.to_string());
                        } else if let Some(price_num) = price.as_f64() {
                            adjusted_price = Some(format!("{:.2}", price_num));
                        }
                    }
                }
            }
        }
        
        // Add price adjustment if found
        if let Some(price) = adjusted_price {
            updates.push(UpdateItem {
                id: line_item.id.clone(),
                price: PriceUpdate {
                    adjustment_fixed_price: price.clone(),
                },
            });
        }
    }
    
    Output {
        operation: Operation { update: updates },
    }
}

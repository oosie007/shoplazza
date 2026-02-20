// Working wasm-bindgen example following official documentation
use wasm_bindgen::prelude::*;

// Import console.log for debugging
#[wasm_bindgen]
extern "C" {
    #[wasm_bindgen(js_namespace = console)]
    fn log(s: &str);
}

macro_rules! console_log {
    ($($t:tt)*) => (log(&format_args!($($t)*).to_string()))
}

#[wasm_bindgen]
pub fn processCart(input: JsValue) -> Result<JsValue, JsValue> {
    console_log!("🔍 processCart called with input: {:?}", input);
    
    // Convert JsValue to string for processing
    let input_str = match input.as_string() {
        Some(s) => {
            console_log!("✅ Input converted to string: {}", s);
            s
        },
        None => {
            console_log!("❌ Failed to convert input to string");
            return Err(JsValue::from_str("Invalid input"));
        }
    };
    
    console_log!("📝 Processing input string: {}", input_str);
    
    // Create the result as a JsValue
    let result = serde_json::json!({
        "operation": {
            "update": [
                {
                    "id": "1",
                    "price": {
                        "adjustment_fixed_price": "19.99"
                    }
                }
            ]
        }
    });
    
    console_log!("🏗️ Created result JSON: {:?}", result);
    
    // Convert back to JsValue
    match serde_wasm_bindgen::to_value(&result) {
        Ok(js_result) => {
            console_log!("✅ Successfully converted to JsValue: {:?}", js_result);
            Ok(js_result)
        },
        Err(e) => {
            console_log!("❌ Failed to convert to JsValue: {:?}", e);
            Err(JsValue::from_str(&format!("Serialization error: {:?}", e)))
        }
    }
}

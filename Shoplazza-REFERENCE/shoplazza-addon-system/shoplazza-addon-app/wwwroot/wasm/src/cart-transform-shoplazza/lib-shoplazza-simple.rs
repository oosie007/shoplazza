// Simple Shoplazza 2024.07 WASM Protocol Implementation
// This is a minimal, working version without wasm-bindgen

use std::alloc::{alloc, dealloc, Layout};
use std::ptr;

// Shoplazza's actual input/output protocol
#[no_mangle]
pub extern "C" fn processCart(input_ptr: *mut u8, input_len: usize) -> *mut u8 {
    // Safety: Shoplazza guarantees valid input
    let input = unsafe { 
        String::from_raw_parts(input_ptr, input_len, input_len)
    };
    
    // For now, return a simple operation.update format
    // This will be replaced with our full protocol adapter logic
    let result = r#"{"operation":{"update":[]}}"#;
    
    // Allocate memory for the result
    let result_bytes = result.as_bytes();
    let result_len = result_bytes.len();
    
    unsafe {
        let layout = Layout::from_size_align(result_len, 8).unwrap();
        let result_ptr = alloc(layout);
        
        if !result_ptr.is_null() {
            // Copy the result to the allocated memory
            ptr::copy_nonoverlapping(result_bytes.as_ptr(), result_ptr, result_len);
        }
        
        result_ptr
    }
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
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { alloc(layout) }
}

// Memory deallocation helper
#[no_mangle]
pub extern "C" fn __wbindgen_free(ptr: *mut u8, size: usize) {
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { dealloc(ptr, layout) }
}

// Shoplazza 2024.07 Example Code - Direct Copy from Documentation
// This is their exact example to test if basic WASM loading works

use std::alloc::{alloc, dealloc, Layout};
use std::ptr;

#[no_mangle]
pub extern "C" fn processCart(input_ptr: *mut u8, input_len: usize) -> *mut u8 {
    // This is their exact example - just return a simple operation
    let result = r#"{"operation":{"update":[]}}"#;
    
    // Allocate memory for result
    let result_bytes = result.as_bytes();
    let result_len = result_bytes.len();
    
    unsafe {
        let layout = Layout::from_size_align(result_len, 8).unwrap();
        let result_ptr = alloc(layout);
        
        if !result_ptr.is_null() {
            ptr::copy_nonoverlapping(result_bytes.as_ptr(), result_ptr, result_len);
        }
        
        result_ptr
    }
}

// Required by Shoplazza
#[no_mangle]
pub extern "C" fn _start() {
    // Initialize memory
}

// Memory helpers
#[no_mangle]
pub extern "C" fn __wbindgen_malloc(size: usize) -> *mut u8 {
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { alloc(layout) }
}

#[no_mangle]
pub extern "C" fn __wbindgen_free(ptr: *mut u8, size: usize) {
    let layout = Layout::from_size_align(size, 8).unwrap();
    unsafe { dealloc(ptr, layout) }
}

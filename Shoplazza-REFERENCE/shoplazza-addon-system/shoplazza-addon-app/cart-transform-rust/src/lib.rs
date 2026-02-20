// Extremely simple WASM - testing absolute minimum
// This should work with any WASM runtime

#[no_mangle]
pub extern "C" fn processCart(_input_ptr: *mut u8, _input_len: usize) -> *mut u8 {
    // Return null pointer - simplest possible return
    std::ptr::null_mut()
}

#[no_mangle]
pub extern "C" fn _start() {
    // Do absolutely nothing
}

use std::slice;
use std::ffi::c_char;
use std::ffi::CStr;
use image::ImageBuffer;
use image::DynamicImage;
use image::ImageFormat;

#[no_mangle]
pub unsafe extern "C" fn write_jpeg_data(
    data: *const u8,
    size: i32,
    width: i32,
    height: i32,
    path: *const c_char,
){
    let path_str = CStr::from_ptr(path).to_str().unwrap();
    let bytes_from_ptr = slice::from_raw_parts(data, size as usize).to_vec();

    let img = ImageBuffer::from_vec(width as u32, height as u32, bytes_from_ptr).unwrap();
    let img = DynamicImage::ImageRgba8(img).flipv();

    img.to_rgb8().save_with_format(path_str, ImageFormat::Jpeg).unwrap();
}

use image::DynamicImage;
use image::ImageBuffer;
use image::ImageFormat;
use turbojpeg::Image;
use turbojpeg::PixelFormat;
use std::ffi::c_char;
use std::ffi::CStr;
use std::slice;
use turbojpeg::{compress_image, Decompressor};

#[no_mangle]
pub unsafe extern "C" fn write_jpeg_data(
    data: *const u8,
    size: i32,
    width: i32,
    height: i32,
    path: *const c_char,
) {
    // turbojpeg conflicts with unity-specific libjpeg when running on unity iOS]

    // let path_str = CStr::from_ptr(path).to_str().unwrap();
    // let bytes_from_ptr = slice::from_raw_parts(data, size as usize).to_vec();

    // let img = ImageBuffer::from_vec(width as u32, height as u32, bytes_from_ptr).unwrap();
    // let img = DynamicImage::ImageRgba8(img);

    // let buffer = compress_image(&img.to_rgb8(), 90, turbojpeg::Subsamp::None).unwrap();
    // std::fs::write(path_str, &buffer).unwrap();

    let path_str = CStr::from_ptr(path).to_str().unwrap();
    let bytes_from_ptr = slice::from_raw_parts(data, size as usize).to_vec();

    let img = ImageBuffer::from_vec(width as u32, height as u32, bytes_from_ptr).unwrap();
    let img = DynamicImage::ImageRgba8(img);

    img.to_rgb8().save_with_format(path_str, ImageFormat::Jpeg).unwrap();
}

#[cfg(target_os = "macos")]
#[no_mangle]
pub unsafe extern "C" fn decode_jpeg(
    data: *const u8,
    src_size: *const u8,
    dst_width: *mut i32,
    dst_height: *mut i32,
    dst_size: *mut i32,
    dst_data: *mut u8
) {
    let input_bytes = slice::from_raw_parts(data, src_size as usize).to_vec();

    let mut decompressor = Decompressor::new().unwrap();

    let header = decompressor.read_header(&input_bytes).unwrap();
    let (width, height) = (header.width, header.height);
    *dst_width = header.width as i32;
    *dst_height = header.height as i32;

    let mut image = Image {
        pixels: slice::from_raw_parts_mut(dst_data, width*height*3 as usize),
        width: width,
        pitch: 3 * width, // we use no padding between rows
        height: height,
        format: PixelFormat::RGB,
    };

    decompressor.decompress(&input_bytes, image.as_deref_mut()).unwrap();

    *dst_size = image.pixels.len() as i32;
}

extern crate noise;

use noise::{Fbm, MultiFractal, NoiseFn, Point2, Point3, Point4, RidgedMulti, Seedable};
use std::mem;

pub struct NoiseGenerator {
    pub period_x: f64,
    pub period_y: f64,
    pub noise_type: NoiseType,
}

impl NoiseGenerator {
    fn set_frequency(&mut self, frequency: f64) {
        match self.noise_type {
            NoiseType::Fbm(ref mut fbm) => {
                let mut repl = fbm.clone().set_frequency(frequency);
                mem::swap(fbm, &mut repl)
            }
            NoiseType::Ridged(ref mut ridged) => {
                let mut repl = ridged.clone().set_frequency(frequency);
                mem::swap(ridged, &mut repl)
            }
        }
    }

    fn set_lacunarity(&mut self, lacunarity: f64) {
        match self.noise_type {
            NoiseType::Fbm(ref mut fbm) => {
                let mut repl = fbm.clone().set_lacunarity(lacunarity);
                mem::swap(fbm, &mut repl)
            }
            NoiseType::Ridged(ref mut ridged) => {
                let mut repl = ridged.clone().set_lacunarity(lacunarity);
                mem::swap(ridged, &mut repl)
            }
        }
    }

    fn set_persistence(&mut self, persistence: f64) {
        match self.noise_type {
            NoiseType::Fbm(ref mut fbm) => {
                let mut repl = fbm.clone().set_persistence(persistence);
                mem::swap(fbm, &mut repl)
            }
            NoiseType::Ridged(ref mut ridged) => {
                let mut repl = ridged.clone().set_persistence(persistence);
                mem::swap(ridged, &mut repl)
            }
        }
    }

    fn set_octaves(&mut self, octaves: usize) {
        match self.noise_type {
            NoiseType::Fbm(ref mut fbm) => {
                let mut repl = fbm.clone().set_octaves(octaves);
                mem::swap(fbm, &mut repl)
            }
            NoiseType::Ridged(ref mut ridged) => {
                let mut repl = ridged.clone().set_octaves(octaves);
                mem::swap(ridged, &mut repl)
            }
        }
    }

    fn set_seed(&mut self, seed: u32) {
        match self.noise_type {
            NoiseType::Fbm(ref mut fbm) => {
                let mut repl = fbm.clone().set_seed(seed);
                mem::swap(fbm, &mut repl)
            }
            NoiseType::Ridged(ref mut ridged) => {
                let mut repl = ridged.clone().set_seed(seed);
                mem::swap(ridged, &mut repl)
            }
        }
    }
}

impl NoiseFn<Point2<f64>> for NoiseGenerator {
    fn get(&self, point: Point2<f64>) -> f64 {
        match self.noise_type {
            NoiseType::Fbm(ref fbm) => fbm.get(point),
            NoiseType::Ridged(ref ridged) => ridged.get(point),
        }
    }
}

impl NoiseFn<Point3<f64>> for NoiseGenerator {
    fn get(&self, point: Point3<f64>) -> f64 {
        match self.noise_type {
            NoiseType::Fbm(ref fbm) => fbm.get(point),
            NoiseType::Ridged(ref ridged) => ridged.get(point),
        }
    }
}

impl NoiseFn<Point4<f64>> for NoiseGenerator {
    fn get(&self, point: Point4<f64>) -> f64 {
        match self.noise_type {
            NoiseType::Fbm(ref fbm) => fbm.get(point),
            NoiseType::Ridged(ref ridged) => ridged.get(point),
        }
    }
}


pub enum NoiseType {
    Fbm(Fbm),
    Ridged(RidgedMulti),
}

impl NoiseType {
    pub fn new(noise_type: u8) -> Self {
        match noise_type {
            0 => {
                let fbm = Fbm::new();
                NoiseType::Fbm(fbm)
            }
            1 => {
                let ridged = RidgedMulti::new();
                NoiseType::Ridged(ridged)
            }
            _ => {
                panic!("Invalid type code.");
            }
        }
    }
}

#[repr(C)]
#[derive(Copy, Clone, Debug)]
pub struct Vec4 {
    pub x: f64,
    pub y: f64,
    pub z: f64,
    pub w: f64,
}

#[repr(C)]
#[derive(Copy, Clone, Debug)]
pub struct Vec3 {
    pub x: f64,
    pub y: f64,
    pub z: f64
}

#[repr(C)]
#[derive(Copy, Clone, Debug)]
pub struct Vec2 {
    pub x: f64,
    pub y: f64,
}

#[no_mangle]
pub unsafe extern "C" fn generator_new(noise_type: u8) -> *mut NoiseGenerator {
    let noise = NoiseGenerator {
        period_x: 1.0,
        period_y: 1.0,
        noise_type: NoiseType::new(noise_type),
    };
    let boxed = Box::new(noise);
    Box::into_raw(boxed)
}

#[no_mangle]
pub unsafe extern "C" fn generator_dispose(ptr: *mut NoiseGenerator) {
    let boxed = Box::from_raw(ptr);
    mem::drop(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn get_noise_2d(ptr: *mut NoiseGenerator, vec: Vec2) -> f64 {
    let boxed = Box::from_raw(ptr);
    let val = boxed.get([vec.x, vec.y]);
    mem::forget(boxed);
    val
}

#[no_mangle]
pub unsafe extern "C" fn get_noise_3d(ptr: *mut NoiseGenerator, vec: Vec3) -> f64 {
    let boxed = Box::from_raw(ptr);
    let val = boxed.get([vec.x, vec.y, vec.z]);
    mem::forget(boxed);
    val
}

#[no_mangle]
pub unsafe extern "C" fn get_noise_4d(ptr: *mut NoiseGenerator, vec: Vec4) -> f64 {
    let boxed = Box::from_raw(ptr);
    let val = boxed.get([vec.x, vec.y, vec.z, vec.w]);
    mem::forget(boxed);
    val
}

#[no_mangle]
pub unsafe extern "C" fn get_noise_tiled_2d(ptr: *mut NoiseGenerator, vec: Vec2) -> f64 {
    let boxed = Box::from_raw(ptr);
    let val = actual_get_noise_tiled_2d(&boxed, [vec.x, vec.y]);
    mem::forget(boxed);
    val
}

fn actual_get_noise_tiled_2d(gen: &NoiseGenerator, point: Point2<f64>) -> f64 {
    // https://www.gamedev.net/blogs/entry/2138456-seamless-noise/
    let s = point[0] / gen.period_x;
    let t = point[1] / gen.period_y;

    const X1: f64 = 0f64;
    const X2: f64 = 1f64;
    const Y1: f64 = 0f64;
    const Y2: f64 = 1f64;

    const DX: f64 = X2 - X1;
    const DY: f64 = Y2 - Y1;

    const TAU: f64 = std::f64::consts::PI * 2f64;

    let nx = X1 + (s * TAU).cos() * (DX / TAU);
    let ny = Y1 + (t * TAU).cos() * (DY / TAU);
    let nz = X1 + (s * TAU).sin() * (DX / TAU);
    let nw = Y1 + (t * TAU).sin() * (DY / TAU);

    gen.get([nx, ny, nz, nw])
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_frequency(ptr: *mut NoiseGenerator, frequency: f64) {
    let mut boxed = Box::from_raw(ptr);
    boxed.set_frequency(frequency);
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_octaves(ptr: *mut NoiseGenerator, octaves: u32) {
    let mut boxed = Box::from_raw(ptr);
    boxed.set_octaves(octaves as usize);
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_lacunarity(ptr: *mut NoiseGenerator, lacunarity: f64) {
    let mut boxed = Box::from_raw(ptr);
    boxed.set_lacunarity(lacunarity);
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_persistence(ptr: *mut NoiseGenerator, persistence: f64) {
    let mut boxed = Box::from_raw(ptr);
    boxed.set_persistence(persistence);
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_period_x(ptr: *mut NoiseGenerator, period_x: f64) {
    let mut boxed = Box::from_raw(ptr);
    boxed.period_x = period_x;
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_period_y(ptr: *mut NoiseGenerator, period_y: f64) {
    let mut boxed = Box::from_raw(ptr);
    boxed.period_y = period_y;
    mem::forget(boxed);
}

#[no_mangle]
pub unsafe extern "C" fn generator_set_seed(ptr: *mut NoiseGenerator, seed: u32) {
    let mut boxed = Box::from_raw(ptr);
    boxed.set_seed(seed);
    mem::forget(boxed);
}

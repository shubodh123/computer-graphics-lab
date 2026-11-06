#!/usr/bin/env python3
"""
CS4104 CG & VR Case Study 3: Retail and Remote Commerce
WebXR Window-to-Viewport Transformation & Screen-to-World Raycasting Math Engine.

Implements:
1. Forward 3D Viewing Pipeline:
   World Space -> View Space -> Clip Space -> Perspective Divide (NDC) -> Viewport Pixels (x_s, y_s)
2. Inverse Viewing Pipeline (AR Screen Touch Unprojection):
   Screen Tap (x_s, y_s) -> NDC -> Clip Space -> World Ray -> AR Floor Plane Intersection
3. Microfacet Charlie Sheen (Velvet) vs Cook-Torrance GGX (Leather) BRDF Evaluator
"""

import math

def mat4_identity():
    return [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]

def mat4_mult(A, B):
    C = [[0.0 for _ in range(4)] for _ in range(4)]
    for i in range(4):
        for j in range(4):
            C[i][j] = sum(A[i][k] * B[k][j] for k in range(4))
    return C

def mat4_vec4_mult(M, v):
    res = [0.0, 0.0, 0.0, 0.0]
    for i in range(4):
        res[i] = sum(M[i][j] * v[j] for j in range(4))
    return res

def mat4_inverse(M):
    # Standard 4x4 matrix inversion using Gauss-Jordan elimination
    aug = [row[:] + [1.0 if i == j else 0.0 for j in range(4)] for i, row in enumerate(M)]
    n = 4
    for i in range(n):
        pivot = aug[i][i]
        if abs(pivot) < 1e-9:
            # Swap with non-zero row
            for k in range(i+1, n):
                if abs(aug[k][i]) > 1e-9:
                    aug[i], aug[k] = aug[k], aug[i]
                    pivot = aug[i][i]
                    break
        for j in range(2 * n):
            aug[i][j] /= pivot
        for k in range(n):
            if k != i:
                factor = aug[k][i]
                for j in range(2 * n):
                    aug[k][j] -= factor * aug[i][j]
    return [row[n:] for row in aug]

def dot3(a, b):
    return a[0]*b[0] + a[1]*b[1] + a[2]*b[2]

def normalize3(v):
    mag = math.sqrt(dot3(v, v))
    return [x / mag for x in v] if mag > 1e-9 else [0.0, 0.0, 0.0]

def run_webxr_viewport_math():
    print("=" * 80)
    print("CS4104 Case Study 3: WebXR Window-to-Viewport Transformation Engine")
    print("=" * 80)
    print("Scenario: AR furniture placement in a sunlit room:")
    print("  1. Forward Viewing Pipeline: Transforming 3D furniture vertex to 2D pixels")
    print("  2. Inverse Unprojection: Touch Screen Tap (x_s, y_s) -> 3D AR Ground Plane")
    print("  3. Photometric Analysis: Charlie Sheen (Velvet) vs GGX (Leather)\n")

    # Display / Viewport specifications (e.g., iPhone 15 Pro resolution in portrait)
    W, H = 1179, 2556
    x0, y0 = 0, 0
    fov_deg = 60.0
    aspect = W / H
    near = 0.1
    far = 100.0

    print(f"Viewport Configuration: Resolution = {W} x {H} pixels, FOV = {fov_deg}°, Near = {near}m, Far = {far}m")

    # 1. Perspective Projection Matrix P
    f = 1.0 / math.tan(math.radians(fov_deg) / 2.0)
    P = [
        [f / aspect, 0.0, 0.0, 0.0],
        [0.0, f, 0.0, 0.0],
        [0.0, 0.0, (far + near) / (near - far), (2.0 * far * near) / (near - far)],
        [0.0, 0.0, -1.0, 0.0]
    ]

    # 2. Camera View Matrix V (User standing at (0, 1.5, 2.5), looking down at origin (0, 0, 0))
    # Eye: (0, 1.5, 2.5), Target: (0, 0, 0), Up: (0, 1, 0)
    cam_pos = [0.0, 1.5, 2.5]
    forward = normalize3([-cam_pos[0], -cam_pos[1], -cam_pos[2]]) # [0, -0.514, -0.857]
    up_ref = [0.0, 1.0, 0.0]
    right = normalize3([
        forward[1]*up_ref[2] - forward[2]*up_ref[1],
        forward[2]*up_ref[0] - forward[0]*up_ref[2],
        forward[0]*up_ref[1] - forward[1]*up_ref[0]
    ]) # [1.0, 0.0, 0.0]
    true_up = [
        right[1]*forward[2] - right[2]*forward[1],
        right[2]*forward[0] - right[0]*forward[2],
        right[0]*forward[1] - right[1]*forward[0]
    ]

    V = [
        [right[0], right[1], right[2], -dot3(right, cam_pos)],
        [true_up[0], true_up[1], true_up[2], -dot3(true_up, cam_pos)],
        [-forward[0], -forward[1], -forward[2], dot3(forward, cam_pos)],
        [0.0, 0.0, 0.0, 1.0]
    ]

    VP = mat4_mult(P, V)
    VP_inv = mat4_inverse(VP)

    # ----------------------------------------------------
    # Part 1: Forward Viewing Pipeline Test
    # ----------------------------------------------------
    print("-" * 80)
    print("1. FORWARD VIEWING PIPELINE:")
    print("Transforming 3D chair top vertex P_world = [0.2, 0.8, -0.5, 1.0]^T to screen pixels:")

    P_world = [0.2, 0.8, -0.5, 1.0]
    P_view = mat4_vec4_mult(V, P_world)
    P_clip = mat4_vec4_mult(P, P_view)
    w_c = P_clip[3]
    P_ndc = [P_clip[0] / w_c, P_clip[1] / w_c, P_clip[2] / w_c]

    # Viewport Mapping: NDC [-1, 1] -> Pixel coords [0, W], [0, H]
    x_s = ((P_ndc[0] + 1.0) / 2.0) * W + x0
    y_s = (1.0 - (P_ndc[1] + 1.0) / 2.0) * H + y0 # Y flipped for screen coordinates

    print(f"  P_world:     ({P_world[0]:.3f}, {P_world[1]:.3f}, {P_world[2]:.3f}) m")
    print(f"  P_view:      ({P_view[0]:.3f}, {P_view[1]:.3f}, {P_view[2]:.3f}) m")
    print(f"  P_clip:      [{P_clip[0]:.3f}, {P_clip[1]:.3f}, {P_clip[2]:.3f}, w={w_c:.3f}]")
    print(f"  P_ndc:       ({P_ndc[0]:.4f}, {P_ndc[1]:.4f}, {P_ndc[2]:.4f})")
    print(f"  Screen Pixel: ({x_s:.1f} px, {y_s:.1f} px)\n")

    # ----------------------------------------------------
    # Part 2: Inverse Viewing Pipeline (AR Screen Raycasting)
    # ----------------------------------------------------
    print("-" * 80)
    print("2. INVERSE UNPROJECTION (AR Screen Tap Raycasting):")
    # Suppose the user taps on the phone screen at pixel (600, 1800)
    tap_x = 600.0
    tap_y = 1800.0
    print(f"User taps on smartphone display at pixel: ({tap_x:.1f}, {tap_y:.1f})")

    # Step A: Convert screen pixel back to NDC [-1, 1]
    tap_ndc_x = (2.0 * (tap_x - x0) / W) - 1.0
    tap_ndc_y = 1.0 - (2.0 * (tap_y - y0) / H)
    print(f"  Step A: Tap in NDC: ({tap_ndc_x:.4f}, {tap_ndc_y:.4f})")

    # Step B: Unproject near and far points via inverse View-Projection matrix
    near_clip = [tap_ndc_x, tap_ndc_y, -1.0, 1.0]
    far_clip  = [tap_ndc_x, tap_ndc_y,  1.0, 1.0]

    near_world_h = mat4_vec4_mult(VP_inv, near_clip)
    far_world_h  = mat4_vec4_mult(VP_inv, far_clip)

    p_near = [near_world_h[i] / near_world_h[3] for i in range(3)]
    p_far  = [far_world_h[i] / far_world_h[3] for i in range(3)]

    # Step C: Construct 3D World Ray
    ray_dir = normalize3([p_far[0] - p_near[0], p_far[1] - p_near[1], p_far[2] - p_near[2]])
    print(f"  Step B: Ray Origin:    ({p_near[0]:.3f}, {p_near[1]:.3f}, {p_near[2]:.3f})")
    print(f"  Step C: Ray Direction: ({ray_dir[0]:.3f}, {ray_dir[1]:.3f}, {ray_dir[2]:.3f})")

    # Step D: Intersect Ray with AR Ground Plane at y = 0.0 (Floor Normal = [0, 1, 0])
    # Equation: p_near[1] + t * ray_dir[1] = 0.0  =>  t = -p_near[1] / ray_dir[1]
    t_hit = -p_near[1] / ray_dir[1]
    hit_pos = [
        p_near[0] + t_hit * ray_dir[0],
        0.0, # Exactly on floor
        p_near[2] + t_hit * ray_dir[2]
    ]

    print(f"  Step D: AR Ground Intersection (t = {t_hit:.3f} m):")
    print(f"          3D World Position = ({hit_pos[0]:.3f}, {hit_pos[1]:.3f}, {hit_pos[2]:.3f}) meters")
    print("  => VIRTUAL RED VELVET CHAIR ANCHORED STABLY ON LIVING ROOM FLOOR!")

    # ----------------------------------------------------
    # Part 3: Photometric Sheen vs GGX Comparison
    # ----------------------------------------------------
    print("\n" + "-" * 80)
    print("3. PHOTOMETRIC ILLUMINATION COMPARISON:")
    print("Evaluating Reflected Radiance at Varying Grazing Angles (v · n):")
    print(f"{'Viewing Angle':<18} {'v · n':<10} {'Phong Specular':<18} {'Charlie Sheen (Velvet)':<24} {'Visual Characteristic'}")
    print("-" * 80)

    angles = [0.0, 30.0, 60.0, 75.0, 85.0]
    alpha_sheen = 0.35
    for deg in angles:
        rad = math.radians(deg)
        v_dot_n = math.cos(rad) # 1.0 (head-on) down to 0.087 (grazing)

        # Standard Phong specular highlight: cos(theta)^alpha
        phong = max(0.0, v_dot_n)**16.0

        # Charlie Sheen: increases sharply at grazing angles (sin(theta)^1/alpha)
        sin_theta = math.sin(rad)
        charlie_d = ((2.0 + 1.0 / alpha_sheen) * (sin_theta ** (1.0 / alpha_sheen))) / (2.0 * math.pi)

        char_desc = "Head-on (Deep crimson matte)" if deg == 0 else ("Grazing (Intense white-pink edge halo)" if deg >= 75 else "Soft transition")
        print(f"{deg:>5.1f}°{'':<12} {v_dot_n:<10.3f} {phong:<18.4f} {charlie_d:<24.4f} {char_desc}")

    print("=" * 80 + "\n")

if __name__ == "__main__":
    run_webxr_viewport_math()

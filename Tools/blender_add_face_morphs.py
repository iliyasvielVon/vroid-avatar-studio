# -*- coding: utf-8 -*-
"""
给 VRoid/VRM 模型添加「捏脸」形态键（shape keys / blendshapes）。

用法：
  1. 给 Blender 装 VRM Add-on for Blender，导入你的 .vrm
  2. Scripting 工作区打开本脚本，点运行（或拖到文本编辑器运行）
  3. 用 VRM Add-on 重新导出 .vrm，放回 Unity 工程的 StreamingAssets/Avatars/
  4. Unity 里「捏脸」面板会自动多出 Adj_* 滑条

加的形态键（基于头部包围盒，不依赖朝向，稳健）：
  Adj_HeadSize   头整体大小
  Adj_FaceWidth  脸宽（X 缩放）
  Adj_FaceLength 脸长（Z 缩放）
  Adj_ChinLength 下巴长度（下半部下移）

无损：原模型不变，只新增形态键，默认权重 0。
"""
import bpy
from mathutils import Vector

HEAD_GROUP = "J_Bip_C_Head"   # VRoid 头骨顶点组
HEAD_FALLBACK_TOP_FRACTION = 0.22  # 找不到头骨组时，取最高 22% 顶点当作头


def head_vertex_indices(obj):
    me = obj.data
    vg = obj.vertex_groups.get(HEAD_GROUP)
    idx = []
    if vg is not None:
        gi = vg.index
        for v in me.vertices:
            w = 0.0
            for g in v.groups:
                if g.group == gi:
                    w = g.weight
                    break
            if w > 0.3:
                idx.append(v.index)
    if idx:
        return idx
    # fallback: highest-Z fraction
    zs = sorted(v.co.z for v in me.vertices)
    if not zs:
        return []
    cutoff = zs[int(len(zs) * (1.0 - HEAD_FALLBACK_TOP_FRACTION))]
    return [v.index for v in me.vertices if v.co.z >= cutoff]


def basis_coords(obj):
    kb = obj.data.shape_keys.key_blocks["Basis"] if obj.data.shape_keys else None
    if kb:
        return [Vector(d.co) for d in kb.data]
    return [Vector(v.co) for v in obj.data.vertices]


def ensure_basis(obj):
    if obj.data.shape_keys is None:
        obj.shape_key_add(name="Basis", from_mix=False)


def add_morph(obj, name, fn, head_idx, base, center):
    if name in (obj.data.shape_keys.key_blocks if obj.data.shape_keys else {}):
        return  # already present
    key = obj.shape_key_add(name=name, from_mix=False)
    for i in head_idx:
        co = base[i].copy()
        key.data[i].co = fn(co, center)


def process(obj):
    if obj.type != 'MESH' or not obj.data.vertices:
        return False
    ensure_basis(obj)
    head_idx = head_vertex_indices(obj)
    if not head_idx:
        return False
    base = basis_coords(obj)
    # head bounding-box center (from basis)
    hv = [base[i] for i in head_idx]
    mn = Vector((min(v.x for v in hv), min(v.y for v in hv), min(v.z for v in hv)))
    mx = Vector((max(v.x for v in hv), max(v.y for v in hv), max(v.z for v in hv)))
    center = (mn + mx) * 0.5
    low_z, high_z = mn.z, mx.z
    span_z = max(high_z - low_z, 1e-5)

    def head_size(co, c):
        return c + (co - c) * 1.12

    def face_width(co, c):
        d = co - c; d.x *= 1.14; return c + d

    def face_length(co, c):
        d = co - c; d.z *= 1.10; return c + d

    def chin_length(co, c):
        # lower part of the head moves down; weight = how low the vertex sits
        t = (high_z - co.z) / span_z          # 0 at top, 1 at bottom
        t = max(0.0, t) ** 2
        return Vector((co.x, co.y, co.z - 0.06 * span_z * t))

    add_morph(obj, "Adj_HeadSize", head_size, head_idx, base, center)
    add_morph(obj, "Adj_FaceWidth", face_width, head_idx, base, center)
    add_morph(obj, "Adj_FaceLength", face_length, head_idx, base, center)
    add_morph(obj, "Adj_ChinLength", chin_length, head_idx, base, center)
    return True


def main():
    done = 0
    for obj in bpy.data.objects:
        try:
            if process(obj):
                done += 1
                print(f"[face-morphs] added to: {obj.name}")
        except Exception as e:
            print(f"[face-morphs] skip {obj.name}: {e}")
    print(f"[face-morphs] done. meshes modified: {done}")


if __name__ == "__main__":
    main()

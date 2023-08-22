#ifndef PLUME_MESH_DATA_CG_INCLUDED
#define PLUME_MESH_DATA_CG_INCLUDED

uint index_buffer_stride;
ByteAddressBuffer index_buffer;

uint vertex_buffer_stride;
uint vertex_buffer_position_offset;
ByteAddressBuffer vertex_buffer;

uint3 load_triangle_indices(const uint triangle_idx)
{
    uint3 indices = 0;

    if (index_buffer_stride == 2)
    {
        // The input address of ByteAddressBuffer is expressed in bytes and must be a multiple of 4.
        // Cf https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/sm5-object-byteaddressbuffer-load#parameters

        // We do an integer division to get the closest 4bytes aligned chunk then do a bitwise operation to split
        // the 4 bytes into 2 ushort (2bytes)
        const uint uint_aligned_index0 = triangle_idx * 3 / 2u;
        const uint uint_aligned_index1 = (triangle_idx * 3 + 1) / 2u;
        const uint uint_aligned_index2 = (triangle_idx * 3 + 2) / 2u;

        if (triangle_idx % 2 == 0)
        {
            indices[0] = index_buffer.Load(uint_aligned_index0 * 4u) & 0xFFFF;
            indices[1] = (index_buffer.Load(uint_aligned_index1 * 4u) & 0xFFFF0000) >> 16;
            indices[2] = index_buffer.Load(uint_aligned_index2 * 4u) & 0xFFFF;
        }
        else
        {
            indices[0] = (index_buffer.Load(uint_aligned_index0 * 4u) & 0xFFFF0000) >> 16;
            indices[1] = index_buffer.Load(uint_aligned_index1 * 4u) & 0xFFFF;
            indices[2] = (index_buffer.Load(uint_aligned_index2 * 4u) & 0xFFFF0000) >> 16;
        }
    }
    else // (index_buffer_stride == 4)
    {
        indices[0] = index_buffer.Load(triangle_idx * 3 * 4u);
        indices[1] = index_buffer.Load((triangle_idx * 3 + 1) * 4u);
        indices[2] = index_buffer.Load((triangle_idx * 3 + 2) * 4u);
    }

    return indices;
}

float3 load_vertex_position(const uint vertex_idx)
{
    // Assuming the vertex position data is 4bytes aligned
    float3 pos = 0;
    pos.x = asfloat((uint)vertex_buffer.Load(vertex_idx * vertex_buffer_stride + vertex_buffer_position_offset));
    pos.y = asfloat((uint)vertex_buffer.Load(vertex_idx * vertex_buffer_stride + vertex_buffer_position_offset + 4));
    pos.z = asfloat((uint)vertex_buffer.Load(vertex_idx * vertex_buffer_stride + vertex_buffer_position_offset + 8));
    return pos;
}

#endif

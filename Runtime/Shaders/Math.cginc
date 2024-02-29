#ifndef PLUME_MATH_CG_INCLUDED
#define PLUME_MATH_CG_INCLUDED

static const float epsilon = 0.000001f;
static const float two_pi = 6.28318531f;
static const float sqrt_2_pi = 2.50662827f;

uint n_th_triangle_formula(const uint r)
{
    return (r + 1) * (r + 2) / 2u;
}

float map(const float value, const float old_min, const float old_max, const float new_min, const float new_max)
{
    return (value - old_min) / (old_max - old_min) * (new_max - new_min) + new_min;
}

#endif

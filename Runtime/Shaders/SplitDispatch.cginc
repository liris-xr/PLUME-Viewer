#ifndef PLUME_SPLIT_DISPATCH_CG_INCLUDED
#define PLUME_SPLIT_DISPATCH_CG_INCLUDED

// The compute shader calls might be split in several dispatch call. We keep track of the dispatch index
uint x_dispatch_index;
uint y_dispatch_index;
uint dispatch_max_thread_group;

#endif
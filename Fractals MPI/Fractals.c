#define min(a, b) (a < b ? a : b)
#define ceil(a, b) (a / b + ((a % b) > 0))

#define foreachA(f, d, b, p) \
	for (p.x = f.x; p.x < b.width; p.x += d.x) \
    for (p.y = f.y; p.y < b.height; p.y += d.y)

#define foreachB(f, d, b, p) \
	int _s = b.width * b.height; \
	int _c = d.x * d.y; \
	int _l = ceil(_s, _c); \
	int _w = f.x + f.y * d.x; \
	int _p0 = _w * _l; \
	int _pf = min((_p0 + _l), _s); \
	int _p; \
	for (_p = _p0; p.x = (double)(_p % b.width), p.y = (double)(_p / b.width), _p < _pf; _p++)

#define foreach foreachB

#ifdef __CUDACC__

    #define DeviceKernel __global__
    #define DeviceFunc __device__
    #define DeviceVar
    #define DeviceBlockStart extern "C" {
    #define DeviceBlockEnd }

    #define uint unsigned int
    #define ulong unsigned long
    #define mul_hi __mulhi

    #define get_global_id(i) (i == 0 ? threadIdx.x + blockIdx.x * blockDim.x : i == 1 ? threadIdx.y + blockIdx.y * blockDim.y : -1)
    #define get_global_size(i) (i == 0 ? gridDim.x * blockDim.x : i == 1 ? gridDim.y * blockDim.y : -1)

    #define ptr_buffer_size 1

#elif defined OpenCL

    #pragma OPENCL EXTENSION cl_khr_fp64: enable

    #define DeviceKernel __kernel
    #define DeviceFunc
    #define DeviceVar __global
    #define DeviceBlockStart
    #define DeviceBlockEnd

    #define ptr_buffer_size 1

#elif defined MPI

    #define DeviceKernel
    #define DeviceFunc
    #define DeviceVar
    #define DeviceBlockStart
    #define DeviceBlockEnd
    #define HostBlockEnabled

    #define uint unsigned int
    #define ulong unsigned long
    #define mul_hi(x, y) (uint)((ulong)x * y >> 32)

    #define ptr_buffer_size 1024

    int get_global_id(int i)
    {
		if (i != 0)
			return 0;

        int d;
        MPI_Comm_rank(MPI_COMM_WORLD, &d);
        return d;
    }

    int get_global_size(int i)
    {
		if (i != 0)
			return 1;

		int d;
        MPI_Comm_size(MPI_COMM_WORLD, &d);
        return d;
    }

#else

    #define DeviceKernel
    #define DeviceFunc
    #define DeviceVar
    #define DeviceBlockStart
    #define DeviceBlockEnd

    #define uint unsigned int
    #define ulong unsigned long
    #define mul_hi(x, y) (uint)((ulong)x * y >> 32)

    #define get_global_id(i) 0
    #define get_global_size(i) 1

    #define ptr_buffer_size 1

#endif

DeviceBlockStart

typedef struct
{
    double real;
    double imaginary;
} Complex;

typedef struct
{
    DeviceVar float* raw;
    int width;
    int height;
} Bitmap;

typedef struct
{
    double x;
    double y;
} Point;

typedef struct
{
    uint* a;
    uint* b;
} Random;

DeviceFunc Complex __Complex(double real, double imaginary)
{
    Complex obj = { real, imaginary };
    return obj;
}

DeviceFunc Complex add(Complex *a, Complex *b)
{
    return __Complex(a->real + b->real, a->imaginary + b->imaginary);
}

DeviceFunc Complex multiply(Complex *a, Complex *b)
{
    return __Complex(a->real * b->real - a->imaginary * b->imaginary, a->real * b->imaginary + a->imaginary * b->real);
}

DeviceFunc double magnitude2(Complex *c)
{
    return c->real * c->real + c->imaginary * c->imaginary;
}

DeviceFunc double absolute(double v)
{
    return v >= 0 ? v : -v;
}

DeviceFunc void setPixel(Point *p, float value, Bitmap *bitmap)
{
    int offset = (int)p->x + (int)p->y * bitmap->width;
    bitmap->raw[offset] = value;
}

DeviceFunc uint MWC64X(Random* state)
{
    const uint A = 4294883355U;
    uint a = *(state->a);
    uint b = *(state->b);
    uint res = a ^ b;
    uint hi = mul_hi(a, A);
    a = a * A + b;
    b = hi + (a < b);
    *(state->a) = a;
    *(state->b) = b;
    return res;
}

DeviceFunc double randomFactor(Random* state)
{
    return (double)(MWC64X(state)) / (double)(0xFFFFFFFF);
}

DeviceKernel void ship(
    DeviceVar float* output,
    DeviceVar int* width,
    DeviceVar int* height,
    DeviceVar float* scale,
    DeviceVar float* origin_x,
    DeviceVar float* origin_y,
    DeviceVar float* threshold,
    DeviceVar int* iterations)
{
    Bitmap bitmap = { output, *width, *height };

	Point f = { get_global_id(0), get_global_id(1) };
	Point d = { get_global_size(0), get_global_size(1) };

    Complex a, seed;
    Point p, j;
    int i;

    foreach (f, d, bitmap, p)
    {
        j.x = *scale * (double)(bitmap.width / 2 - p.x) / (bitmap.width / 2) - *origin_x;
        j.y = *scale * (double)(bitmap.height / 2 - p.y) / (bitmap.height / 2) - *origin_y;

        seed = a = __Complex(j.x, j.y);
        
        for (i = 0; i < *iterations; i ++)
        {
            a = __Complex(absolute(a.real), absolute(a.imaginary));
            a = multiply(&a, &a);
            a = add(&a, &seed);

            if (magnitude2(&a) > *threshold)
                break;
        }

        setPixel(&p, (float)i / *iterations, &bitmap);
    }
}

DeviceKernel void mandelbrot(
    DeviceVar float* output,
    DeviceVar int* width,
    DeviceVar int* height,
    DeviceVar float* scale,
    DeviceVar float* origin_x,
    DeviceVar float* origin_y,
    DeviceVar float* threshold,
    DeviceVar int* iterations)
{
    Bitmap bitmap = { output, *width, *height };

	Point f = { get_global_id(0), get_global_id(1) };
	Point d = { get_global_size(0), get_global_size(1) };

    Complex a, seed;
    Point p, j;
    int i;

    foreach (f, d, bitmap, p)
    {
        j.x = *scale * (double)(bitmap.width / 2 - p.x) / (bitmap.width / 2) - *origin_x;
        j.y = *scale * (double)(bitmap.height / 2 - p.y) / (bitmap.height / 2) - *origin_y;

        seed = a = __Complex(j.x, j.y);
        
        for (i = 0; i < *iterations; i ++)
        {
            a = multiply(&a, &a);
            a = add(&a, &seed);

            if (magnitude2(&a) > *threshold)
                break;
        }

        setPixel(&p, (float)i / *iterations, &bitmap);
    }
}

DeviceKernel void julia(
    DeviceVar float* output,
    DeviceVar int* width,
    DeviceVar int* height,
    DeviceVar float* scale,
    DeviceVar float* origin_x,
    DeviceVar float* origin_y,
    DeviceVar float* seed_r,
    DeviceVar float* seed_i,
    DeviceVar float* threshold,
    DeviceVar int* iterations)
{
    Complex seed = __Complex(*seed_r, *seed_i);
    Bitmap bitmap = { output, *width, *height };

	Point f = { get_global_id(0), get_global_id(1) };
	Point d = { get_global_size(0), get_global_size(1) };

	Complex a;
    Point p, j;
    int i;

	foreach (f, d, bitmap, p)
    {
        j.x = *scale * (double)(bitmap.width / 2 - p.x) / (bitmap.width / 2) - *origin_x;
        j.y = *scale * (double)(bitmap.height / 2 - p.y) / (bitmap.height / 2) - *origin_y;

        a = __Complex(j.x, j.y);

        for (i = 0; i < *iterations; i ++)
        {
            a = multiply(&a, &a);
            a = add(&a, &seed);

            if (magnitude2(&a) > *threshold)
                break;
        }
        
        setPixel(&p, (float)i / *iterations, &bitmap);
    }
}

DeviceKernel void fern(
    DeviceVar float* output,
    DeviceVar int* width,
    DeviceVar int* height,
    DeviceVar int* iterations,
    DeviceVar int* random_a,
    DeviceVar int* random_b)
{
    Random state = { (uint*)random_a, (uint*)random_b };
    Bitmap bitmap = { output, *width, *height };

    int i;
    double random;
    Point c, o, t = { randomFactor(&state), randomFactor(&state) };

    for (i = 0; i < *iterations; i ++)
    {
        random = randomFactor(&state);
        c = t;

        if (random < 0.01f)
        {
            t.x = 0.0f;
            t.y = 0.16f * c.y;
        }
        else if (random < 0.86f)
        {
            t.x = (0.85f * c.x) + (0.04f * c.y);
            t.y = (-0.04f * c.x) + (0.85f * c.y) + 1.6f;
        }
        else if (random < 0.93f)
        {
            t.x = (0.2f * c.x) - (0.26f * c.y);
            t.y = (0.23f * c.x) + (0.22f * c.y) + 1.6f;
        }
        else
        {
            t.x = (-0.15f * c.x) + (0.28f * c.y);
            t.y = (0.26f * c.x) + (0.24f * c.y) + 0.44f;
        }

        o.x = *width / 2.0f + t.x * *width / 10.0f;
        o.y = *height - t.y * *height / 12.0f;

        setPixel(&o, 0.5f, &bitmap);
    }
}

DeviceBlockEnd

#ifdef HostBlockEnabled

int equal(char* str1, char* str2)
{
    int i;

    for (i = 0; str1[i] == str2[i]; i++);

    i--;

    return i >= 0 && str1[i] == str2[i] && str2[i] == '\0';
}

float float_parse(char* string)
{
    int sign = 1, magnitude = 1, i = 0;
    float integer = 0.0f, fraction = 0.0f;
    
    if (string[i] == '-')
    {
        sign = -1;
        i++;
    }

    for (; ; i++)
    {
        if (string[i] == '\0')
            return integer * sign;
        else if (string[i] == ',')
            break;
        else if (string[i] >= '0' && string[i] <= '9')
            integer = integer * 10 + (string[i] - '0');
        else
            return 0.0f;
    }

    for (i++; ; i++, magnitude *= 10)
    {
        if (string[i] == '\0')
            return ((fraction / magnitude) + integer) * sign;
        else if (string[i] >= '0' && string[i] <= '9')
            fraction = fraction * 10 + (string[i] - '0');
        else
            return 0.0f;
    }
}

float* float_ptr(char* string)
{
    static float buffer[ptr_buffer_size];
    static int buffer_pos = 0;

    float *result = &buffer[buffer_pos = ((buffer_pos + 1) % ptr_buffer_size)];
    *result = float_parse(string);
    return result;
}

int* int_ptr(char* string)
{
    static int buffer[ptr_buffer_size];
    static int buffer_pos = 0;

    int *result = &buffer[buffer_pos = ((buffer_pos + 1) % ptr_buffer_size)];
    *result = (int)float_parse(string);
    return result;
}

void execute(char* function_name, void* result_buffer, char** params)
{
    if (equal(function_name, "ship"))
    {
        ship(
            (float*)result_buffer,
            int_ptr(params[0]),
            int_ptr(params[1]),
            float_ptr(params[2]),
            float_ptr(params[3]),
            float_ptr(params[4]),
            float_ptr(params[5]),
            int_ptr(params[6]));
    }
    else if (equal(function_name, "julia"))
    {
        julia(
            (float*)result_buffer,
            int_ptr(params[0]),
            int_ptr(params[1]),
            float_ptr(params[2]),
            float_ptr(params[3]),
            float_ptr(params[4]),
            float_ptr(params[5]),
            float_ptr(params[6]),
            float_ptr(params[7]),
            int_ptr(params[8]));
    }
    else if (equal(function_name, "mandelbrot"))
    {
        mandelbrot(
            (float*)result_buffer,
            int_ptr(params[0]),
            int_ptr(params[1]),
            float_ptr(params[2]),
            float_ptr(params[3]),
            float_ptr(params[4]),
            float_ptr(params[5]),
            int_ptr(params[6]));
    }
    else if (equal(function_name, "fern"))
    {
        fern(
            (float*)result_buffer,
            int_ptr(params[0]),
            int_ptr(params[1]),
            int_ptr(params[2]),
            int_ptr(params[3]),
            int_ptr(params[4]));
    }
}

#endif
#include "stdafx.h"

#include <stdlib.h>
#include <io.h>
#include <fcntl.h>
#include "mpi.h"

#define MPI
#include "Fractals.c"

#ifdef UNICODE
#undef UNICODE
#endif

typedef enum
{
	t_int,
	t_float,
	t_unknown
} Type;

typedef struct
{
	void *memory;
	int size;
	Type type;
	MPI_Datatype mpi_type;
	int element_size;

} Buffer;

Type parse_type(char* type)
{
	if (strcmp(type, "float") == 0)
		return t_float;
	else if (strcmp(type, "int") == 0)
		return t_int;
	else
		return t_unknown;
}

MPI_Datatype mpi_type(Type type)
{
	switch (type)
	{
		case t_float:
			return MPI_FLOAT;
		case t_int:
			return MPI_INT;
		default:
			return MPI_DATATYPE_NULL;
	}
}

Buffer create_buffer(int buffer_size, char* type_name)
{
	Buffer buffer;
	buffer.size = buffer_size;
	buffer.type = parse_type(type_name);
	buffer.mpi_type = mpi_type(buffer.type);

	switch (buffer.type)
	{
		case t_float:
			buffer.element_size = sizeof(float);
			break;
		case t_int:
			buffer.element_size = sizeof(int);
			break;
		default:
			buffer.element_size = 0;
	}

	int capacity = buffer.size * buffer.element_size;
	buffer.memory = (capacity > 0) ? malloc(capacity) : NULL;

	return buffer;
}

Buffer empty_buffer()
{
	return create_buffer(0, "");
}

void destroy_buffer(Buffer buffer)
{
	if (buffer.memory != NULL)
		free(buffer.memory);
}

void clear_buffer(Buffer buffer)
{
	memset(buffer.memory, 0, buffer.size);
}

void* offset_buffer(Buffer buffer, int offset)
{
	int shift = offset * buffer.element_size;
	return (void*)(&((char*)buffer.memory)[shift]);
}

void return_result(Buffer buffer)
{
	/*
	 *  Writing requires binary mode:
	 *	~  freopen(NULL, "wb", stdout); // unix; stdio.h
	 *	~  _setmode(_fileno(stdout), _O_BINARY); // win32; io.h, fcntl.h
	*/
	_setmode(_fileno(stdout), _O_BINARY);
	fwrite(buffer.memory, buffer.element_size, buffer.size, stdout);
}

int main(int argc, char* argv[])
{
	char** args = (char**)&argv[1];
	char* result_type = args[0];
	char* function_name = args[1];
	int buffer_size = atoi(args[2]);
	char** params = &args[3];

	int process_id, process_count, i, chunk_size, offset;
	void *buffer_beginning;

	MPI_Init(&argc, (char***)&argv);
	MPI_Comm_rank(MPI_COMM_WORLD, &process_id);
	MPI_Comm_size(MPI_COMM_WORLD, &process_count);

	Buffer result_buffer = (process_id == 0) ? create_buffer(buffer_size, result_type) : empty_buffer();
	Buffer local_buffer = create_buffer(buffer_size, result_type);
	clear_buffer(local_buffer);

	execute(function_name, local_buffer.memory, params);

	chunk_size = ceil(buffer_size, process_count);
	offset = chunk_size * process_id;
	buffer_beginning = offset_buffer(local_buffer, offset);

	MPI_Gather(buffer_beginning, chunk_size, local_buffer.mpi_type, result_buffer.memory, chunk_size, local_buffer.mpi_type, 0, MPI_COMM_WORLD);

	if (process_id == 0)
		return_result(result_buffer);

	destroy_buffer(result_buffer);
	destroy_buffer(local_buffer);

	MPI_Finalize();

	return 0;
}
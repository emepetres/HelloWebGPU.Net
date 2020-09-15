#pragma once

#include "pch.h"
#include <Windows.h>
#include <dawn/webgpu.h>

#define EXPORT _declspec(dllexport)

extern "C" {
	EXPORT void initialize(WGPUDevice _device, WGPUQueue _queue, WGPUSwapChain _swapchain);
	EXPORT void initializePipelineAndBuffers(WGPURenderPipeline _pipeline, WGPUBuffer _vertBuf, WGPUBuffer _indxBuf, WGPUBuffer _uRotBuf, WGPUBindGroup _bindGroup);
	EXPORT WGPUShaderModule createShader(const uint32_t* code, uint32_t size, const char* label);
	EXPORT WGPUShaderModule createVertShader();
	EXPORT WGPUShaderModule createFragShader();
	EXPORT WGPURenderPipeline TestDeviceCreateRenderPipeline(WGPURenderPipelineDescriptor* descriptor);
	EXPORT WGPUBuffer createBuffer(const void* data, size_t size, WGPUBufferUsage usage);
	EXPORT WGPURenderPipeline createPipeline(WGPUBindGroupLayout bindGroupLayout);
	EXPORT WGPUBuffer createVertBuffer();
	EXPORT WGPUBuffer createIndxBuffer();
	EXPORT WGPUBuffer createDataBuffer();
	EXPORT WGPUBindGroup createBindGroup(WGPUBindGroupLayout bindGroupLayout);
	EXPORT void createPipelineAndBuffers();
	EXPORT bool redraw();
}
#pragma once

#include "pch.h"
#include <Windows.h>
#include <dawn/webgpu.h>

#define EXPORT _declspec(dllexport)

extern "C" {
	EXPORT void initialize(WGPUDevice _device, WGPUQueue _queue, WGPUSwapChain _swapchain);
	EXPORT WGPUShaderModule createShader(const uint32_t* code, uint32_t size, const char* label);
	EXPORT WGPUShaderModule createVertShader();
	EXPORT WGPUShaderModule createFragShader();
	EXPORT WGPURenderPipeline TestDeviceCreateRenderPipeline(WGPURenderPipelineDescriptor* descriptor);
	EXPORT WGPUBuffer createBuffer(const void* data, size_t size, WGPUBufferUsage usage);
	EXPORT void createPipelineAndBuffers();
	EXPORT bool redraw();
}
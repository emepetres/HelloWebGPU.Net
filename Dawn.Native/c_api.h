#pragma once

#include "pch.h"
#include <Windows.h>
#include <dawn/webgpu.h>

#define EXPORT _declspec(dllexport)

extern "C" {
	EXPORT WGPUDevice createDevice(HWND handle, WGPUBackendType type);
	EXPORT WGPUSwapChain createSwapChain(WGPUDevice device, WGPUTextureUsage usage, UINT32 width, UINT32 height, WGPUPresentMode presentMode);
	EXPORT WGPUTextureFormat getSwapChainFormat(WGPUDevice device);
}
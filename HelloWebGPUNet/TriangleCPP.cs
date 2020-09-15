using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet
{
    class TriangleCPP
    {
		[DllImport("Dawn.Native.dll")]
		public static extern void initialize(IntPtr _device, IntPtr _queue, IntPtr _swapchain);

		[DllImport("Dawn.Native.dll")]
		public unsafe static extern IntPtr createShader(UInt32* code, UInt32 size, string label = null);

		[DllImport("Dawn.Native.dll")]
		public static extern IntPtr createVertShader();

		[DllImport("Dawn.Native.dll")]
		public static extern IntPtr createFragShader();

		[DllImport("Dawn.Native.dll")]
		public static extern IntPtr TestDeviceCreateRenderPipeline(ref WGPURenderPipelineDescriptor descriptor);

		[DllImport("Dawn.Native.dll")]
		public unsafe static extern IntPtr createBuffer(void* data, ulong size, WGPUBufferUsage usage);

		[DllImport("Dawn.Native.dll")]
		public static extern void createPipelineAndBuffers();

		[DllImport("Dawn.Native.dll")]
		public static extern bool redraw();
	}
}

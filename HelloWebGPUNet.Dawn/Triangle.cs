using HelloWebGPUNet.WebGPU;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet
{
	public unsafe class Triangle
	{
		public static IntPtr Device;
		public static IntPtr Queue;
		public static IntPtr SwapChain;

		public static IntPtr pipeline;
		public static IntPtr vertBuf; // vertex buffer with triangle position and colours
		public static IntPtr indxBuf; // index buffer
		public static IntPtr uRotBuf; // uniform buffer (containing the rotation angle)
		public static IntPtr bindGroup;

		static float rotDeg = 0.0f; // Current rotation angle (in degrees, updated per frame).
		static char* str_entrypoint = (char*)Marshal.StringToHGlobalAnsi("main").ToPointer();

		public static IntPtr createBindGroupLayout()
		{
			WGPUBindGroupLayoutEntry bglEntry = new WGPUBindGroupLayoutEntry
			{
				binding = 0,
				visibility = WGPUShaderStage.WGPUShaderStage_Vertex,
				type = WGPUBindingType.WGPUBindingType_UniformBuffer
			};
			WGPUBindGroupLayoutDescriptor bglDesc = new WGPUBindGroupLayoutDescriptor
			{
				entryCount = 1,
				entries = &bglEntry
			};
			return WebGPUNative.wgpuDeviceCreateBindGroupLayout(Device, &bglDesc);
		}

		public static IntPtr CreatePipeline(IntPtr bindGroupLayout)
		{
			// Load shaders
			var vertMod = CreateShader("Shaders/VertexShader.spirv");
			//var vertMod = TriangleCPP.createVertShader();

			var fragMod = CreateShader("Shaders/FragmentShader.spirv");
			//var fragMod = TriangleCPP.createFragShader();

			WGPUPipelineLayoutDescriptor layoutDesc = new WGPUPipelineLayoutDescriptor
			{
				bindGroupLayoutCount = 0
			};
			IntPtr pipelineLayout = WebGPUNative.wgpuDeviceCreatePipelineLayout(Device, &layoutDesc);

			// begin pipeline set-up
			WGPURenderPipelineDescriptor desc = new WGPURenderPipelineDescriptor
			{
				layout = pipelineLayout,
				vertexStage = new WGPUProgrammableStageDescriptor()
				{
					module = vertMod,
					entryPoint = str_entrypoint
				}
			};

			WGPUProgrammableStageDescriptor fragStage = new WGPUProgrammableStageDescriptor
			{
				module = fragMod,
				entryPoint = str_entrypoint
			};
			desc.fragmentStage = &fragStage;

			// describe buffer layouts
			var vertAttrs = stackalloc WGPUVertexAttributeDescriptor[2];
			vertAttrs[0] = new WGPUVertexAttributeDescriptor
			{
				format = WGPUVertexFormat.WGPUVertexFormat_Float4,
				offset = 0,
				shaderLocation = 0
			};
			vertAttrs[1] = new WGPUVertexAttributeDescriptor
			{
				format = WGPUVertexFormat.WGPUVertexFormat_Float4,
				offset = 4 * sizeof(float),
				shaderLocation = 1
			};
			WGPUVertexBufferLayoutDescriptor vertDesc = new WGPUVertexBufferLayoutDescriptor
			{
				arrayStride = 8 * sizeof(float),
				attributeCount = 2,
				attributes = vertAttrs
			};
			WGPUVertexStateDescriptor vertState = new WGPUVertexStateDescriptor
			{
				// TODO EMSCRIPTEN: WGPUIndexFormat.WGPUIndexFormat_Uint16 still needed in wasm
				////indexFormat = WGPUIndexFormat.WGPUIndexFormat_Uint16,
				vertexBufferCount = 1,
				vertexBuffers = &vertDesc
			};

			desc.vertexState = &vertState;
			desc.primitiveTopology = WGPUPrimitiveTopology.WGPUPrimitiveTopology_TriangleList;

			desc.sampleCount = 1;

			// describe blend
			WGPUBlendDescriptor blendDesc = new WGPUBlendDescriptor
			{
				operation = WGPUBlendOperation.WGPUBlendOperation_Add,
				srcFactor = WGPUBlendFactor.WGPUBlendFactor_SrcAlpha,
				dstFactor = WGPUBlendFactor.WGPUBlendFactor_OneMinusSrcAlpha
			};
			WGPUColorStateDescriptor colorDesc = new WGPUColorStateDescriptor
			{
				format = Dawn.getSwapChainFormat(Device),
				alphaBlend = blendDesc,
				colorBlend = blendDesc,
				writeMask = WGPUColorWriteMask.WGPUColorWriteMask_All
			};

			desc.colorStateCount = 1;
			desc.colorStates = &colorDesc;

			desc.sampleMask = 0xFFFFFFFF; // <-- Note: this currently causes Emscripten to fail (sampleMask ends up as -1, which trips an assert)

			IntPtr _pipeline = WebGPUNative.wgpuDeviceCreateRenderPipeline(Device, ref desc);

			// partial clean-up (just move to the end, no?)
			WebGPUNative.wgpuPipelineLayoutRelease(pipelineLayout);

			WebGPUNative.wgpuShaderModuleRelease(fragMod);
			WebGPUNative.wgpuShaderModuleRelease(vertMod);

			return _pipeline;
		}

		public static IntPtr CreateVertBuffer()
		{
			// create the buffers (x, y, r, g, b)
			Vector4[] vertData = new Vector4[]
			{
            	// TriangleList
            	new Vector4(0f, 0.5f, 0.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4(0.5f, -0.5f, 0.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-0.5f, -0.5f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
			};
			var p_vertData = stackalloc Vector4[vertData.Length];
			for (int i = 0; i < vertData.Length; i++)
			{
				p_vertData[i] = vertData[i];
			}
			return CreateBuffer(p_vertData, (ulong)(vertData.Length * sizeof(Vector4)), WGPUBufferUsage.WGPUBufferUsage_Vertex);
		}

		public static void CreatePipelineAndBuffers()
		{
			IntPtr bindGroupLayout = createBindGroupLayout();
			//IntPtr bindGroupLayout = TriangleCPP.createBindGroupLayout();

			pipeline = CreatePipeline(bindGroupLayout);
			//pipeline = TriangleCPP.createPipeline(bindGroupLayout);

			vertBuf = CreateVertBuffer();
			//vertBuf = TriangleCPP.createVertBuffer();

			// last bit of clean-up
			WebGPUNative.wgpuBindGroupLayoutRelease(bindGroupLayout);
		}

		public static bool redraw()
		{
			IntPtr backBufView = WebGPUNative.wgpuSwapChainGetCurrentTextureView(SwapChain); // create textureView

			WGPURenderPassColorAttachmentDescriptor colorDesc = new WGPURenderPassColorAttachmentDescriptor
			{
				attachment = backBufView,
				loadOp = WGPULoadOp.WGPULoadOp_Clear,
				storeOp = WGPUStoreOp.WGPUStoreOp_Store,
				clearColor = new WGPUColor
				{
					r = 0.3f,
					g = 0.3f,
					b = 0.3f,
					a = 1.0f
				}
			};

			WGPURenderPassDescriptor renderPass = new WGPURenderPassDescriptor
			{
				colorAttachmentCount = 1,
				colorAttachments = &colorDesc
			};

			IntPtr encoder = WebGPUNative.wgpuDeviceCreateCommandEncoder(Device, null); // create encoder
			IntPtr pass = WebGPUNative.wgpuCommandEncoderBeginRenderPass(encoder, &renderPass);


			// draw the triangle (comment these five lines to simply clear the screen)
			WebGPUNative.wgpuRenderPassEncoderSetPipeline(pass, pipeline);
			WebGPUNative.wgpuRenderPassEncoderSetVertexBuffer(pass, 0, vertBuf, 0, 0);
			WebGPUNative.wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);

			WebGPUNative.wgpuRenderPassEncoderEndPass(pass);
			WebGPUNative.wgpuRenderPassEncoderRelease(pass);                         // release pass
			IntPtr commands = WebGPUNative.wgpuCommandEncoderFinish(encoder, null);  // create commands
			WebGPUNative.wgpuCommandEncoderRelease(encoder);                         // release encoder

			WebGPUNative.wgpuQueueSubmit(Queue, 1, &commands);
			WebGPUNative.wgpuCommandBufferRelease(commands);                         // release commands

			// TODO EMSCRIPTEN: wgpuSwapChainPresent is unsupported in Emscripten, so what do we do?
			WebGPUNative.wgpuSwapChainPresent(SwapChain);

			WebGPUNative.wgpuTextureViewRelease(backBufView);                        // release textureView

			return true;
		}

		private static IntPtr CreateShader(string path, char* label = null)
		{
			var data = File.ReadAllBytes(path);
			uint[] byte_code = new uint[data.Length / sizeof(uint)];
			Buffer.BlockCopy(data, 0, byte_code, 0, data.Length);
			return CreateShader(byte_code, label);
		}

		private static IntPtr CreateShader(UInt32[] byte_code, char* label = null)
		{
			IntPtr shader;
			fixed (uint* code = byte_code)
			{
				WGPUShaderModuleSPIRVDescriptor spirv = new WGPUShaderModuleSPIRVDescriptor()
				{
					chain = new WGPUChainedStruct()
					{
						sType = WGPUSType.WGPUSType_ShaderModuleSPIRVDescriptor
					},
					codeSize = (uint)byte_code.Length,
					code = code
				};

				WGPUShaderModuleDescriptor desc = new WGPUShaderModuleDescriptor()
				{
					nextInChain = (WGPUChainedStruct*)&spirv,
					label = label
				};

				shader = WebGPUNative.wgpuDeviceCreateShaderModule(Device, &desc);
			}

			return shader;
		}

		/**
		 * Helper to create a buffer.
		 *
		 * \param[in] data pointer to the start of the raw data
		 * \param[in] size number of bytes in \a data
		 * \param[in] usage type of buffer
		 */
		private static IntPtr CreateBuffer(void* data, ulong size, WGPUBufferUsage usage)
		{
			WGPUBufferDescriptor desc = new WGPUBufferDescriptor
			{
				usage = WGPUBufferUsage.WGPUBufferUsage_CopyDst | usage,
				size = size
			};
			IntPtr buffer = WebGPUNative.wgpuDeviceCreateBuffer(Device, ref desc);
			QueueWriteBuffer(buffer, 0, data, size);
			return buffer;
		}

		private static void QueueWriteBuffer(IntPtr buffer, ulong bufferOffset, void* data, ulong size)
		{
			// TODO EMSCRIPTEN change to wgpuBufferSetSubData(buffer, bufferOffset, size, data) for emscripten
			WebGPUNative.wgpuQueueWriteBuffer(Queue, buffer, bufferOffset, data, size);
		}
	}
}

using HelloWebGPUNet.WebGPU;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
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
			//ShaderCodeToUnmanagedMemory(triangleVert, out triangle_vert);
			//ShaderCodeToUnmanagedMemory(triangleFrag, out triangle_frag);
			//var vertMod = CreateShader((uint*)triangle_vert.ToPointer(), (uint)triangleVert.Length);
			//var fragMod = CreateShader((uint*)triangle_frag.ToPointer(), (uint)triangleFrag.Length);
			var vertMod = TriangleCPP.createVertShader();
			var fragMod = TriangleCPP.createFragShader();

			WGPUPipelineLayoutDescriptor layoutDesc = new WGPUPipelineLayoutDescriptor
			{
				bindGroupLayoutCount = 1,
				bindGroupLayouts = &bindGroupLayout
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
				format = WGPUVertexFormat.WGPUVertexFormat_Float2,
				offset = 0,
				shaderLocation = 0
			};
			vertAttrs[1] = new WGPUVertexAttributeDescriptor
			{
				format = WGPUVertexFormat.WGPUVertexFormat_Float3,
				offset = 2 * sizeof(float),
				shaderLocation = 1
			};

			WGPUVertexBufferLayoutDescriptor vertDesc = new WGPUVertexBufferLayoutDescriptor
			{
				arrayStride = 5 * sizeof(float),
				attributeCount = 2,
				attributes = vertAttrs
			};
			WGPUVertexStateDescriptor vertState = new WGPUVertexStateDescriptor
			{
				indexFormat = WGPUIndexFormat.WGPUIndexFormat_Uint16,
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
			//IntPtr _pipeline = TriangleCPP.TestDeviceCreateRenderPipeline(ref desc);

			// partial clean-up (just move to the end, no?)
			WebGPUNative.wgpuPipelineLayoutRelease(pipelineLayout);

			WebGPUNative.wgpuShaderModuleRelease(fragMod);
			WebGPUNative.wgpuShaderModuleRelease(vertMod);

			return _pipeline;
		}

		public static IntPtr CreateVertBuffer()
        {
			// create the buffers (x, y, r, g, b)
			float[] vertData = {
				-0.8f, -0.8f, 0.0f, 0.0f, 1.0f, // BL
				 0.8f, -0.8f, 0.0f, 1.0f, 0.0f, // BR
				-0.0f,  0.8f, 1.0f, 0.0f, 0.0f, // top
			};
			var p_vertData = stackalloc float[vertData.Length];
			for (int i = 0; i < vertData.Length; i++)
			{
				p_vertData[i] = vertData[i];
			}
			return CreateBuffer(p_vertData, (ulong)(vertData.Length * sizeof(float)), WGPUBufferUsage.WGPUBufferUsage_Vertex);
		}

		public static IntPtr CreateIndxBuffer()
		{
			UInt16[] indxData = {
				0, 1, 2,
				0 // padding (better way of doing this?)
			};
			var p_indxData = stackalloc float[indxData.Length];
			for (int i = 0; i < indxData.Length; i++)
			{
				p_indxData[i] = indxData[i];
			}
			return CreateBuffer(p_indxData, (ulong)(indxData.Length * sizeof(UInt16)), WGPUBufferUsage.WGPUBufferUsage_Index);
		}

		public static IntPtr CreateDataBuffer()
		{
			IntPtr data_buff;
			// create the uniform bind group (note 'rotDeg' is copied here, not bound in any way)
			fixed (void* data = &rotDeg)
			{
				data_buff = CreateBuffer(data, sizeof(float), WGPUBufferUsage.WGPUBufferUsage_Uniform);
			}

			return data_buff;
		}

		public static IntPtr CreateBindGroup(IntPtr bindGroupLayout)
		{
			WGPUBindGroupEntry bgEntry = new WGPUBindGroupEntry
			{
				binding = 0,
				buffer = uRotBuf,
				offset = 0,
				size = sizeof(float) // sizeof(rotDeg)
			};

			WGPUBindGroupDescriptor bgDesc = new WGPUBindGroupDescriptor
			{
				layout = bindGroupLayout,
				entryCount = 1,
				entries = &bgEntry
			};

			return WebGPUNative.wgpuDeviceCreateBindGroup(Device, &bgDesc);
		}

		public static void CreatePipelineAndBuffers()
        {
			IntPtr bindGroupLayout = createBindGroupLayout();

			//pipeline = CreatePipeline(bindGroupLayout);
			pipeline = TriangleCPP.createPipeline(bindGroupLayout);

			//vertBuf = CreateVertBuffer();
			vertBuf = TriangleCPP.createVertBuffer();
			//indxBuf = CreateIndxBuffer();
			indxBuf = TriangleCPP.createIndxBuffer();
			//uRotBuf = CreateDataBuffer();
			uRotBuf = TriangleCPP.createDataBuffer();

			bindGroup = CreateBindGroup(bindGroupLayout);
			//bindGroup = TriangleCPP.createBindGroup(bindGroupLayout);

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

			// update the rotation
			rotDeg += 0.1f;
			fixed (void* data = &rotDeg)
			{
				QueueWriteBuffer(uRotBuf, 0, data, sizeof(float));
			}

            // draw the triangle (comment these five lines to simply clear the screen)
            WebGPUNative.wgpuRenderPassEncoderSetPipeline(pass, pipeline);
            WebGPUNative.wgpuRenderPassEncoderSetBindGroup(pass, 0, bindGroup, 0, null);
            WebGPUNative.wgpuRenderPassEncoderSetVertexBuffer(pass, 0, vertBuf, 0, 0);
            WebGPUNative.wgpuRenderPassEncoderSetIndexBuffer(pass, indxBuf, 0, 0);
            WebGPUNative.wgpuRenderPassEncoderDrawIndexed(pass, 3, 1, 0, 0, 0);

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

		private static void ShaderCodeToUnmanagedMemory(UInt32[] code, out IntPtr p_code)
        {
			int byteCount = code.Length * sizeof(UInt32);
			p_code = Marshal.AllocHGlobal(byteCount);
			// auxiliar byte array is needed because marshal does not accept UInt
			byte[] byte_code = new byte[byteCount];
			Buffer.BlockCopy(code, 0, byte_code, 0, byteCount);
			Marshal.Copy(byte_code, 0, p_code, byteCount);
		}

		/**
		 * Helper to create a shader from SPIR-V IR.
		 *
		 * \param[in] code shader source (output using the \c -V \c -x options in \c glslangValidator)
		 * \param[in] size size of \a code in bytes
		 * \param[in] label optional shader name
		 */
		private static IntPtr CreateShader(uint* code, UInt32 size, char* label = null)
        {
			WGPUShaderModuleSPIRVDescriptor spirv = new WGPUShaderModuleSPIRVDescriptor()
			{
				chain = new WGPUChainedStruct()
				{
					sType = WGPUSType.WGPUSType_ShaderModuleSPIRVDescriptor
				},
				codeSize = size * sizeof(UInt32),
				code = code
			};

			WGPUShaderModuleDescriptor desc = new WGPUShaderModuleDescriptor()
			{
				nextInChain = (WGPUChainedStruct*)&spirv,
				label = label
			};

			return WebGPUNative.wgpuDeviceCreateShaderModule(Device, &desc);
		}

		private static IntPtr CreateShader2(UInt32[] byte_code, char* label = null)
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
					codeSize = (uint)byte_code.Length * sizeof(UInt32),
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
		private static IntPtr CreateBuffer(void* data, ulong size, WGPUBufferUsage usage) {
			WGPUBufferDescriptor desc = new WGPUBufferDescriptor
			{
				usage = WGPUBufferUsage.WGPUBufferUsage_CopyDst | usage,
				size = size
			};
			IntPtr buffer = WebGPUNative.wgpuDeviceCreateBuffer(Device, &desc);
			QueueWriteBuffer(buffer, 0, data, size);
			return buffer;
		}

		private static void QueueWriteBuffer(IntPtr buffer, ulong bufferOffset, void * data, ulong size)
        {
			// TODO EMSCRIPTEN change to wgpuBufferSetSubData(buffer, bufferOffset, size, data) for emscripten
			WebGPUNative.wgpuQueueWriteBuffer(Queue, buffer, bufferOffset, data, size);
		}

		public static char* Str2Ptr(string str)
        {
			char* p = stackalloc char[str.Length];
			for (int i = 0; i < str.Length; i++)
            {
				p[i] = str[i];
            }
			return p;
		}

		/**
		 * Vertex shader SPIR-V.
		 * \code
		 *	// glslc -Os -mfmt=num -o - -c in.vert
		 *	#version 450
		 *	layout(set = 0, binding = 0) uniform Rotation {
		 *		float uRot;
		 *	};
		 *	layout(location = 0) in  vec2 aPos;
		 *	layout(location = 1) in  vec3 aCol;
		 *	layout(location = 0) out vec3 vCol;
		 *	void main() {
		 *		float cosA = cos(radians(uRot));
		 *		float sinA = sin(radians(uRot));
		 *		mat3 rot = mat3(cosA, sinA, 0.0,
		 *					   -sinA, cosA, 0.0,
		 *						0.0,  0.0,  1.0);
		 *		gl_Position = vec4(rot * vec3(aPos, 1.0), 1.0);
		 *		vCol = aCol;
		 *	}
		 * \endcode
		 */
		private static IntPtr triangle_vert;
		private static UInt32[] triangleVert = {
			0x07230203, 0x00010000, 0x000d0008, 0x00000043, 0x00000000, 0x00020011, 0x00000001, 0x0006000b,
			0x00000001, 0x4c534c47, 0x6474732e, 0x3035342e, 0x00000000, 0x0003000e, 0x00000000, 0x00000001,
			0x0009000f, 0x00000000, 0x00000004, 0x6e69616d, 0x00000000, 0x0000002d, 0x00000031, 0x0000003e,
			0x00000040, 0x00050048, 0x00000009, 0x00000000, 0x00000023, 0x00000000, 0x00030047, 0x00000009,
			0x00000002, 0x00040047, 0x0000000b, 0x00000022, 0x00000000, 0x00040047, 0x0000000b, 0x00000021,
			0x00000000, 0x00050048, 0x0000002b, 0x00000000, 0x0000000b, 0x00000000, 0x00050048, 0x0000002b,
			0x00000001, 0x0000000b, 0x00000001, 0x00050048, 0x0000002b, 0x00000002, 0x0000000b, 0x00000003,
			0x00050048, 0x0000002b, 0x00000003, 0x0000000b, 0x00000004, 0x00030047, 0x0000002b, 0x00000002,
			0x00040047, 0x00000031, 0x0000001e, 0x00000000, 0x00040047, 0x0000003e, 0x0000001e, 0x00000000,
			0x00040047, 0x00000040, 0x0000001e, 0x00000001, 0x00020013, 0x00000002, 0x00030021, 0x00000003,
			0x00000002, 0x00030016, 0x00000006, 0x00000020, 0x0003001e, 0x00000009, 0x00000006, 0x00040020,
			0x0000000a, 0x00000002, 0x00000009, 0x0004003b, 0x0000000a, 0x0000000b, 0x00000002, 0x00040015,
			0x0000000c, 0x00000020, 0x00000001, 0x0004002b, 0x0000000c, 0x0000000d, 0x00000000, 0x00040020,
			0x0000000e, 0x00000002, 0x00000006, 0x00040017, 0x00000018, 0x00000006, 0x00000003, 0x00040018,
			0x00000019, 0x00000018, 0x00000003, 0x0004002b, 0x00000006, 0x0000001e, 0x00000000, 0x0004002b,
			0x00000006, 0x00000022, 0x3f800000, 0x00040017, 0x00000027, 0x00000006, 0x00000004, 0x00040015,
			0x00000028, 0x00000020, 0x00000000, 0x0004002b, 0x00000028, 0x00000029, 0x00000001, 0x0004001c,
			0x0000002a, 0x00000006, 0x00000029, 0x0006001e, 0x0000002b, 0x00000027, 0x00000006, 0x0000002a,
			0x0000002a, 0x00040020, 0x0000002c, 0x00000003, 0x0000002b, 0x0004003b, 0x0000002c, 0x0000002d,
			0x00000003, 0x00040017, 0x0000002f, 0x00000006, 0x00000002, 0x00040020, 0x00000030, 0x00000001,
			0x0000002f, 0x0004003b, 0x00000030, 0x00000031, 0x00000001, 0x00040020, 0x0000003b, 0x00000003,
			0x00000027, 0x00040020, 0x0000003d, 0x00000003, 0x00000018, 0x0004003b, 0x0000003d, 0x0000003e,
			0x00000003, 0x00040020, 0x0000003f, 0x00000001, 0x00000018, 0x0004003b, 0x0000003f, 0x00000040,
			0x00000001, 0x0006002c, 0x00000018, 0x00000042, 0x0000001e, 0x0000001e, 0x00000022, 0x00050036,
			0x00000002, 0x00000004, 0x00000000, 0x00000003, 0x000200f8, 0x00000005, 0x00050041, 0x0000000e,
			0x0000000f, 0x0000000b, 0x0000000d, 0x0004003d, 0x00000006, 0x00000010, 0x0000000f, 0x0006000c,
			0x00000006, 0x00000011, 0x00000001, 0x0000000b, 0x00000010, 0x0006000c, 0x00000006, 0x00000012,
			0x00000001, 0x0000000e, 0x00000011, 0x0006000c, 0x00000006, 0x00000017, 0x00000001, 0x0000000d,
			0x00000011, 0x0004007f, 0x00000006, 0x00000020, 0x00000017, 0x00060050, 0x00000018, 0x00000023,
			0x00000012, 0x00000017, 0x0000001e, 0x00060050, 0x00000018, 0x00000024, 0x00000020, 0x00000012,
			0x0000001e, 0x00060050, 0x00000019, 0x00000026, 0x00000023, 0x00000024, 0x00000042, 0x0004003d,
			0x0000002f, 0x00000032, 0x00000031, 0x00050051, 0x00000006, 0x00000033, 0x00000032, 0x00000000,
			0x00050051, 0x00000006, 0x00000034, 0x00000032, 0x00000001, 0x00060050, 0x00000018, 0x00000035,
			0x00000033, 0x00000034, 0x00000022, 0x00050091, 0x00000018, 0x00000036, 0x00000026, 0x00000035,
			0x00050051, 0x00000006, 0x00000037, 0x00000036, 0x00000000, 0x00050051, 0x00000006, 0x00000038,
			0x00000036, 0x00000001, 0x00050051, 0x00000006, 0x00000039, 0x00000036, 0x00000002, 0x00070050,
			0x00000027, 0x0000003a, 0x00000037, 0x00000038, 0x00000039, 0x00000022, 0x00050041, 0x0000003b,
			0x0000003c, 0x0000002d, 0x0000000d, 0x0003003e, 0x0000003c, 0x0000003a, 0x0004003d, 0x00000018,
			0x00000041, 0x00000040, 0x0003003e, 0x0000003e, 0x00000041, 0x000100fd, 0x00010038
		};

		/**
		 * Fragment shader SPIR-V.
		 * \code
		 *	// glslc -Os -mfmt=num -o - -c in.frag
		 *	#version 450
		 *	layout(location = 0) in  vec3 vCol;
		 *	layout(location = 0) out vec4 fragColor;
		 *	void main() {
		 *		fragColor = vec4(vCol, 1.0);
		 *	}
		 * \endcode
		 */
		private static IntPtr triangle_frag;
		private static UInt32[] triangleFrag = {
			0x07230203, 0x00010000, 0x000d0007, 0x00000013, 0x00000000, 0x00020011, 0x00000001, 0x0006000b,
			0x00000001, 0x4c534c47, 0x6474732e, 0x3035342e, 0x00000000, 0x0003000e, 0x00000000, 0x00000001,
			0x0007000f, 0x00000004, 0x00000004, 0x6e69616d, 0x00000000, 0x00000009, 0x0000000c, 0x00030010,
			0x00000004, 0x00000007, 0x00040047, 0x00000009, 0x0000001e, 0x00000000, 0x00040047, 0x0000000c,
			0x0000001e, 0x00000000, 0x00020013, 0x00000002, 0x00030021, 0x00000003, 0x00000002, 0x00030016,
			0x00000006, 0x00000020, 0x00040017, 0x00000007, 0x00000006, 0x00000004, 0x00040020, 0x00000008,
			0x00000003, 0x00000007, 0x0004003b, 0x00000008, 0x00000009, 0x00000003, 0x00040017, 0x0000000a,
			0x00000006, 0x00000003, 0x00040020, 0x0000000b, 0x00000001, 0x0000000a, 0x0004003b, 0x0000000b,
			0x0000000c, 0x00000001, 0x0004002b, 0x00000006, 0x0000000e, 0x3f800000, 0x00050036, 0x00000002,
			0x00000004, 0x00000000, 0x00000003, 0x000200f8, 0x00000005, 0x0004003d, 0x0000000a, 0x0000000d,
			0x0000000c, 0x00050051, 0x00000006, 0x0000000f, 0x0000000d, 0x00000000, 0x00050051, 0x00000006,
			0x00000010, 0x0000000d, 0x00000001, 0x00050051, 0x00000006, 0x00000011, 0x0000000d, 0x00000002,
			0x00070050, 0x00000007, 0x00000012, 0x0000000f, 0x00000010, 0x00000011, 0x0000000e, 0x0003003e,
			0x00000009, 0x00000012, 0x000100fd, 0x00010038
		};
	}

	//public class WGPUPipelineLayout
	//{
	//	private IntPtr value;
	//	public static implicit operator WGPUPipelineLayout(IntPtr val)
	//	{
	//		return new WGPUPipelineLayout() { value = val };
	//	}
	//	public static implicit operator IntPtr(WGPUPipelineLayout obj)
	//	{
	//		return ((obj == null) ? IntPtr.Zero : obj.value);
	//	}
	//}
}
